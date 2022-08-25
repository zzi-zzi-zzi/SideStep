/*
SideStep is licensed under a
Creative Commons Attribution-NonCommercial-ShareAlike 4.0 International License.
You should have received a copy of the license along with this
work. If not, see <http://creativecommons.org/licenses/by-nc-sa/4.0/>.
Orginal work done by zzi
                                                                                 */

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
    [Avoider(AvoiderType.CastType, 2)]
    [Avoider(AvoiderType.CastType, 5)]
    [Avoider(AvoiderType.CastType, 6)]
    [Avoider(AvoiderType.CastType, 10)]
    public class CircleGeneric : Omen
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
                range = Range(spellCaster, out center,omenOverride.MatrixOverride,omenOverride.RangeOverride);
            }
            else
            {
                range = Range(spellCaster, out center);
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
    
  // Some specific spells that need to be moved to their own file...
  [Avoider(AvoiderType.Spell, 31234)] // Body Slam - Handeling this as Torus due to the knockback. 
    public class BodySlam31234 : Omen
    {
        public override IEnumerable<AvoidInfo> OmenHandle(BattleCharacter spellCaster)
        {
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

            Logger.Info($"Body Slam: [{center}][Range: {range}][Middle: {range / 2.5}]");
            var cached = spellCaster.CastingSpellId;
            var points = Torus(range / 2.5f, 50);
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
