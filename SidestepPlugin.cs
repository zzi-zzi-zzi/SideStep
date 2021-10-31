/*
SideStep is licensed under a
Creative Commons Attribution-NonCommercial-ShareAlike 4.0 International License.
You should have received a copy of the license along with this
work. If not, see <http://creativecommons.org/licenses/by-nc-sa/4.0/>.
Orginal work done by zzi
                                                                                 */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using Buddy.Coroutines;
using Clio.Utilities;
using ff14bot;
using ff14bot.AClasses;
using ff14bot.Behavior;
using ff14bot.Enums;
using ff14bot.Helpers;
using ff14bot.Managers;
using ff14bot.Navigation;
using ff14bot.Objects;
using ff14bot.Overlay3D;
using ff14bot.Pathing;
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
        public override Version Version => new Version(3, 0);
        public override string Name => "SideStep";
        public override bool WantButton => true;
        private bool on = false;

        // public override void OnButtonPress()
        // {
        //    on = !on;
        //    Logger.Info("Visuals: {0}", on);
        //    if (on)
        //        Overlay3D.Drawing += ff14.Visualize;
        //    else
        //        Overlay3D.Drawing -= ff14.Visualize;
        // }



        public override void OnInitialize()
        {
            LoadAvoidanceObjects();
        }
        public override void OnEnabled()
        {
            Logger.Verbose("Sidestep has been Enabled");

            TreeHooks.Instance.OnHooksCleared += rehookavoid;
            rehookavoid(null, null);
        }
        public override void OnDisabled()
        {
            Logger.Verbose("Sidestep has been Disabled");
            if (s_hook != null)
                TreeHooks.Instance.RemoveHook("TreeStart", s_hook);
            TreeHooks.Instance.OnHooksCleared -= rehookavoid;
        }

        private static ActionRunCoroutine s_hook;

        private void rehookavoid(object sender, EventArgs e)
        {
            s_hook = new ActionRunCoroutine(async ctx =>
            {
                var supportsCapabilities = RoutineManager.Current.SupportedCapabilities != CapabilityFlags.None;

                if (AvoidanceManager.IsRunningOutOfAvoid && Core.Me.IsCasting)
                {
                    ActionManager.StopCasting();
                    return true;
                }

                if (AvoidanceManager.IsRunningOutOfAvoid && !supportsCapabilities)
                    return true;
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


        private Dictionary<ulong, IAvoider> _owners = new Dictionary<ulong, IAvoider>();

        private void LoadAvoidanceObjects()
        {
            var baseType = typeof(IAvoider);
            var types = Assembly.GetExecutingAssembly().GetTypes().Where(t => t != baseType && baseType.IsAssignableFrom(t));


            foreach (var type in types)
            {
                var atbs = type.GetCustomAttributes<AvoiderAttribute>();
                foreach (var atb in atbs)
                {
                    _owners.Add((ulong)atb.Type + atb.Key, (IAvoider)Activator.CreateInstance(type));
                }
            }
        }



        private readonly List<AvoidInfo> _tracked = new List<AvoidInfo>();

        public override void OnPulse()
        {
            //don't run if we don't have a navigation provider
            if (Navigator.NavigationProvider == null)
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
                .Where(IsValid)
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

        private IEnumerable<AvoidInfo> HandleNewCast(BattleCharacter battleCharacter)
        {
            if (_owners.TryGetValue((ulong)AvoiderType.Spell + battleCharacter.CastingSpellId, out var iAvoider))
            {
                Logger.Verbose($"{battleCharacter.SpellCastInfo.SpellData.LocalizedName} [Spell][Id: {battleCharacter.CastingSpellId}][Omen: {battleCharacter.SpellCastInfo.SpellData.Omen}][RawCastType: {battleCharacter.SpellCastInfo.SpellData.RawCastType}][ObjId: {battleCharacter.ObjectId}]");
            }
            else if (_owners.TryGetValue((ulong)AvoiderType.Omen + battleCharacter.SpellCastInfo.SpellData.Omen, out iAvoider))
            {
                Logger.Verbose($"{battleCharacter.SpellCastInfo.SpellData.LocalizedName} [Omen][Id: {battleCharacter.CastingSpellId}][Omen: {battleCharacter.SpellCastInfo.SpellData.Omen}][RawCastType: {battleCharacter.SpellCastInfo.SpellData.RawCastType}][ObjId: {battleCharacter.ObjectId}]");
            }
            else if (_owners.TryGetValue((ulong)AvoiderType.CastType + battleCharacter.SpellCastInfo.SpellData.RawCastType, out iAvoider))
            {
                Logger.Verbose($"{battleCharacter.SpellCastInfo.SpellData.LocalizedName} [CastType][Id: {battleCharacter.CastingSpellId}][Omen: {battleCharacter.SpellCastInfo.SpellData.Omen}][RawCastType: {battleCharacter.SpellCastInfo.SpellData.RawCastType}][ObjId: {battleCharacter.ObjectId}]");
            }
            else
            {
                Logger.Verbose($"No Avoid info for: {battleCharacter.SpellCastInfo.SpellData.LocalizedName} [None][Id: {battleCharacter.CastingSpellId}][Omen: {battleCharacter.SpellCastInfo.SpellData.Omen}][RawCastType: {battleCharacter.SpellCastInfo.SpellData.RawCastType}][ObjId: {battleCharacter.ObjectId}]");
                return new AvoidInfo[0];
            }

            return iAvoider.Handle(battleCharacter);
        }


        private static bool IsValid(BattleCharacter c)
        {

            //if (!c.InCombat)
            //    return false;

            if (c.IsMe)
                return false;

            //if (!c.StatusFlags.HasFlag(StatusFlags.Hostile))
            //    return false;

            if (c.CastingSpellId == 0)
                return false;


            //if (c.DistanceSqr() < 50 * 50)
            //{
            if (c.SpellCastInfo.SpellData.Omen != 0 || OmenOverrideManager.HasOverride(c.CastingSpellId))
            {
                if (!AvoidanceManager.AvoidInfos.Any(s => s.Collection.Contains(c)))
                {
                    return true;
                }
            }
            //}


            //var valid = c.InCombat && !c.IsMe && c.StatusFlags.HasFlag(StatusFlags.Hostile) && c.CastingSpellId != 0 &&
            //            c.SpellCastInfo.SpellData.Omen != 0 && //skip spells that don't have an omen
            //
            //            !AvoidanceManager.AvoidInfos.Any(s => s.Collection.Contains(c));
            return false;
        }
    }
}
