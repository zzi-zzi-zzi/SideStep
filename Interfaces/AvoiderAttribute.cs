/*
SideStep is licensed under a
Creative Commons Attribution-NonCommercial-ShareAlike 4.0 International License.
You should have received a copy of the license along with this
work. If not, see <http://creativecommons.org/licenses/by-nc-sa/4.0/>.
Orginal work done by zzi
                                                                                 */
using System;

namespace Sidestep.Interfaces
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class AvoiderAttribute : Attribute
    {
        public AvoiderType Type{ get; set; }
        public uint Key{ get; set; }
        public AvoiderAttribute(AvoiderType type, uint key)
        {
            Type = type;
            Key = key;
        }

        public float Range { get; set; } = Single.NaN;
    }
}