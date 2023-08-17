using System;
using System.Collections.Generic;
using ff14bot.Managers;
using ff14bot.Objects;
using ff14bot.Pathing.Avoidance;
using Sidestep.Common;
using Sidestep.Interfaces;
using Sidestep.Logging;

namespace Sidestep.Avoid;

public class Malikahs_Well
{
    [Avoider(AvoiderType.Spell, 15593)] // Malikah's Well - Flail Smash
    public static IEnumerable<AvoidInfo> TorusWithAvoidCenter(BattleCharacter spellCaster,
        float omenOverride = Single.NaN)
    {
        Logger.Info("~~~~~~~~~~~~~~~Flail Smash~~~~~~~~~~~~~~~");
        if (spellCaster.SpellCastInfo.SpellData.EffectRange > 45)
            Logger.Info("Spell range is > 45. Does this require specific logic?");
        //var loc = spellCaster.SpellCastInfo.CastLocation != Vector3.Zero ? spellCaster.SpellCastInfo.CastLocation : spellCaster.Location;

        var center = spellCaster.OmenMatrix.Center;
        var range = !float.IsNaN(omenOverride) ? omenOverride : spellCaster.Range(out center);


        Logger.Info($"Avoid Torus: [{center}][Range: {range}][Middle: {range / 2.5}]");
        var cached = spellCaster.CastingSpellId;
        var points = Omen.Torus(range / 2.5f, range);
        return new[]
        {
            AvoidanceManager.AddAvoidPolygon(
                () => spellCaster.IsValid && spellCaster.CastingSpellId == cached, //can run
                () => center, //LeashPoint
                50f, //Leash Radius
                bc => 0f, // Rotation
                bc => 1.0f, // Scale
                bc => 1.0f, //height
                bc => points, //radiusProducer
                bc => center, //locationProducer
                () => new[] { spellCaster } //collectionProducer
            ),
            AvoidanceManager.AddAvoidLocation(
                () => spellCaster.IsValid && spellCaster.CastingSpellId == cached, //can run
                c => c.CombatReach / 2, //radiusProducer
                c => center, //locationProducer
                () => new[] { spellCaster },
                c => c.IsValid && c.CastingSpellId == cached
            )
        };
    }
}