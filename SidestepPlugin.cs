/*
SideStep is licensed under a
Creative Commons Attribution-NonCommercial-ShareAlike 4.0 International License.
You should have received a copy of the license along with this
work. If not, see <http://creativecommons.org/licenses/by-nc-sa/4.0/>.
Original work done by zzi
                                                                                 */

#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
        
        
        private readonly HashSet<(uint, uint)> _loggedSpells = new();
        private readonly Dictionary<uint, uint> _loggedWorldStates = new();
        private static ActionRunCoroutine? _sHook;
        private readonly Dictionary<AvoiderAttribute, Func<BattleCharacter, float, IEnumerable<AvoidInfo>>> _avoiders = new();
        private readonly List<AvoidInfo> _tracked = new();
        
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
            TreeRoot.OnStart += OnStart;
            TreeRoot.OnStop += OnStop;
            GameEvents.OnMapChanged += OnMapChanged;
            TreeHooks.Instance.OnHooksCleared += ReHookAvoid;
            ReHookAvoid(null, null);
        }

        private void OnMapChanged(object? sender, EventArgs e) => Clear();

        private void OnStop(BotBase bot) => Clear();

        private void OnStart(BotBase bot) => Clear();
        
        private void Clear()
        {
            LoadAvoidanceObjects();
            _tracked.Clear();
            _loggedSpells.Clear();
            _loggedWorldStates.Clear();
        }

        public override void OnDisabled()
        {
            Logger.Verbose("Sidestep has been Disabled");
            if (_sHook != null)
                TreeHooks.Instance.RemoveHook("TreeStart", _sHook);

            TreeHooks.Instance.OnHooksCleared -= ReHookAvoid;
            TreeRoot.OnStart -= OnStart;
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
                //remove tracked avoidances that have completed
                var removable = _tracked
                    .Where(i => !i.Condition() && AvoidanceManager.Avoids.All(z => z.AvoidInfo != i)).ToList();

                if (removable.Count != 0)
                {
                    Logger.Info($"Removing {removable.Count} completed spells");
                    AvoidanceManager.RemoveAllAvoids(i => removable.Contains(i));
                    _tracked.RemoveAll(x => removable.Contains(x));
                }
                
                // spells
                var newSpellCasts = GameObjectManager.GetObjectsOfType<BattleCharacter>()
                    .Select(IsValid)
                    .SelectMany(HandleNewCast)
                    .ToList();
                
                var count = newSpellCasts.Count;

                if (newSpellCasts.Count != 0)
                {
                    _tracked.AddRange(newSpellCasts);
                }
                
                // world events
                if (DirectorManager.ActiveDirector != null && DirectorManager.ActiveDirector.IsValid && DirectorManager.ActiveDirector is InstanceContentDirector ic)
                {
                    var newWorldCast = ic.MapEffects
                        .Select(MapEffects)
                        .SelectMany(HandleNewWorld).ToList();
                    
                    if (newWorldCast.Count != 0)
                    {
                        _tracked.AddRange(newWorldCast);
                    }
                    count += newWorldCast.Count;
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
            _avoiders.Clear();
            using (new PerformanceLogger("LoadAvoidanceObjects"))
            {
                var funcs = Assembly.GetExecutingAssembly().GetTypes().SelectMany(f => f.GetMethods())
                    .Where(m => m.GetCustomAttributes(typeof(AvoiderAttribute), false).Length > 0);

                foreach (var type in funcs)
                {
                    var atbs = type.GetCustomAttributes<AvoiderAttribute>();
                    var del = (Func<BattleCharacter, float, IEnumerable<AvoidInfo>>)Delegate.CreateDelegate(typeof(Func<BattleCharacter, float, IEnumerable<AvoidInfo>>), type);
                    foreach (var atb in atbs)
                    {
                        if (!_avoiders.TryGetValue(atb, out var existing))
                        {
                            _avoiders.Add(atb, del);
                        }
                        else
                        {
                            Logger.Warn(
                                $"Duplicate Sidestep key for: {atb.Type} - {atb.Key} -- Matching Delegate:? {existing == del}");
                        }
                    }
                }
            }
            Logger.Info("Loaded {0} avoidance objects", _avoiders.Count);
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
            if (_avoiders.Keys.Any(k => k.Type == attribute.Type && k.Key == attribute.Key))
            {
                throw new ArgumentException($"Duplicate key for: {attribute.Type} - {attribute.Key}");
            }
            Logger.Info("Adding custom avoidance type {0} with key {0}", attribute.Type, key);
            _avoiders.Add(attribute, handler);
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
            var attribute = new AvoiderAttribute((AvoiderType)avoiderType, key);
            var avoiderKey = _avoiders.Keys.FirstOrDefault(k => k.Type == attribute.Type && k.Key == attribute.Key);
            if (avoiderKey == null) return false;
            
            Logger.Info("removing avoidance type {0} with key {0}", attribute.Type, key);
            _avoiders.Remove(avoiderKey);
            return true;
        }
        #endregion
        

        private (AvoiderAttribute?, MapEffect?) MapEffects(MapEffect arg)
        {
            var avoiderAttributes = _avoiders.Keys.Where(s => 
                s.Type == AvoiderType.World && s.Key == arg.ID && s.Zone == WorldManager.ZoneId
            ).ToArray();
            
            // we only support 1 handler for any given event
            var am = AvoidanceManager.AvoidInfos.Any(s => s.Collection.Contains((WorldManager.ZoneId, AvoiderType.World, arg.ID)));
            if (am)
            {
                return (null, arg);
            }
            
            var packed = (uint)(((arg.State & 0xFFFF) << 8) | arg.Flags);
            var log = _loggedWorldStates.GetValueOrDefault(arg.ID, (uint)0) != packed;
            
            if (log)
            {
                _loggedWorldStates[arg.ID] = packed;
                Logger.Info( $"[Detection] [World] [ID: {arg.ID}] [State: {arg.State}] [Flags: {arg.Flags}] [Unk: {arg.unk}]");
            }
            

            return (avoiderAttributes.First(), arg);
        }

        private IEnumerable<AvoidInfo> HandleNewWorld((AvoiderAttribute?, MapEffect?) arg)
        {
            //var ai = arg.Item1;
            //var me =  arg.Item2;
            //if (ai == null || me == null)
            //    return Array.Empty<AvoidInfo>();

            //var handle = _avoiders[ai];

            //return handle(me, ai.Range);
            return Array.Empty<AvoidInfo>();
        }
        
        private IEnumerable<AvoidInfo> HandleNewCast((AvoiderAttribute?, BattleCharacter?) bx)
        {
            var avoiderAttribute = bx.Item1;
            var c = bx.Item2;

            if (avoiderAttribute == null || c == null || !c.IsValid)
                return Array.Empty<AvoidInfo>();

            var handle = _avoiders[avoiderAttribute];

            return handle(c, avoiderAttribute.Range);
        }

        private (AvoiderAttribute?, BattleCharacter?) IsValid(BattleCharacter c)
        {
            if (c.IsMe || c.CastingSpellId == 0)
                return (null, c);
            
            var log = false || _loggedSpells.Add((c.NpcId, c.CastingSpellId));

            var oid = c.SpellCastInfo.SpellData.Omen;
            var spid = c.CastingSpellId;
            var cid = c.SpellCastInfo.SpellData.RawCastType;

            var avoiderAttributes = _avoiders.Keys.Where(s =>
                s.Type == AvoiderType.Omen && s.Key == oid ||
                s.Type == AvoiderType.Spell && s.Key == spid ||
                s.Type == AvoiderType.CastType && s.Key == cid
            ).ToArray();

            // we only support 1 handler for any given spell
            var am = AvoidanceManager.AvoidInfos.Any(s => s.Collection.Contains(c));

            if (log)
            {
                Logger.Verbose( $"[Detection] [Spell: {c.CastingSpellId}] [Omen: {oid}] [Raw Cast Type: {cid}] [Capable: {avoiderAttributes.Length}] [avoidance manager: {am}]");
            }
            
            if (am)
            {
                return (null, c);
            }

            AvoiderAttribute? avoiderAttribute;

            if (avoiderAttributes.Any(t => t.Type == AvoiderType.Spell))
            {
                avoiderAttribute = avoiderAttributes.FirstOrDefault(t => t.Type == AvoiderType.Spell);
                if (log)
                    Logger.Verbose(
                        $"{c.SpellCastInfo.SpellData.LocalizedName} [Type 0: Spell][Id: {c.CastingSpellId}][Omen: {c.SpellCastInfo.SpellData.Omen}][RawCastType: {c.SpellCastInfo.SpellData.RawCastType}][ObjId: {c.ObjectId}]");
            }
            else if (avoiderAttributes.Any(t => t.Type == AvoiderType.Omen))
            {
                avoiderAttribute = avoiderAttributes.FirstOrDefault(t => t.Type == AvoiderType.Omen);
                if (log)
                    Logger.Verbose(
                        $"{c.SpellCastInfo.SpellData.LocalizedName} [Type 1: Omen][Id: {c.CastingSpellId}][Omen: {c.SpellCastInfo.SpellData.Omen}][RawCastType: {c.SpellCastInfo.SpellData.RawCastType}][ObjId: {c.ObjectId}]");
            }
            else if (avoiderAttributes.Any(t => t.Type == AvoiderType.CastType))
            {
                avoiderAttribute = avoiderAttributes.FirstOrDefault(t => t.Type == AvoiderType.CastType);
                if (log)
                    Logger.Verbose(
                        $"{c.SpellCastInfo.SpellData.LocalizedName} [Type 2: RawCastType][Id: {c.CastingSpellId}][Omen: {c.SpellCastInfo.SpellData.Omen}][RawCastType: {c.SpellCastInfo.SpellData.RawCastType}][ObjId: {c.ObjectId}]");
            }
            else
            {
                if (log)
                    Logger.Verbose(
                        $"No Avoid info for: {c.SpellCastInfo.SpellData.LocalizedName} [NpcID: {c.NpcId}][Id: {c.CastingSpellId}][Omen: {c.SpellCastInfo.SpellData.Omen}][RawCastType: {c.SpellCastInfo.SpellData.RawCastType}][ObjId: {c.ObjectId}]");
                return (null, c);
            }

            return (avoiderAttribute, c);
        }
    }
}