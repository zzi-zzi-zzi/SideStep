using ff14bot.Managers;
using ff14bot.Objects;
using ff14bot.Pathing.Avoidance;
using Sidestep.Common;
using Sidestep.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace SideStep.Avoid.Dungeons._7._0
{
    internal class Worqor
    {
        private static volatile uint count = 0;

        [Avoider(AvoiderType.Spell, 36278)]
        public static IEnumerable<AvoidInfo> SnowballTest(BattleCharacter spellCaster, float rangeOverride)
        {
            var cached = spellCaster.CastingSpellId;
            var square = spellCaster.Square();
            
            var priority = AvoidancePriority.High;
            
            count += 1;
            if(count >= 5)
                priority = AvoidancePriority.Low;
            if (count >= 11)
                count = 0;

            return new[]{ AvoidanceManager.AddAvoidPolygon(
                () => spellCaster.IsValid && spellCaster.CastingSpellId == cached,
                null,
                40f,
                _ => 0f, //rotation
                _ => 1.0f, //scale
                _ => 15.0f, //height
                _ => square, //points
                _ => spellCaster.Location,
                () => new[] {spellCaster}, //objs,
                bc => bc.IsAlive,
                false,
                priority
            ) };

        }
    }
}
