using Clio.Utilities;

namespace Sidestep.Common;

public static class M4X4Ext
{
    
    public static Vector2 Transform2d(this Matrix44 matrix, Vector2 from)
    {
        var transformed = new Vector2();
        transformed.X = from.X * matrix.M00 +
                        //from.Y * matrix.M10 +
                        from.Y * matrix.M20
            //+
            //matrix.M30
            ;

        transformed.Y = from.X * matrix.M02 +
                        //from.Y * matrix.M12 +
                        from.Y * matrix.M22
            //+
            //matrix.M32
            ;

        return transformed;

    }
    public static void Transform(this Matrix44 matrix, Vector3 from, out Vector3 transformed)
    {
        transformed = new Vector3();

        transformed.X = from.X * matrix.M00 +
                        from.Y * matrix.M10 +
                        from.Z * matrix.M20 +
                        matrix.M30;

        transformed.Y = from.X * matrix.M01 +
                        from.Y * matrix.M11 +
                        from.Z * matrix.M21 +
                        matrix.M31;

        transformed.Z = from.X * matrix.M02 +
                        from.Y * matrix.M12 +
                        from.Z * matrix.M22 +
                        matrix.M32;
    }
}