/*
SideStep is licensed under a
Creative Commons Attribution-NonCommercial-ShareAlike 4.0 International License.
You should have received a copy of the license along with this
work. If not, see <http://creativecommons.org/licenses/by-nc-sa/4.0/>.
Orginal work done by zzi
                                                                                 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Buddy.Coroutines;
using ff14bot;
using ff14bot.AClasses;
using ff14bot.Behavior;
using ff14bot.Enums;
using ff14bot.Helpers;
using ff14bot.Managers;
using ff14bot.Navigation;
using ff14bot.Objects;
using ff14bot.Pathing.Avoidance;
using Sidestep.Helpers;
using Sidestep.Interfaces;
using Sidestep.Logging;
using TreeSharp;

namespace Sidestep
{

    public class Sidestep : BotPlugin
    {

        public override string Author => "ZZI";
        public override Version Version => new Version(6, 1, 2);
        public override string Name => "SideStep";
        public override bool WantButton => true;

        public override string ButtonText => "Clear";

        public override void OnButtonPress()
        {
            AvoidanceManager.RemoveAllAvoids(_ => true);
        }

        public override void OnInitialize()
        {
            LoadAvoidanceObjects();
        }
        public override void OnEnabled()
        {
            Logger.Verbose("Sidestep has been Enabled");

            TreeHooks.Instance.OnHooksCleared += ReHookAvoid;
            ReHookAvoid(null, null);
        }
        public override void OnDisabled()
        {
            Logger.Verbose("Sidestep has been Disabled");
            if (s_hook != null)
                TreeHooks.Instance.RemoveHook("TreeStart", s_hook);
            TreeHooks.Instance.OnHooksCleared -= ReHookAvoid;
        }

        private static ActionRunCoroutine s_hook;

        private static void ReHookAvoid(object sender, EventArgs e)
        {
            s_hook = new ActionRunCoroutine(async ctx =>
            {
                var poiType = Poi.Current.Type;

                // taken from HB
                // Special case: Bot will do a lot of fast stop n go when avoiding a mob that moves slowly and trying to
                // do something near the mob. To fix, a delay is added to slow down the 'Stop n go' behavior
                if (poiType == PoiType.Collect || poiType == PoiType.Gather || poiType == PoiType.Hotspot)
                {
                    if (Core.Me.InCombat && AvoidanceManager.Avoids.Any(o => o.IsPointInAvoid(Poi.Current.Location)))
                    {
                        TreeRoot.StatusText = "Waiting for 'avoid' to move before attempting to interact " +
                                              Poi.Current.Name;
                        var randomWaitTime = (new Random()).Next(3000, 8000);
                        await Coroutine.Wait(randomWaitTime,
                            () => Core.Me.InCombat ||
                                  !AvoidanceManager.Avoids.Any(o => o.IsPointInAvoid(Poi.Current.Location)));
                    }
                }
                return false;
            });

            TreeHooks.Instance.InsertHook("TreeStart", 0, s_hook);
        }

        public delegate IEnumerable<AvoidInfo> AvoidHandler(BattleCharacter spellCaster, float rangeOverride = Single.NaN);

        private Dictionary<AvoiderAttribute, AvoidHandler> _avoiders = new();
        private void LoadAvoidanceObjects()
        {
            using (new PerformanceLogger("LoadAvoidanceObjects"))
            {
                var funcs = Assembly.GetExecutingAssembly().GetTypes().SelectMany(f => f.GetMethods())
                    .Where(m => m.GetCustomAttributes(typeof(AvoiderAttribute), false).Length > 0);

                foreach (var type in funcs)
                {
                    var atbs = type.GetCustomAttributes<AvoiderAttribute>();
                    var del = (AvoidHandler) Delegate.CreateDelegate(typeof(AvoidHandler), type);
                    foreach (var atb in atbs)
                    {
                        if (!_avoiders.ContainsKey(atb))
                        {
                            _avoiders.Add(atb, del);
                        }
                        else
                        {
                            var existing = _avoiders[atb];
                            Logger.Warn($"Duplicate Sidestep key for: {atb.Type} - {atb.Key} -- Matching Delegate:? {existing == del}");
                        }
                    }
                }
            }
        }

        private readonly List<AvoidInfo> _tracked = new();
        
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
                var removable = _tracked.Where(i => !i.Condition() && AvoidanceManager.Avoids.All(z => z.AvoidInfo != i)).ToList();

                if (removable.Any())
                {
                    Logger.Info($"Removing {removable.Count} completed spells");
                    AvoidanceManager.RemoveAllAvoids(i => removable.Contains(i));
                    _tracked.RemoveAll(x => removable.Contains(x));
                }


                var newSpellCasts = GameObjectManager.GetObjectsOfType<BattleCharacter>()
                .Select(IsValid)
                .SelectMany(HandleNewCast)
                .ToList();

                if (newSpellCasts.Any())
                {
                    _tracked.AddRange(newSpellCasts);
                    //AvoidanceManager.AvoidInfos.AddRange(newSpellCasts);
                    Logger.Info($"Added: {newSpellCasts.Count()} to the avoidance manager");
                }
            }
        }

        private IEnumerable<AvoidInfo> HandleNewCast((IEnumerable<AvoiderAttribute>, BattleCharacter) BX )
        {
            var attributes = BX.Item1;
            var c = BX.Item2;

            if (c == null || !c.IsValid)
                return Array.Empty<AvoidInfo>();
            
            var avoiderAttributes = attributes as AvoiderAttribute[] ?? attributes.ToArray();

            if (avoiderAttributes.Length == 0)
                return Array.Empty<AvoidInfo>();
            
            AvoiderAttribute avoiderAttribute;
            
            if (avoiderAttributes.Any(t => t.Type == AvoiderType.Spell))
            {
                avoiderAttribute = avoiderAttributes.FirstOrDefault(t => t.Type == AvoiderType.Spell);
                Logger.Verbose($"{c.SpellCastInfo.SpellData.LocalizedName} [Spell][Id: {c.CastingSpellId}][Omen: {c.SpellCastInfo.SpellData.Omen}][RawCastType: {c.SpellCastInfo.SpellData.RawCastType}][ObjId: {c.ObjectId}]");
            }
            else if (avoiderAttributes.Any(t => t.Type == AvoiderType.Omen))
            {
                avoiderAttribute = avoiderAttributes.FirstOrDefault(t => t.Type == AvoiderType.Omen);
                Logger.Verbose($"{c.SpellCastInfo.SpellData.LocalizedName} [Omen][Id: {c.CastingSpellId}][Omen: {c.SpellCastInfo.SpellData.Omen}][RawCastType: {c.SpellCastInfo.SpellData.RawCastType}][ObjId: {c.ObjectId}]");
            }
            else if (avoiderAttributes.Any(t => t.Type == AvoiderType.CastType))
            {
                avoiderAttribute = avoiderAttributes.FirstOrDefault(t => t.Type == AvoiderType.CastType);
                Logger.Verbose($"{c.SpellCastInfo.SpellData.LocalizedName} [CastType][Id: {c.CastingSpellId}][Omen: {c.SpellCastInfo.SpellData.Omen}][RawCastType: {c.SpellCastInfo.SpellData.RawCastType}][ObjId: {c.ObjectId}]");
            }
            else
            {
                Logger.Verbose($"No Avoid info for: {c.SpellCastInfo.SpellData.LocalizedName} [None][Id: {c.CastingSpellId}][Omen: {c.SpellCastInfo.SpellData.Omen}][RawCastType: {c.SpellCastInfo.SpellData.RawCastType}][ObjId: {c.ObjectId}]");
                return Array.Empty<AvoidInfo>();
            }

            var handle = _avoiders[avoiderAttribute];
            
            return handle(c, avoiderAttribute.Range);
        }


        private (IEnumerable<AvoiderAttribute>, BattleCharacter) IsValid(BattleCharacter c)
        {

            if (c.IsMe)
                return (Array.Empty<AvoiderAttribute>(), c);

            if (c.CastingSpellId == 0)
                return (Array.Empty<AvoiderAttribute>(), c);

            var oid = c.SpellCastInfo.SpellData.Omen;
            var spid = c.CastingSpellId;
            var cid = c.SpellCastInfo.SpellData.RawCastType;
            
            var allowed = _avoiders.Keys.Where(s => 
                    s.Type == AvoiderType.Omen && s.Key == oid ||
                    s.Type == AvoiderType.Spell && s.Key == spid ||
                    s.Type == AvoiderType.CastType && s.Key == cid
            );
            
            var am = AvoidanceManager.AvoidInfos.Any(s => s.Collection.Contains(c));
            var avoiderAttributes = allowed as AvoiderAttribute[] ?? allowed.ToArray(); //prevent multiple enumeration
            
            Logger.Verbose($"[Detection] Detected Spell: {c.CastingSpellId} Capable: {avoiderAttributes.Length} && am: {am}");

            if(am)
            {
                return (Array.Empty<AvoiderAttribute>(), c);
            }
            
            return (avoiderAttributes, c);
        }
    }
}
