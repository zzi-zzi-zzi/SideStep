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