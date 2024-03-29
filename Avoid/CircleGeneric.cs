﻿/*
SideStep is licensed under a
Creative Commons Attribution-NonCommercial-ShareAlike 4.0 International License.
You should have received a copy of the license along with this
work. If not, see <http://creativecommons.org/licenses/by-nc-sa/4.0/>.
Orginal work done by zzi
                                                                                 */

using System;
using System.Collections.Generic;
using Clio.Utilities;
using ff14bot.Managers;
using ff14bot.Objects;
using ff14bot.Pathing.Avoidance;
using Sidestep.Common;
using Sidestep.Interfaces;
using Sidestep.Logging;

namespace Sidestep.Avoid
{
    /// <summary>
    /// AOE Around an object
    /// </summary>
    public class CircleGeneric
    {
        [Avoider(AvoiderType.CastType, 2)]
        [Avoider(AvoiderType.CastType, 5)]
        [Avoider(AvoiderType.Spell, 6420, Range = 15f)]
        [Avoider(AvoiderType.CastType, 6)]
        public static IEnumerable<AvoidInfo> Handle(BattleCharacter spellCaster, float omenOverride = Single.NaN)
        {
            if (spellCaster.SpellCastInfo.SpellData.EffectRange > 45)
                Logger.Info("Spell range is > 45. Does this require specific logic?");
            //var loc = spellCaster.SpellCastInfo.CastLocation != Vector3.Zero ? spellCaster.SpellCastInfo.CastLocation : spellCaster.Location;

            var center = spellCaster.OmenMatrix.Center;
            var range = !float.IsNaN(omenOverride) ? omenOverride : spellCaster.Range(out center);

            if (center == Vector3.Zero && range == 0)
                return Array.Empty<AvoidInfo>();

            Logger.Info(
                $"Avoid Circle: [{center}][Range: {range}] [{spellCaster.CastingSpellId}][Override: {omenOverride}]");
            var cached = spellCaster.CastingSpellId;

            return new[]
            {
                AvoidanceManager.AddAvoidLocation(
                    () => spellCaster.IsValid && spellCaster.CastingSpellId == cached, //can run
                    () => center, //LeashPoint
                    50f, //Leash Radius
                    bc => range + 0.5f, //radiusProducer
                    bc => center, //locationProducer
                    () => new[] { spellCaster } //collectionProducer
                )
            };
        }

        [Avoider(AvoiderType.Spell, 31234)] // Body Slam - Handling this as Torus due to the knock back. 
        public static IEnumerable<AvoidInfo> BodySlam31234(BattleCharacter spellCaster, float _ = Single.NaN)
        {
            var range = spellCaster.Range(out var center);

            Logger.Info($"Body Slam: [{center}][Range: {range}][Middle: {range / 2.5}]");
            var cached = spellCaster.CastingSpellId;
            var points = Omen.Torus(range / 2.5f, 50);
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
                )
            };
        }
    }
}