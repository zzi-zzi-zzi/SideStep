/*
SideStep is licensed under a
Creative Commons Attribution-NonCommercial-ShareAlike 4.0 International License.
You should have received a copy of the license along with this
work. If not, see <http://creativecommons.org/licenses/by-nc-sa/4.0/>.
Orginal work done by zzi
                                                                                 */
using System;
using System.Windows.Forms.VisualStyles;

namespace Sidestep.Interfaces
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public class AvoiderAttribute : Attribute
    {
        public AvoiderType Type;
        public uint Key;

        public AvoiderAttribute(AvoiderType type, uint key)
        {
            Type = type;
            Key = key;
        }
    }
}