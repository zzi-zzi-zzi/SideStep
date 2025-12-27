/*
SideStep is licensed under a
Creative Commons Attribution-NonCommercial-ShareAlike 4.0 International License.
You should have received a copy of the license along with this
work. If not, see <http://creativecommons.org/licenses/by-nc-sa/4.0/>.
Original work done by zzi
                                                                                 */

using System;
using ff14bot.Managers;
using ff14bot.Objects;
using ff14bot.Pathing.Avoidance;
using Sidestep.Common;
using Sidestep.Interfaces;
using System.Collections.Generic;

namespace Sidestep.Avoid
{
    internal class Fans
    {
        //glfan20
        [Avoider(AvoiderType.Omen, 80, 20f)]
        [Avoider(AvoiderType.Omen, 38, 20f)]
        [Avoider(AvoiderType.Omen, 56, 20f)]//this isn't perfect, the aoe appears first but the monster will spin to face it and as it spins the avoid will move with it until it reaches the end
        [Avoider(AvoiderType.Omen, 146, 20f)]
        //glfan30
        [Avoider(AvoiderType.Omen, 99, 30f)]
        [Avoider(AvoiderType.Omen, 105, 30f)]
        //gl_fan060_1bf
        [Avoider(AvoiderType.Omen, 3, 60f)]
        [Avoider(AvoiderType.Omen, 98, 60f)]
        [Avoider(AvoiderType.Omen, 100, 60f)]
        [Avoider(AvoiderType.Omen, 159, 60f)]
        [Avoider(AvoiderType.Omen, 183, 60f)]
        //fan090
        
        [Avoider(AvoiderType.Omen, 4, 90f)]
        [Avoider(AvoiderType.Omen, 102, 90f)]
        [Avoider(AvoiderType.Omen, 163, 90f)]
        [Avoider(AvoiderType.Omen, 184, 90f)]
        
        //fan120
        
        [Avoider(AvoiderType.Omen, 5, 120f)]
        [Avoider(AvoiderType.Omen, 101, 120f)]
        [Avoider(AvoiderType.Omen, 120, 120f)]
        [Avoider(AvoiderType.Omen, 185, 120f)]
        
        //fan150
        [Avoider(AvoiderType.Omen, 28, 150f)]
        //fan180
        [Avoider(AvoiderType.Omen, 107, 180f)]
        //fan210
        [Avoider(AvoiderType.Omen, 128, 210f)]
        //fan270
        [Avoider(AvoiderType.Omen, 15, 270f)]
        [Avoider(AvoiderType.Omen, 16, 270f)]
        [Avoider(AvoiderType.Omen, 17, 270f)]
        public static IEnumerable<AvoidInfo> SuperFans(BattleCharacter spellCaster, float rangeOverride)
        {
            return spellCaster.AddCone(rangeOverride);
        }
        
        // Non Omened Fans
        //Stone Vigil
        [Avoider(AvoiderType.Spell, 903, 120f)]
        public static IEnumerable<AvoidInfo> ReallyLongBossFan(BattleCharacter spellCaster, float rangeOverride)
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

                //add something under the mob so we don't get hit by standing at the mobs' location.
                AvoidanceManager.AddAvoidLocation(
                    () => spellCaster.IsValid && spellCaster.CastingSpellId == cachedSpell, //can run
                    spellCaster.CombatReach / 2,
                    () => spellCaster.Location
                )
            };
        }
    }
}
