/*
SideStep is licensed under a
Creative Commons Attribution-NonCommercial-ShareAlike 4.0 International License.
You should have received a copy of the license along with this
work. If not, see <http://creativecommons.org/licenses/by-nc-sa/4.0/>.
Orginal work done by zzi
                                                                                 */
using System;
using System.Threading.Tasks;
using Clio.Common;
using Clio.Utilities;
using ff14bot;
using ff14bot.Helpers;
using ff14bot.Managers;
using ff14bot.Objects;
using ff14bot.Pathing.Avoidance;
using Sidestep.Common;
using Sidestep.Interfaces;
using Sidestep.Helpers;
using System.Collections.Generic;

namespace Sidestep.Avoid
{
    /// <summary>
    /// lazer beam type
    /// </summary>
    
    [Avoider(AvoiderType.CastType, 12)] //found in Eureka
    [Avoider(AvoiderType.Spell, 9198)] //Found in Ala Mhigo (Dungeon) - Cast by 12th Legion Roader on spawn before the first boss. 
    [Avoider(AvoiderType.CastType, 4)]
    public class LazerGeneric : Omen
    {

        //public async Task<bool> Handle(SpellCast spellCaster)
        //{

        //    var res = inside(spellCaster.CasterLocation, spellCaster.CasterHeading, spellCaster.Data.EffectRange,
        //        spellCaster.Data.XAxisModified > 0 ? spellCast.Data.XAxisModified : spellCaster.Caster.CombatReach);
        //    if (res.Item1)
        //    {
        //        await Help.MoveTo(res.Item2, spellCaster);
        //    }
        //    return Core.Me.IsMelee();
        //}

        public override IEnumerable<AvoidInfo> OmenHandle(BattleCharacter spellCaster)
        {
            var cached = spellCaster.CastingSpellId;
            //var rotation = Rotation(spellCaster);
            //var cl = spellCaster.SpellCastInfo.CastLocation;
            var square = Square(spellCaster);

            return new[]{ AvoidanceManager.AddAvoidPolygon(
                () => spellCaster.IsValid && spellCaster.CastingSpellId == cached,
                null,
                40f,
                bc => 0f, //rotation
                bc => 1.0f, //scale
                bc => 15.0f, //height
                bc => square, //points
                bc => spellCaster.Location,
                () => new[] {spellCaster} //objs
            ) };


        }
        
    }

    [Avoider(AvoiderType.Omen, 188)]
    public class Cross : Omen
    {
        public override IEnumerable<AvoidInfo> OmenHandle(BattleCharacter spellCaster)
        {
            var cached = spellCaster.CastingSpellId;
            var square = Square(spellCaster);
            var result = new List<AvoidInfo>();

            result.Add(AvoidanceManager.AddAvoidPolygon(
                () => spellCaster.IsValid && spellCaster.CastingSpellId == cached,
                null,
                40f,
                bc => 0f, //rotation
                bc => 1.0f, //scale
                bc => 15.0f, //height
                bc => square, //points
                bc => spellCaster.Location,
                () => new[] { spellCaster } //objs
            ));

            result.Add(AvoidanceManager.AddAvoidPolygon(
                () => spellCaster.IsValid && spellCaster.CastingSpellId == cached,
                null,
                40f,
                bc => (float)Math.PI, //rotation
                bc => 1.0f, //scale
                bc => 15.0f, //height
                bc => square, //points
                bc => spellCaster.Location,
                () => new[] { spellCaster } //objs
            ));
            result.Add(AvoidanceManager.AddAvoidPolygon(
                () => spellCaster.IsValid && spellCaster.CastingSpellId == cached,
                null,
                40f,
                bc => - (float)Math.PI / 2, //rotation
                bc => 1.0f, //scale
                bc => 15.0f, //height
                bc => square, //points
                bc => spellCaster.Location,
                () => new[] { spellCaster } //objs
            ));
            result.Add(AvoidanceManager.AddAvoidPolygon(
                () => spellCaster.IsValid && spellCaster.CastingSpellId == cached,
                null,
                40f,
                bc => (float)Math.PI / 2, //rotation
                bc => 1.0f, //scale
                bc => 15.0f, //height
                bc => square, //points
                bc => spellCaster.Location,
                () => new[] { spellCaster } //objs
            ));


            return result;
        }
    }
}