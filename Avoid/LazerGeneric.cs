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

namespace Sidestep.Avoid
{
    /// <summary>
    /// lazer beam type
    /// </summary>
    
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

        public override AvoidInfo OmenHandle(BattleCharacter spellCaster)
        {
            var cached = spellCaster.CastingSpellId;
            //var rotation = Rotation(spellCaster);
            //var cl = spellCaster.SpellCastInfo.CastLocation;
            var square = Square(spellCaster);

            return AvoidanceManager.AddAvoidPolygon(
                () => spellCaster.IsValid && spellCaster.CastingSpellId == cached,
                null,
                40f,
                bc => 0f, //rotation
                bc => 1.0f, //scale
                bc => 15.0f, //height
                bc => square, //points
                bc => spellCaster.Location,
                () => new[] {spellCaster} //objs
            );


        }
        
    }
}