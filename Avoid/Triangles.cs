/*
SideStep is licensed under a
Creative Commons Attribution-NonCommercial-ShareAlike 4.0 International License.
You should have received a copy of the license along with this
work. If not, see <http://creativecommons.org/licenses/by-nc-sa/4.0/>.
Orginal work done by zzi
                                                                                 */

using System;
using ff14bot.Managers;
using ff14bot.Objects;
using ff14bot.Pathing.Avoidance;
using Sidestep.Common;
using Sidestep.Interfaces;
using System.Collections.Generic;
using System.Numerics;
using Clio.Common;
using Clio.Utilities;
using ff14bot;

namespace Sidestep.Avoid
{
    internal class Fans
    {
        //glfan20
        [Avoider(AvoiderType.Omen, 80, Range = 20f)]
        [Avoider(AvoiderType.Omen, 38, Range = 20f)]
        [Avoider(AvoiderType.Omen, 56, Range = 20f)]//this isn't perfect, the aoe appears first but the monster will spin to face it and as it spins the avoid will move with it until it reaches the end
        [Avoider(AvoiderType.Omen, 146, Range = 20f)]
        //glfan30
        [Avoider(AvoiderType.Omen, 99, Range = 30f)]
        [Avoider(AvoiderType.Omen, 105, Range = 30f)]
        //gl_fan060_1bf
        [Avoider(AvoiderType.Omen, 3, Range = 60f)]
        [Avoider(AvoiderType.Omen, 98, Range = 60f)]
        [Avoider(AvoiderType.Omen, 100, Range = 60f)]
        [Avoider(AvoiderType.Omen, 159, Range = 60f)]
        [Avoider(AvoiderType.Omen, 183, Range = 60f)]
        //fan090
        
        [Avoider(AvoiderType.Omen, 4, Range = 90f)]
        [Avoider(AvoiderType.Omen, 102, Range = 90f)]
        [Avoider(AvoiderType.Omen, 163, Range = 90f)]
        [Avoider(AvoiderType.Omen, 184, Range = 90f)]
        
        //fan120
        
        [Avoider(AvoiderType.Omen, 5, Range = 120f)]
        [Avoider(AvoiderType.Omen, 101, Range = 120f)]
        [Avoider(AvoiderType.Omen, 120, Range = 120f)]
        [Avoider(AvoiderType.Omen, 185, Range = 120f)]
        
        //fan150
        [Avoider(AvoiderType.Omen, 28, Range = 150f)]
        //fan180
        [Avoider(AvoiderType.Omen, 107, Range = 180f)]
        //fan210
        [Avoider(AvoiderType.Omen, 128, Range = 210f)]
        //fan270
        [Avoider(AvoiderType.Omen, 15, Range = 270f)]
        [Avoider(AvoiderType.Omen, 16, Range = 270f)]
        [Avoider(AvoiderType.Omen, 17, Range = 270f)]
        public static IEnumerable<AvoidInfo> SuperFans(BattleCharacter spellCaster, float rangeOverride = Single.NaN)
        {
            return spellCaster.AddCone(rangeOverride);
        }
        
        // Non Omened Fans
        //Stone Vigil
        [Avoider(AvoiderType.Spell, 903, Range = 120f)]
        public static IEnumerable<AvoidInfo> ReallyLongBossFan(BattleCharacter spellCaster, float rangeOverride = Single.NaN)
        {

            var loc = spellCaster.Location;
            var cachedSpell = spellCaster.CastingSpellId;

            return new[]
            {
                AvoidanceManager.AddAvoidUnitCone<BattleCharacter>(
                    () => spellCaster.IsValid && spellCaster.CastingSpellId == cachedSpell, //can run
                    bc => bc.ObjectId == spellCaster.ObjectId, //object selector
                    () => loc, //LeashPoint
                    50, //leash size
                    0, //rotation - same direction as the mob is facing.
                    45f, //radius / Depth
                    120f, //arcDegrees
                    _ => loc
                ),

                //add something under the mob so we don't get hit by standing at the mobs location.
                AvoidanceManager.AddAvoidLocation(
                    () => spellCaster.IsValid && spellCaster.CastingSpellId == cachedSpell, //can run
                    spellCaster.CombatReach / 2,
                    () => spellCaster.Location
                )
            };
        }

        public static void TestOperation()
        {
           
            try
            {
                var loc = Core.Target.Location;
                var tar = Core.Target.ObjectId;
                
              
                AvoidanceManager.Pulse();
            }
            catch (Exception ex)
            {
                Log("failed to make cone: {0}", ex);
            }
        }

        public static void Log(string m, params object[] data)
        {
            
        }
    }
}
