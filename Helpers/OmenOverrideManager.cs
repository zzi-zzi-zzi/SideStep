using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Clio.Utilities;

namespace Sidestep.Helpers
{
    public static class OmenOverrideManager
    {

        private static readonly Dictionary<uint, OmenOverride> OmenOverrides = new Dictionary<uint, OmenOverride>()
        {
            {6420, new OmenOverride(15f) },
        };

        
        static OmenOverrideManager()
        {

        }

        public static bool HasOverride(uint spellId)
        {
            return OmenOverrides.ContainsKey(spellId);
        }

        public static bool TryGetOverride(uint spellId, out OmenOverride omenOverride)
        {
            return OmenOverrides.TryGetValue(spellId,out omenOverride);
        }

        public class OmenOverride
        {

            public OmenOverride(float range)
            {
                RangeOverride = range;
            }

            public OmenOverride(Matrix44 m44)
            {
                MatrixOverride = m44;
            }

            public OmenOverride(float range, Matrix44 m44)
            {
                RangeOverride = range;
                MatrixOverride = m44;
            }


            public float? RangeOverride;
            public Matrix44? MatrixOverride;
        }


    }
}
