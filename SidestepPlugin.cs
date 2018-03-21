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
using ff14bot.Objects;
using ff14bot.Overlay3D;
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

        //public override void OnButtonPress()
        //{
        //    on = !on;
        //    Logger.Info("Visuals: {0}", on);
        //    if (on)
        //        Overlay3D.Drawing += ff14.Visualize;
        //    else
        //        Overlay3D.Drawing -= ff14.Visualize;
        //}

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
            if(s_hook != null)
	            TreeHooks.Instance.RemoveHook("TreeStart", s_hook);
	        TreeHooks.Instance.OnHooksCleared -= rehookavoid;
        }

        private static ActionRunCoroutine s_hook;

        private void rehookavoid(object sender, EventArgs e)
        {
            s_hook = new ActionRunCoroutine(async ctx =>
            {
                var supportsCapabilities = RoutineManager.Current.SupportedCapabilities != CapabilityFlags.None;

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
	        var types = Assembly.GetExecutingAssembly().GetTypes().Where(t => t!=baseType && baseType.IsAssignableFrom(t));
	        

            foreach (var type in types)
	        {
	            var atbs = type.GetCustomAttributes<AvoiderAttribute>();
	            foreach (var atb in atbs)
	            {
	                _owners.Add((ulong)atb.Type + atb.Key, (IAvoider)Activator.CreateInstance(type));
                }
	        }
	    }

	    

	    private List <AvoidInfo> _tracked = new List<AvoidInfo>();

	    public override void OnPulse()
	    {
	        using (new PerformanceLogger("Pulse"))
	        {
                //remove tracked avoidances that have completed
	            var removeable = _tracked.Where(i => !i.Condition() && !AvoidanceManager.Avoids.Any(z => z.AvoidInfo == i)).ToList();

                if (removeable.Any())
	            {
	                Logger.Info($"Removing {removeable.Count()} completed spells");
	                AvoidanceManager.RemoveAllAvoids(i => removeable.Contains(i));
	                _tracked.RemoveAll(x => removeable.Contains(x));
	            }
	            var newSpellCasts = GameObjectManager.GetObjectsOfType<BattleCharacter>()
	                .Where(c => c.InCombat && !c.IsMe && c.StatusFlags.HasFlag(StatusFlags.Hostile) && c.CastingSpellId != 0 && IsNearby(c) &&
                                
                                c.SpellCastInfo.SpellData.Omen != 0 && //skip spells that don't have an omen

	                            !AvoidanceManager.AvoidInfos.Any(s => s.Collection.Contains(c)))
	                            .GroupBy(p => p.ObjectId)
                    .Select(o =>
	                {
	                    var c = o.First();
	                    if (_owners.ContainsKey((ulong) AvoiderType.Spell + c.CastingSpellId))
	                    {
	                        Logger.Verbose($"{c.SpellCastInfo.SpellData.LocalizedName} [Spell][Id: {c.CastingSpellId}][Omen: {c.SpellCastInfo.SpellData.Omen}][RawCastType: {c.SpellCastInfo.SpellData.RawCastType}][ObjId: {c.ObjectId}]");
                            return _owners[(ulong) AvoiderType.Spell + c.CastingSpellId].Handle(c);
	                    }

	                    if (_owners.ContainsKey((ulong) AvoiderType.Omen + c.SpellCastInfo.SpellData.Omen))
	                    {
	                        Logger.Verbose($"{c.SpellCastInfo.SpellData.LocalizedName} [Omen][Id: {c.CastingSpellId}][Omen: {c.SpellCastInfo.SpellData.Omen}][RawCastType: {c.SpellCastInfo.SpellData.RawCastType}][ObjId: {c.ObjectId}]");
                            return _owners[(ulong) AvoiderType.Omen + c.SpellCastInfo.SpellData.Omen].Handle(c);
	                    }

	                    if (_owners.ContainsKey((ulong) AvoiderType.CastType + c.SpellCastInfo.SpellData.RawCastType))
	                    {
	                        Logger.Verbose($"{c.SpellCastInfo.SpellData.LocalizedName} [CastType][Id: {c.CastingSpellId}][Omen: {c.SpellCastInfo.SpellData.Omen}][RawCastType: {c.SpellCastInfo.SpellData.RawCastType}][ObjId: {c.ObjectId}]");
                            return _owners[(ulong) AvoiderType.CastType + c.SpellCastInfo.SpellData.RawCastType].Handle(c);
	                    }

	                    Logger.Verbose($"No Avoid info for: {c.SpellCastInfo.SpellData.LocalizedName} [None][Id: {c.CastingSpellId}][Omen: {c.SpellCastInfo.SpellData.Omen}][RawCastType: {c.SpellCastInfo.SpellData.RawCastType}][ObjId: {c.ObjectId}]");
                        return null;
	                })
                    .Where(i => i != null).ToList();

	            if (newSpellCasts.Any())
	            {
	                _tracked.AddRange(newSpellCasts);
                    //AvoidanceManager.AvoidInfos.AddRange(newSpellCasts);
	                Logger.Info($"Added: {newSpellCasts.Count()} to the avoidance manager");
	            }
	        }
	    }

	  
	    private static bool IsNearby(GameObject character)
	    {
	        return character.Distance() < 50;
	    }
    }
}
