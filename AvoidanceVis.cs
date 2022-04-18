using Clio.Utilities;
using ff14bot.AClasses;
using ff14bot.Managers;
using ff14bot.Overlay3D;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;

namespace FoxDen.Plugin.AvoidShit
{
    class AvoidanceVis : BotPlugin
    {
        internal static PropertyInfo HeightFieldProperty;

        internal static Type CompactHeightFieldType;

        internal static PropertyInfo SpansProp;
        internal static PropertyInfo CellProp;

        internal static PropertyInfo CellSize;
        internal static PropertyInfo CellHeight;

        internal static PropertyInfo NumCellsZ;
        internal static PropertyInfo NumCellsX;

        internal static PropertyInfo Bounds;

        internal static Type CellType;
        internal static Type SpansType;

        internal static MethodInfo GetCell;

        //cell types
        internal static PropertyInfo CellIndexProp;
        internal static PropertyInfo CellCountProp;

        //spans Type
        internal static PropertyInfo SpansAreaProp;

        internal static FieldInfo SpansBottomField;

        public override string Author => "Fox Den";

        public override string Name => "Avoid shit";

        public override Version Version => new Version(1,0,0,0);


        static AvoidanceVis()
        {
            HeightFieldProperty = typeof(AvoidanceManager).GetProperty("Heightfield", BindingFlags.Static | BindingFlags.NonPublic);
            CompactHeightFieldType = HeightFieldProperty.PropertyType;
            SpansProp = CompactHeightFieldType.GetProperty("Spans");
            CellProp = CompactHeightFieldType.GetProperty("Cells");

            CellSize = CompactHeightFieldType.GetProperty("CellSize");
            CellHeight = CompactHeightFieldType.GetProperty("CellHeight");

            NumCellsZ = CompactHeightFieldType.GetProperty("NumCellsZ");
            NumCellsX = CompactHeightFieldType.GetProperty("NumCellsX");

            Bounds = CompactHeightFieldType.GetProperty("Bounds");

            CellType = CellProp.PropertyType.GetElementType();

            SpansType = SpansProp.PropertyType.GetElementType();

            GetCell = CompactHeightFieldType.GetMethods()
                .First(i => i.ReturnType == CellType && i.GetParameters().Length == 2);

            CellIndexProp = CellType.GetProperty("Index");
            CellCountProp = CellType.GetProperty("Count");

            SpansAreaProp = SpansType.GetProperty("Area");
            SpansBottomField = SpansType.GetFields().First(i => i.FieldType == typeof(ushort));

        }

        public override void OnEnabled()
        {
            Overlay3D.Drawing += Draw;
            
        }

        public override void OnDisabled()
        {
            Overlay3D.Drawing -= Draw;
        }

        void Draw(object sender, DrawingEventArgs e)
        {
            if (HeightFieldProperty.GetValue(null) == null)
                return;

            var dd = e.Drawer;


            var heightField = HeightFieldProperty.GetValue(null);
            float cs = (float)CellSize.GetValue(heightField);
            float ch = (float)CellHeight.GetValue(heightField);
            Array Spans = (Array)SpansProp.GetValue(heightField);

            BoundingBox3 bounds = (BoundingBox3)Bounds.GetValue(heightField);
            //var cpl = Core.Player.Location;

            var td = new Dictionary<Color, List<Vector3>>();
            
            for (int y = 0; y < (int)NumCellsZ.GetValue(heightField); ++y)
            {
                for (int x = 0; x < (int)NumCellsX.GetValue(heightField); ++x)
                {


                    float fx = bounds.Min.X + x * cs;
                    float fz = bounds.Min.Z + y * cs;
                    var c = GetCell.Invoke(heightField, new object[] { x, y }); // chf.GetCell(x, y);

                    uint Index = (uint)CellIndexProp.GetValue(c);
                    uint Count = (uint)CellCountProp.GetValue(c);

                    for (uint i = Index, ni = Index + Count; i < ni; ++i)
                    {
                        var s = Spans.GetValue(i);


                        var area = (byte)SpansAreaProp.GetValue(s);
                        ushort bottom = (ushort)SpansBottomField.GetValue(s);

                        Color color;
                        if (area == 1)
                            color = Color.FromArgb(128, 128, 255, 64);
                        else if (area == 0)
                            color = Color.FromArgb(128, 0, 0, 64);
                        else
                            color = duIntToCol(area, 64);

                        float fy = bounds.Min.Y + (bottom + 1) * ch;

                        if (!td.TryGetValue(color, out var tl))
                        {
                            tl = new List<Vector3>();
                            td[color] = tl;
                        }
                        tl.AddRange(new[]
                        {
                            new Vector3(fx, fy, fz),
                            new Vector3(fx, fy, fz + cs),
                            new Vector3(fx + cs, fy, fz + cs),
                            new Vector3(fx + cs, fy, fz + cs),
                            new Vector3(fx + cs, fy, fz),
                            new Vector3(fx, fy, fz)
                        });
                    }
                }
            }
            
            foreach(var k in td)
                dd.DrawTriangles(k.Value.ToArray(), k.Key);

            
        }

        static int bit(int a, int b)
        {
            return (a & (1 << b)) >> b;
        }
        private static Color duIntToCol(byte i, int a)
        {
            var r = bit(i, 1) + bit(i, 3) * 2 + 1;
            var g = bit(i, 2) + bit(i, 4) * 2 + 1;
            var b = bit(i, 0) + bit(i, 5) * 2 + 1;
            return Color.FromArgb(r * 63, g * 63, b * 63, a);
        }
    }
}
