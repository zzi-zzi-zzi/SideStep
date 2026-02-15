/*
SideStep is licensed under a
Creative Commons Attribution-NonCommercial-ShareAlike 4.0 International License.
You should have received a copy of the license along with this
work. If not, see <http://creativecommons.org/licenses/by-nc-sa/4.0/>.
Original work done by zzi
                                                                                 */

#nullable enable
using Buddy.Coroutines;
using ff14bot;
using ff14bot.AClasses;
using ff14bot.Behavior;
using ff14bot.Directors;
using ff14bot.Helpers;
using ff14bot.Managers;
using ff14bot.Navigation;
using ff14bot.NeoProfiles;
using ff14bot.Objects;
using ff14bot.Pathing.Avoidance;
using Sidestep.Helpers;
using Sidestep.Interfaces;
using Sidestep.Logging;
using SideStep.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using TreeSharp;

namespace Sidestep
{
    // ReSharper disable once UnusedType.Global
    public class Sidestep : BotPlugin
    {
        public override string Author => "ZZI";
        public override Version Version => new Version(7, 4, 0);
        public override string Name => "SideStep";
        public override bool WantButton => true;

        public override string ButtonText => "Clear";
        
        private static ActionRunCoroutine? _sHook;
        
        private struct AvoidanceHandler
        {
            public Func<BattleCharacter, float, IEnumerable<AvoidInfo>> Method;
            public AvoiderAttribute Attribute;
        }

        private struct MapEffectHandler
        {
            public Func<MapEffect, float, IEnumerable<AvoidInfo>> Method;
            public AvoiderAttribute Attribute;
        }
        private struct ZoneHandler
        {
            public Func<IEnumerable<AvoidInfo>> Method;
            public AvoiderAttribute Attribute;
        }

        private readonly LRUCache<(uint, uint), uint> _loggedSpells = new(20);
        private readonly Dictionary<uint, AvoidanceHandler> _spellAvoiders = new();
        private readonly Dictionary<uint, AvoidanceHandler> _omenAvoiders = new();
        private readonly Dictionary<uint, AvoidanceHandler> _castTypeAvoiders = new();
        private readonly Dictionary<(uint Zone, uint Id), MapEffectHandler> _worldAvoiders = new();
        private readonly Dictionary<uint, List<ZoneHandler>> _zoneAvoiders = new();

        private readonly List<AvoidInfo> _tracked = new();

        private readonly List<AvoidInfo> _zoneTracked = new();
        private uint ZoneId = 0;

        #region Plugin Settings

        public override void OnButtonPress()
        {
            Logger.Verbose("Clearing Avoidance Manager");
            AvoidanceManager.RemoveAllAvoids(_ => true);
        }

        public override void OnInitialize()
        {
            LoadAvoidanceObjects();
        }

        public override void OnEnabled()
        {
            Logger.Verbose("Sidestep has been Enabled");
            TreeRoot.OnStop += OnStop;
            GameEvents.OnMapChanged += OnMapChanged;
            TreeHooks.Instance.OnHooksCleared += ReHookAvoid;
            ReHookAvoid(null, null);
        }

        private void OnMapChanged(object? sender, EventArgs e) => RemoveOldAvoids(true);

        private void OnStop(BotBase bot) => Clear();

        private void Clear()
        {
            _tracked.Clear();
            _zoneTracked.Clear();
            // Currently (1.0.818) remove all avoids only triggers on BotEvent.Stop triggers. We may need to clear on other stops
            AvoidanceManager.RemoveAllAvoids(_ => true);
        }

        private int RemoveOldAvoids(bool force = false)
        {
            var removalCount = 0;

            for (int i = _tracked.Count - 1; i >= 0; i--)
            {
                var info = _tracked[i];
                if (!info.Condition() || force)
                {
                    // we need to remove this
                    AvoidanceManager.RemoveAvoid(info);
                    _tracked.RemoveAt(i);
                    removalCount++;
                }
            }

            if(WorldManager.ZoneId != ZoneId)
            {
                for (int i = _zoneTracked.Count - 1; i >= 0; i--)
                {
                    var info = _zoneTracked[i];
                    AvoidanceManager.RemoveAvoid(info);
                    _zoneTracked.RemoveAt(i);
                    removalCount++;
                }
            }

            return removalCount;

        }

