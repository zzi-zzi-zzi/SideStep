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
using ff14bot;
using ff14bot.AClasses;
using ff14bot.Behavior;
using ff14bot.Directors;
using ff14bot.Enums;
using ff14bot.Managers;
using ff14bot.Navigation;
using ff14bot.NeoProfiles;
using ff14bot.Objects;
using Sidestep.Helpers;
using SideStep.Helpers;
using TreeSharp;
using Logger = Sidestep.Logging.ELogger;

namespace Sidestep
{
    // ReSharper disable once UnusedType.Global
    public class EventLoggerPlugin : BotPlugin
    {
        public override string Author => "ZZI";
        public override Version Version => new Version(7, 4, 0);
        public override string Name => "EventLogger";
        public override string Description => "Loggs common combat events to the console. You should only enable this plugin to debug avoidance. ";
        public override bool WantButton => true;

        public override string ButtonText => "Clear";
        
        
        private readonly LRUCache<(uint, uint), uint> _loggedSpells = new(20);
        private readonly Dictionary<uint, ulong> _loggedWorldStates = new();
        // ObjID - VfxIDs
        private readonly Dictionary<uint, HashSet<uint>> _loggedVFX = new();
        private static ActionRunCoroutine? _sHook;
        private List<uint> _party;



        #region Plugin Settings



        public override void OnButtonPress() => Clear();


        public override void OnEnabled()
        {
            Logger.Verbose("EventLogger has been Enabled");
            TreeRoot.OnStart += OnStart;
            TreeRoot.OnStop += OnStop;
            GameEvents.OnMapChanged += OnMapChanged;
        }

        private void OnMapChanged(object? sender, EventArgs e) => Clear();

        private void OnStop(BotBase bot) => Clear();

        private void OnStart(BotBase bot) => Clear();
        
        private void Clear()
        {
            _loggedWorldStates.Clear();
            _loggedVFX.Clear();
            _party = PartyManager.AllMembers.Select(k => k.ObjectId).ToList();
        }

        public override void OnDisabled()
        {
            Logger.Verbose("EventLogger has been Disabled");
            if (_sHook != null)
                TreeHooks.Instance.RemoveHook("TreeStart", _sHook);

            TreeRoot.OnStart -= OnStart;
            TreeRoot.OnStop -= OnStop;
            GameEvents.OnMapChanged -= OnMapChanged;

            Clear();
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

                // spells
                foreach (var obj in GameObjectManager.GetObjectsOfType<BattleCharacter>().Where(bc => !_party.Contains(bc.ObjectId)))
                {
                    IsValid(obj);
                }
                
                // world events
                if (DirectorManager.ActiveDirector != null && DirectorManager.ActiveDirector.IsValid && DirectorManager.ActiveDirector is InstanceContentDirector ic)
                {
                    // MapEffects is a collection, avoid LINQ
                    foreach (var effect in ic.MapEffects)
                    {
                        MapEffects(effect);
                    }
                }

            }
        }

        #endregion



        private void MapEffects(MapEffect arg)
        {
            var zoneId = WorldManager.ZoneId;
            var key = arg.ID;

            // Assuming State is 16-bit and Flags is 8-bit
            var packed = ((ulong)arg.unk << 24) |      // Move 32-bit unk to bits 24-55
                         ((ulong)(arg.State & 0xFFFF) << 8) | // Move 16-bit State to bits 8-23
                         (ushort)(arg.Flags & 0xFF);    // Place 8-bit Flags in bits 0-7

            var log = _loggedWorldStates.GetValueOrDefault(arg.ID, (uint)0) != packed;
            
            if (log)
            {
                _loggedWorldStates[arg.ID] = packed;
                Logger.Info( $"[World Detection] [ID: {arg.ID}] [State: {arg.State}] [Flags: {arg.Flags}] [Unk: {arg.unk}]");
            }
            
        }



        private void IsValid(BattleCharacter c)
        {
            if (c.IsDead)
            {
                _loggedVFX.Remove(c.ObjectId);
                return;
            }
            
            var old = _loggedVFX.GetValueOrDefault(c.ObjectId, new HashSet<uint> {  });

            var nvfx = new HashSet<uint>();
            var name = "name";
            
            if (c.IsNpc)
                name = c.Name;



            var log = false || !_loggedSpells.contains((c.ObjectId, c.CastingSpellId));
            

            // only log hostile spellcasts on targets
            if (log && !c.IsMe && c.CastingSpellId > 0 && c.StatusFlags.HasFlag(StatusFlags.Hostile))
            {
                var oid = c.SpellCastInfo.SpellData.Omen;
                var spid = c.CastingSpellId;
                var cid = c.SpellCastInfo.SpellData.RawCastType;
                _loggedSpells.add((c.ObjectId, c.CastingSpellId), c.NpcId);

                Logger.Info( $"[Spell Detection] [npc: {c.NpcId}] [Obj: {c.ObjectId}] [Name: {name}] [Spell: {c.CastingSpellId}] [Spell Name: {c.SpellCastInfo.Name}] [Omen: {oid}] [Raw Cast Type: {cid}]");
            }


            if (c.VfxContainer.IsValid && (c.StatusFlags.HasFlag(StatusFlags.Hostile) || c.IsMe))
            {
                for (var index = 0; index < c.VfxContainer.Vfx.Length; index++)
                {
                    var vfx = c.VfxContainer.Vfx[index];
                    if (vfx == null) continue;

                    if (!vfx.IsValid) continue;
                    nvfx.Add(vfx.Id);

                    if (old.Contains(vfx.Id))
                    {
                        continue;
                    }



                    Logger.Info($"[VFX Detection] [npc: {c.NpcId}] [Obj: {c.ObjectId}] [Name: {name}] [Vfx: {vfx.Id}] [Slot: {SlotName(index)}]");
                }
                _loggedVFX[c.ObjectId] = nvfx;
            }


        }

        /// <summary>
        /// This needs to be updated as the game updates
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        private string SlotName(int index)
        {
            switch (index)
            {
                case 0:
                    return "Spell - 0";
                case 6:
                case 7:
                    return $"Omen - 0x{index:X}";
                case 10:
                case 11:
                case 12:
                    return $"Lockon - 0x{index:X}";
                

                default:
                    return $"Unknown - 0x{index:X}";
            }
        }
    }
}