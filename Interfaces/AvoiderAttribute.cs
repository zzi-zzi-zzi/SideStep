/*
SideStep is licensed under a
Creative Commons Attribution-NonCommercial-ShareAlike 4.0 International License.
You should have received a copy of the license along with this
work. If not, see <http://creativecommons.org/licenses/by-nc-sa/4.0/>.
Original work done by zzi
                                                                                 */

using System;

namespace Sidestep.Interfaces
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class AvoiderAttribute : Attribute
    {
        public AvoiderType Type { get; }
        public uint Key { get; }

        public float Range { get; }

        public AvoiderAttribute(AvoiderType type, uint key, float range = float.NaN)
        {
            Type = type;
            Key = key;
            Range = range;
        }

        public override bool Equals(object? obj)
        {
            if (obj is not AvoiderAttribute aa)
                return false;
            return aa.Type == Type && aa.Key == Key;
        }

        protected bool Equals(AvoiderAttribute other)
        {
            return base.Equals(other) && Type == other.Type && Key == other.Key;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = base.GetHashCode();
                hashCode = (hashCode * 397) ^ Type.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)Key;
                hashCode = (hashCode * 397) ^ Range.GetHashCode();
                return hashCode;
            }
        }
    }
}