        public override void OnDisabled()
        {
            Logger.Verbose("Sidestep has been Disabled");
            if (_sHook != null)
                TreeHooks.Instance.RemoveHook("TreeStart", _sHook);

            TreeHooks.Instance.OnHooksCleared -= ReHookAvoid;
            TreeRoot.OnStop -= OnStop;
            GameEvents.OnMapChanged -= OnMapChanged;

            _tracked.Clear();
        }
        
        
        public override void OnPulse()
        {
            //don't run if we don't have a navigation provider
            if (Navigator.NavigationProvider == null)
                return;

            // don't run if we can't do combat
            if (WorldManager.InSanctuary)
                return;

            using (new PerformanceLogger("Pulse"))
            {
                // Remove tracked avoidances that have completed
                // Avoid LINQ allocations here by iterating backwards or using a list to collect removal candidates without delegates
                var removalCount = RemoveOldAvoids();
                
                if (removalCount > 0)
                {
                    Logger.Info($"Removing {removalCount} completed spells");
                }
                
                // spells
                var count = 0;
                foreach (var obj in GameObjectManager.GetObjectsOfType<BattleCharacter>())
                {
                    var (handler, character) = IsValid(obj);
                    if (handler != null && character != null) // Ensure valid before processing
                    {
                        var newAvoids = handler.Value.Method(character, handler.Value.Attribute.Range);
                        if (newAvoids != null)
                        {
                            foreach (var avoid in newAvoids)
                            {
                                _tracked.Add(avoid);
                                count++;
                            }
                        }
                    }
                }
                
                // world events
                if (DirectorManager.ActiveDirector != null && DirectorManager.ActiveDirector.IsValid && DirectorManager.ActiveDirector is InstanceContentDirector ic)
                {
                    // MapEffects is a collection, avoid LINQ
                    foreach (var effect in ic.MapEffects)
                    {
                        var (handler, mapEffect) = MapEffects(effect);
                        if (handler != null && mapEffect != null)
                        {
                           // null op for now
                           var newavoids = handler.Value.Method(mapEffect.Value, handler.Value.Attribute.Range);
                           if (newavoids != null)
                           {
                               foreach (var avoid in newavoids)
                               {
                                   _tracked.Add(avoid);
                                   count++;
                               }
                           }
                        }
                    }
                }

                // Zone
                if(ZoneId != WorldManager.ZoneId)
                {
                    ZoneId = WorldManager.ZoneId;
                    if(_zoneAvoiders.TryGetValue(ZoneId, out var handles))
                    {
                        var list = handles.SelectMany(h => h.Method());
                        // Add all zone tracked avoiders
                        _zoneTracked.AddRange(list);
                    }
                }

                if (count != 0)
                {
                    Logger.Info($"Added: {count} to the avoidance manager");
                }
            }
        }

        #endregion


        private static void ReHookAvoid(object? sender, EventArgs? e)
        {
            _sHook = new ActionRunCoroutine(async _ =>
            {
                var poiType = Poi.Current.Type;
                
                // Special case: Bot will do a lot of fast stop & go when avoiding a mob that moves slowly and trying to
                // do something near the mob. To fix, a delay is added to slow down the 'Stop & go' behavior
                if (poiType is not (PoiType.Collect or PoiType.Gather or PoiType.Hotspot)) return false;

                if (!Core.Me.InCombat || !AvoidanceManager.Avoids.Any(o => o.IsPointInAvoid(Poi.Current.Location)))
                    return false;
                
                TreeRoot.StatusText = "Waiting for 'avoid' to move before attempting to interact " +
                                      Poi.Current.Name;
                var randomWaitTime = (new Random()).Next(3000, 8000);
                await Coroutine.Wait(randomWaitTime,
                    () => Core.Me.InCombat ||
                          !AvoidanceManager.Avoids.Any(o => o.IsPointInAvoid(Poi.Current.Location)));
            

                return false;
            });

            TreeHooks.Instance.InsertHook("TreeStart", 0, _sHook);
        }
        
        #region API

