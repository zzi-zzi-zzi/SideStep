/*
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
using Sidestep.Helpers;
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
        [Avoider(AvoiderType.Spell, 6420, Range=15f)]
        [Avoider(AvoiderType.CastType, 6)]
        [Avoider(AvoiderType.CastType, 10)]
        public static IEnumerable<AvoidInfo> Handle(BattleCharacter spellCaster, float omenOverride = Single.NaN)
        {
            if(spellCaster.SpellCastInfo.SpellData.EffectRange > 45)
                Logger.Info("Spell range is > 45. Does this require specific logic?");
            //var loc = spellCaster.SpellCastInfo.CastLocation != Vector3.Zero ? spellCaster.SpellCastInfo.CastLocation : spellCaster.Location;

            Vector3 center;
            var range = 0f;
            if (!float.IsNaN(omenOverride))
            {
                range = spellCaster.Range(out center, null, omenOverride);
            }
            else
            {
                range = spellCaster.Range(out center);
            }

            Logger.Info($"Avoid Cirlce: [{center}][Range: {range}]");
            var cached = spellCaster.CastingSpellId;

            return new[]{ AvoidanceManager.AddAvoidLocation(
                () => spellCaster.IsValid && spellCaster.CastingSpellId == cached, //can run
                () => center, //LeashPoint
                50f, //Leash Radius
                bc => range + 0.5f, //radiusProducer
                bc => center, //locationProducer
                () => new[] {spellCaster} //collectionProducer
            ) };
            
        }
    }
}