using ff14bot.Objects;
using ff14bot.Pathing.Avoidance;

namespace Sidestep.Interfaces
{
    internal interface IAvoider
    {
        AvoidInfo Handle(BattleCharacter spellCaster);
    }
}