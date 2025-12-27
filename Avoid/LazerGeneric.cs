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
    /// <summary>
    /// lazer beam type
    /// </summary>
    
    public class Lazer
    {
        
        [Avoider(AvoiderType.CastType, 12)] //found in Eureka
        [Avoider(AvoiderType.Spell, 9198)] //Found in Ala Mhigo (Dungeon) - Cast by 12th Legion Roader on spawn before the first boss. 
        [Avoider(AvoiderType.CastType, 4)]
        public static IEnumerable<AvoidInfo> LazerGeneric(BattleCharacter spellCaster, float rangeOverride)
        {
            var cached = spellCaster.CastingSpellId;
            //var rotation = Rotation(spellCaster);
            //var cl = spellCaster.SpellCastInfo.CastLocation;
            var square = spellCaster.Square();

            return new[]{ AvoidanceManager.AddAvoidPolygon(
                () => spellCaster.IsValid && spellCaster.CastingSpellId == cached,
                null,
                40f,
                _ => 0f, //rotation
                _ => 1.0f, //scale
                _ => 15.0f, //height
                _ => square, //points
                _ => spellCaster.Location,
                () => new[] {spellCaster} //objs
            ) };

        }
        
        [Avoider(AvoiderType.Omen, 188)]
        public static IEnumerable<AvoidInfo> Cross(BattleCharacter spellCaster, float rangeOverride)
        {
            var cached = spellCaster.CastingSpellId;
            var square = spellCaster.Square();
            var result = new List<AvoidInfo>();

            result.Add(AvoidanceManager.AddAvoidPolygon(
                () => spellCaster.IsValid && spellCaster.CastingSpellId == cached,
                null,
                40f,
                _ => 0f, //rotation
                _ => 1.0f, //scale
                _ => 15.0f, //height
                _ => square, //points
                _ => spellCaster.Location,
                () => new[] { spellCaster } //objs
            ));

            result.Add(AvoidanceManager.AddAvoidPolygon(
                () => spellCaster.IsValid && spellCaster.CastingSpellId == cached,
                null,
                40f,
                _ => (float)Math.PI, //rotation
                _ => 1.0f, //scale
                _ => 15.0f, //height
                _ => square, //points
                _ => spellCaster.Location,
                () => new[] { spellCaster } //objs
            ));
            result.Add(AvoidanceManager.AddAvoidPolygon(
                () => spellCaster.IsValid && spellCaster.CastingSpellId == cached,
                null,
                40f,
                _ => - (float)Math.PI / 2, //rotation
                _ => 1.0f, //scale
                _ => 15.0f, //height
                _ => square, //points
                _ => spellCaster.Location,
                () => new[] { spellCaster } //objs
            ));
            result.Add(AvoidanceManager.AddAvoidPolygon(
                () => spellCaster.IsValid && spellCaster.CastingSpellId == cached,
                null,
                40f,
                _ => (float)Math.PI / 2, //rotation
                _ => 1.0f, //scale
                _ => 15.0f, //height
                _ => square, //points
                _ => spellCaster.Location,
                () => new[] { spellCaster } //objs
            ));


            return result;
        }
        
    }
}