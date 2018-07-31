/*
SideStep is licensed under a
Creative Commons Attribution-NonCommercial-ShareAlike 4.0 International License.
You should have received a copy of the license along with this
work. If not, see <http://creativecommons.org/licenses/by-nc-sa/4.0/>.
Orginal work done by zzi
                                                                                 */
using ff14bot.Objects;
using ff14bot.Pathing.Avoidance;
using System.Collections.Generic;

namespace Sidestep.Interfaces
{
    internal interface IAvoider
    {
        IEnumerable<AvoidInfo> Handle(BattleCharacter spellCaster);
    }
}