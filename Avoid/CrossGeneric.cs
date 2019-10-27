using System;
using System.Collections.Generic;
using ff14bot.Managers;
using ff14bot.Objects;
using ff14bot.Pathing.Avoidance;
using Sidestep.Common;
using Sidestep.Interfaces;
using Sidestep.Logging;

namespace Sidestep.Avoid
{
    [Avoider(AvoiderType.Omen, 188)]
    public class CrossGeneric : Omen
    {
        public override IEnumerable<AvoidInfo> OmenHandle(BattleCharacter spellCaster)
        {
            var cached = spellCaster.CastingSpellId;
            //var rotation = Rotation(spellCaster);
            //var cl = spellCaster.SpellCastInfo.CastLocation;
            var square = Square(spellCaster);
            var result = new List<AvoidInfo>();
            var range = Range(spellCaster, out var center);

            Logger.Info($"Avoid Cross: [{center}][Range: {range}] EffectiveRange [{spellCaster.SpellCastInfo.SpellData.EffectRange}]");

            result.Add(AvoidanceManager.AddAvoidPolygon(
                () => spellCaster.IsValid && spellCaster.CastingSpellId == cached,
                null,
                40f,
                bc => bc.Heading, //rotation
                bc => 1.0f, //scale
                bc => 15.0f, //height
                bc => square, //points
                bc => spellCaster.Location,
                () => new[] {spellCaster} //objs
            ));

            result.Add(AvoidanceManager.AddAvoidPolygon(
                () => spellCaster.IsValid && spellCaster.CastingSpellId == cached,
                null,
                40f,
                bc => bc.Heading - (float) Math.PI, //rotation
                bc => 1.0f, //scale
                bc => 15.0f, //height
                bc => square, //points
                bc => spellCaster.Location,
                () => new[] {spellCaster} //objs
            ));
            result.Add(AvoidanceManager.AddAvoidPolygon(
                () => spellCaster.IsValid && spellCaster.CastingSpellId == cached,
                null,
                40f,
                bc => bc.Heading - (float) Math.PI / 2, //rotation
                bc => 1.0f, //scale
                bc => 15.0f, //height
                bc => square, //points
                bc => spellCaster.Location,
                () => new[] {spellCaster} //objs
            ));
            result.Add(AvoidanceManager.AddAvoidPolygon(
                () => spellCaster.IsValid && spellCaster.CastingSpellId == cached,
                null,
                40f,
                bc => bc.Heading + (float) Math.PI / 2, //rotation
                bc => 1.0f, //scale
                bc => 15.0f, //height
                bc => square, //points
                bc => spellCaster.Location,
                () => new[] {spellCaster} //objs
            ));

            //Add circle right under mob
            result.Add(AvoidanceManager.AddAvoidLocation(
                () => spellCaster.IsValid && spellCaster.CastingSpellId == cached, //can run
                3f,
                () => spellCaster.Location
            ));


            return result.ToArray();
        }
    }
}