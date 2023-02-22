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
        public AvoiderType Type { get; set; }
        public uint Key { get; set; }

        public float Range { get; set; } = Single.NaN;

        public AvoiderAttribute(AvoiderType type, uint key)
        {
            Type = type;
            Key = key;
        }

        public override bool Equals(object obj)
        {
            var aa = obj as AvoiderAttribute;
            if (aa == null)
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
                int hashCode = base.GetHashCode();
                hashCode = (hashCode * 397) ^ Type.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)Key;
                hashCode = (hashCode * 397) ^ Range.GetHashCode();
                return hashCode;
            }
        }
    }
}