        /// <summary>
        /// This will scan the Sidestep Assembly to find AvoiderAttribute types and add them to our Avoider. 
        /// </summary>
        public void LoadAvoidanceObjects()
        {
            Logger.Verbose("Loading avoidance objects");
            
            _spellAvoiders.Clear();
            _omenAvoiders.Clear();
            _castTypeAvoiders.Clear();
            _worldAvoiders.Clear();
            _zoneAvoiders.Clear();

            using (new PerformanceLogger("LoadAvoidanceObjects"))
            {
                var types = Assembly.GetExecutingAssembly().GetTypes();
                foreach (var type in types)
                {
                    foreach (var method in type.GetMethods())
                    {
                        var attributes = method.GetCustomAttributes<AvoiderAttribute>().ToArray();
                        if (attributes.Length == 0) continue;

                        var firstAttr = attributes[0];
                        
                        // Determine if this is a MapEffect (World) or BattleCharacter handler based on the first attribute
                        if (firstAttr.Type == AvoiderType.WorldEvent)
                        {
                            try 
                            {
                                var del = (Func<MapEffect, float, IEnumerable<AvoidInfo>>)Delegate.CreateDelegate(typeof(Func<MapEffect, float, IEnumerable<AvoidInfo>>), method);
                                foreach (var atb in attributes)
                                {
                                    if (atb.Type != AvoiderType.WorldEvent) 
                                    {
                                        Logger.Warn($"Method {method.Name} has mixed AvoiderTypes which is not supported.");
                                        continue;
                                    }
                                    
                                    var handler = new MapEffectHandler { Method = del, Attribute = atb };
                                    if (!_worldAvoiders.TryAdd((atb.Zone, atb.Key), handler))
                                        Logger.Warn($"Duplicate world avoider key: {atb.Key} in zone {atb.Zone}");
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.Error($"Failed to bind World avoider {method.Name}: {ex.Message}");
                            }
                        }
                        else if(firstAttr.Type == AvoiderType.Zone)
                        {
                            try
                            {
                                var del = (Func<IEnumerable<AvoidInfo>>)Delegate.CreateDelegate(typeof(Func<IEnumerable<AvoidInfo>>), method);
                                foreach (var atb in attributes)
                                {
                                    if (atb.Type != AvoiderType.WorldEvent)
                                    {
                                        Logger.Warn($"Method {method.Name} has mixed AvoiderTypes which is not supported.");
                                        continue;
                                    }

                                    var handler = new ZoneHandler { Method = del, Attribute = atb };
                                    var vals = _zoneAvoiders.GetValueOrDefault(atb.Zone, new List<ZoneHandler>());
                                    vals.Add(handler);
                                    _zoneAvoiders[atb.Zone] = vals; 
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.Error($"Failed to bind World avoider {method.Name}: {ex.Message}");
                            }
                        }
                        else
                        {
                            try
                            {
                                var del = (Func<BattleCharacter, float, IEnumerable<AvoidInfo>>)Delegate.CreateDelegate(typeof(Func<BattleCharacter, float, IEnumerable<AvoidInfo>>), method);
                                foreach (var atb in attributes)
                                {
                                    var handler = new AvoidanceHandler { Method = del, Attribute = atb };
                                    switch (atb.Type)
                                    {
                                        case AvoiderType.Spell:
                                            if (!_spellAvoiders.TryAdd(atb.Key, handler))
                                                Logger.Warn($"Duplicate spell avoider key: {atb.Key}");
                                            break;
                                        case AvoiderType.Omen:
                                            if (!_omenAvoiders.TryAdd(atb.Key, handler))
                                                Logger.Warn($"Duplicate omen avoider key: {atb.Key}");
                                            break;
                                        case AvoiderType.CastType:
                                            if (!_castTypeAvoiders.TryAdd(atb.Key, handler))
                                                Logger.Warn($"Duplicate cast type avoider key: {atb.Key}");
                                            break;
                                        case AvoiderType.WorldEvent:
                                            Logger.Warn($"Skipping World attribute on BattleCharacter handler {method.Name}");
                                            break;
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.Error($"Failed to bind avoider {method.Name}: {ex.Message}");
                            }
                        }
                    }
                }
            }
            var count = _spellAvoiders.Count + _omenAvoiders.Count + _castTypeAvoiders.Count + _worldAvoiders.Count;
            Logger.Info("Loaded {0} avoidance objects", count);
        }

        /// <summary>
        /// Adds a delegate type to the avoidance creator. 
        /// </summary>
        /// <param name="avoiderType">This should map to an <see cref="AvoiderType"/></param>
        /// <param name="key">This is the key that is used to match a specific AvoiderType</param>
        /// <param name="handler"></param>
        /// <param name="range"></param>
        /// <exception cref="ArgumentException">Duplicate key for the AvoiderType / Key combo</exception>
        public void AddHandler(ulong avoiderType, uint key, Func<BattleCharacter, float, IEnumerable<AvoidInfo>> handler, float range = float.NaN)
        {
            var attribute = new AvoiderAttribute((AvoiderType)avoiderType, key, range);
            var h = new AvoidanceHandler { Method = handler, Attribute = attribute };
            
            bool added = false;
            switch (attribute.Type)
            {
                case AvoiderType.Spell:
                    added = _spellAvoiders.TryAdd(key, h);
                    break;
                case AvoiderType.Omen:
                    added = _omenAvoiders.TryAdd(key, h);
                    break;
                case AvoiderType.CastType:
                    added = _castTypeAvoiders.TryAdd(key, h);
                    break;
                case AvoiderType.WorldEvent:
                    throw new ArgumentException("Use AddHandler overload for World/MapEffect types");
            }

            if (!added)
            {
                 throw new ArgumentException($"Duplicate key for: {attribute.Type} - {attribute.Key}");
            }
            
            Logger.Info("Adding custom avoidance type {0} with key {1}", attribute.Type, key);
        }

        /// <summary>
        /// Adds a delegate type to the avoidance creator. 
        /// </summary>
        /// <param name="avoiderType">This should map to an <see cref="AvoiderType"/></param>
        /// <param name="key">This is the key that is used to match a specific AvoiderType</param>
        /// <param name="handler"></param>
        /// <param name="range"></param>
        /// <exception cref="ArgumentException">Duplicate key for the AvoiderType / Key combo</exception>
        public void AddHandler(ulong avoiderType, uint zone, uint key, Func<MapEffect, float, IEnumerable<AvoidInfo>> handler, float range = float.NaN)
        {
            var attribute = new AvoiderAttribute((AvoiderType)avoiderType, key, range);
            
            if (attribute.Type != AvoiderType.WorldEvent)
            {
                 throw new ArgumentException("This overload only supports World types");
            }

            var h = new MapEffectHandler { Method = handler, Attribute = attribute };
            
            // Defaulting zone to 0 as in previous logic, though World type should probably specify zone?
            var added = _worldAvoiders.TryAdd((zone, key), h);

            if (!added)
            {
                 throw new ArgumentException($"Duplicate key for: {attribute.Type} - {attribute.Key}");
            }
            
            Logger.Info("Adding custom world avoidance type {0} with key {1}", attribute.Type, key);
        }

        /// <summary>
        /// Remove a specific avoider type and key.
        /// You should not use this to remove any of the SideStep default avoids as you cannot add them back.
        /// </summary>
        /// <param name="avoiderType"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool RemoveHandler(ulong avoiderType, uint key)
        {
            var type = (AvoiderType)avoiderType;
            bool removed = false;
            
            Logger.Info("removing avoidance type {0} with key {1}", type, key);

            switch (type)
            {
                case AvoiderType.Spell:
                    removed = _spellAvoiders.Remove(key);
                    break;
                case AvoiderType.Omen:
                    removed = _omenAvoiders.Remove(key);
                    break;
                case AvoiderType.CastType:
                    removed = _castTypeAvoiders.Remove(key);
                    break;
                case AvoiderType.WorldEvent:
                    throw new ArgumentException("Use RemoveHandler overload for World/MapEffect types");
            }
            return removed;
        }

        public bool RemoveHandler(ulong avoiderType, uint zone, uint key)
        {
            var type = (AvoiderType)avoiderType;
            bool removed = false;
            
            Logger.Info("removing avoidance type {0} with key {1}", type, key);

            switch (type)
            {
                case AvoiderType.WorldEvent:
                    removed = _worldAvoiders.Remove((zone, key));
                    break;
                    
                case AvoiderType.Spell:
                    removed = _spellAvoiders.Remove(key);
                    break;
                case AvoiderType.Omen:
                    removed = _omenAvoiders.Remove(key);
                    break;
                case AvoiderType.CastType:
                    removed = _castTypeAvoiders.Remove(key);
                    break;
                
            }
            return removed;
        }
        #endregion
        

        private (MapEffectHandler?, MapEffect?) MapEffects(MapEffect arg)
        {
            var zoneId = WorldManager.ZoneId;
            var key = arg.ID;

            // Direct lookup O(1)
            var hasHandler = _worldAvoiders.TryGetValue((zoneId, key), out var handler);
            
            // we only support 1 handler for any given event
            // Avoiding LINQ: AvoidInfos.Any(...)
            bool am = false;
            foreach (var s in AvoidanceManager.AvoidInfos) 
            {
                 if (s.Collection.Contains((zoneId, AvoiderType.WorldEvent, key)))
                 {
                     am = true;
                     break;
                 }
            }

            if (am)
            {
                return (null, arg);
            }
            

            
            if(!hasHandler) {
                return (null, arg);
            }

            return (handler, arg);
        }



        private (AvoidanceHandler?, BattleCharacter?) IsValid(BattleCharacter c)
        {
            if (c.IsMe || c.CastingSpellId == 0)
                return (null, c);
            
            var log = false || !_loggedSpells.contains((c.NpcId, c.CastingSpellId));

            var oid = c.SpellCastInfo.SpellData.Omen;
            var spid = c.CastingSpellId;
            var cid = c.SpellCastInfo.SpellData.RawCastType;

            // Priority Check: Spell > Omen > CastType
            // Using O(1) dictionary lookups
            
            AvoidanceHandler? handler = null;
            
            if (_spellAvoiders.TryGetValue(spid, out var hSpell))
            {
                handler = hSpell;
            }
            else if (_omenAvoiders.TryGetValue(oid, out var hOmen))
            {
                handler = hOmen;
            }
            else if (_castTypeAvoiders.TryGetValue(cid, out var hCast))
            {
                handler = hCast;
            }

            // we only support 1 handler for any given spell
            // avoid LINQ
            var am = false;
            foreach (var s in AvoidanceManager.AvoidInfos)
            {
                 if (s.Collection.Contains(c))
                 {
                     am = true;
                     break;
                 }
            }

            if (log)
            {
                uint capableCount = 0;
                if (_spellAvoiders.ContainsKey(spid)) capableCount++;
                if (_omenAvoiders.ContainsKey(oid)) capableCount++;
                if (_castTypeAvoiders.ContainsKey(cid)) capableCount++;
                _loggedSpells.add((c.NpcId, c.CastingSpellId), capableCount);

                Logger.Info( $"[Detection] [Spell: {c.CastingSpellId}] [Omen: {oid}] [Raw Cast Type: {cid}] [Capable: {capableCount}] [avoidance manager: {am}]");
            }
            
            if (am)
            {
                return (null, c);
            }

            if (handler.HasValue)
            {
                if (log)
                {
                    var avoiderAttribute = handler.Value.Attribute;
                    Logger.Verbose(
                        $"{c.SpellCastInfo.SpellData.LocalizedName} [Type {avoiderAttribute.Type}][Id: {c.CastingSpellId}][Omen: {c.SpellCastInfo.SpellData.Omen}][RawCastType: {c.SpellCastInfo.SpellData.RawCastType}][ObjId: {c.ObjectId}]");
                }
                return (handler, c);
            }
            else
            {
                if (log)
                    Logger.Verbose(
                        $"No Avoid info for: {c.SpellCastInfo.SpellData.LocalizedName} [NpcID: {c.NpcId}][Id: {c.CastingSpellId}][Omen: {c.SpellCastInfo.SpellData.Omen}][RawCastType: {c.SpellCastInfo.SpellData.RawCastType}][ObjId: {c.ObjectId}]");
                return (null, c);
            }
        }
    }
}