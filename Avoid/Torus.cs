using System.Collections.Generic;
using Clio.Utilities;
using ff14bot.Managers;
using ff14bot.Objects;
using ff14bot.Pathing.Avoidance;
using Sidestep.Common;
using Sidestep.Helpers;
using Sidestep.Interfaces;
using Sidestep.Logging;

namespace Sidestep.Avoid
{
    [Avoider(AvoiderType.Spell, 664)] // The Howling Eye (Normal) - Eye of the Storm
    public class Torus : Omen
    {
        public override IEnumerable<AvoidInfo> OmenHandle(BattleCharacter spellCaster)
        {
            if(spellCaster.SpellCastInfo.SpellData.EffectRange > 45)
                Logger.Info("Spell range is > 45. Does this require specific logic?");
            //var loc = spellCaster.SpellCastInfo.CastLocation != Vector3.Zero ? spellCaster.SpellCastInfo.CastLocation : spellCaster.Location;

            Vector3 center;
            float range = 0f;
            if (OmenOverrideManager.TryGetOverride(spellCaster.SpellCastInfo.ActionId,out var omenOverride))
            {
                range = Range(spellCaster, out center, omenOverride.MatrixOverride, omenOverride.RangeOverride);
            }
            else
            {
                range = Range(spellCaster, out center);
            }

            
            Logger.Info($"Avoid Torus: [{center}][Range: {range}][Middle: {range / 2.5}]");
            var cached = spellCaster.CastingSpellId;
            var points = Torus(range / 2.5f, range);
            return new[]{ AvoidanceManager.AddAvoidPolygon(
                () => spellCaster.IsValid && spellCaster.CastingSpellId == cached, //can run
                () => center, //LeashPoint
                50f, //Leash Radius
                bc => 0f, // Rotation
                bc => 1.0f, // Scale
                bc => 1.0f, //height
                bc => points, //radiusProducer
                bc => center, //locationProducer
                () => new[] {spellCaster} //collectionProducer
            ) };
        }
    }
}