using System;
using System.Numerics;
using Clio.Common;
using Clio.Utilities;
using ff14bot;
using ff14bot.Helpers;
using ff14bot.Managers;
using ff14bot.Objects;
using ff14bot.Pathing.Avoidance;
using GreyMagic;
using Sidestep.Interfaces;
using Sidestep.Logging;
using Vector2 = Clio.Utilities.Vector2;
using Vector3 = Clio.Utilities.Vector3;

namespace Sidestep.Common
{
    public abstract class Omen : IAvoider
    {
        private Vector3 One = new Vector3(1, 0, 1);

        /// <summary>
        /// gets the range information using the omen data
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public float Range(BattleCharacter spellCaster, out Vector3 center)
        {
            center = Vector3.Zero;
            try
            {

                var m4x4 = spellCaster.GetProjection();

                center = m4x4.Center();
                m4x4.Transform(One, out var edge);

                return center.Distance2D(edge);

            }
            catch (Exception ex)
            {
                Logger.Warn(ex.ToString());
                return spellCaster.SpellCastInfo.SpellData.EffectRange + spellCaster.CombatReach;
            }
        }

       

        public Vector2[] Square(BattleCharacter spellCaster)
        {
            try
            {
                var m4x4 = spellCaster.GetProjection();

                //m4x4.Transform(ref One, out var transformed);
                return new[]
                {
                    m4x4.Transform2d(new Vector2(-1, 1)),
                    m4x4.Transform2d(new Vector2(1, 1)),
                    m4x4.Transform2d(new Vector2(1, 0)),
                    m4x4.Transform2d(new Vector2(-1, 0))
                };
            }
            catch (Exception ex)
            {
                Logger.Warn(ex.ToString());
                return new Vector2[0];
            }
        }

        public AvoidInfo AddCone(BattleCharacter spellCaster, float arcDegrees)
        {
            try
            {
                var cachedSpell = spellCaster.CastingSpellId;
                var m4x4 = spellCaster.GetProjection();

                //sin(0), 0, cos(0)
                m4x4.Transform(new Vector3(0, 0, 1), out var transformed);
                var center = m4x4.Center();
               
                var depth = transformed.Distance2D(center);
                var d = transformed - center;
                
                var rot = MathEx.ToDegrees(MathEx.Rotation(d));

                Logger.Info("Debug: Rotation: {0} vs Mob heading: {1}", rot, MathEx.ToDegrees(spellCaster.Heading));

               return AvoidanceManager.AddAvoidUnitCone<BattleCharacter>(
                    () => spellCaster.IsValid && spellCaster.CastingSpellId == cachedSpell,
                    bc => bc.ObjectId == spellCaster.ObjectId,
                    () => center, //LeashPoint
                    40f,
                    rot, //rotation
                    depth, //radius / Depth
                    arcDegrees + 5, //arcDegrees
                    bc => bc.Location
                );
            }
            catch (Exception ex)
            {
                Logger.Error("failed to make cone: {0}", ex);
                return null;
            }
        }

        public AvoidInfo Handle(BattleCharacter spellCaster)
        {
            if (spellCaster.OmenProjectionPtr() == IntPtr.Zero)
            {
                Logger.Info("Cast contains no Projection data.");
                return null;
            }
            return OmenHandle(spellCaster);
        }

        public abstract AvoidInfo OmenHandle(BattleCharacter spellCaster);
    }

    public static class m4x4Ext
    {
        private static int OmenOffset; // 1540
        private static int OmenProjection; //1b8

        private static int mtx = 0x20;

        
        static m4x4Ext()
        {
            var pf = new PatternFinder(Core.Memory);
            OmenOffset = pf.Find("Search 48 89 BB ? ? ? ? F3 0F 11 8B ? ? ? ? Add 3 Read32").ToInt32();
            OmenProjection = pf.Find("Search 48 8B 99 ? ? ? ? 48 85 DB 74 40 0F B6 8B ? ? ? ? Add 3 Read32").ToInt32();
        }

        public static IntPtr OmenProjectionPtr(this BattleCharacter bc)
        {
            return Core.Memory.Read<IntPtr>(bc.Pointer + OmenOffset);
        }

        public static Matrix44 GetProjection(this BattleCharacter bc)
        {
            return Core.Memory.Read<Matrix44>(
                Core.Memory.Read<IntPtr>(Core.Memory.Read<IntPtr>(bc.Pointer + OmenOffset) + OmenProjection) +
                mtx);
        }

        public static Vector3 Center(this Matrix44 matrix)
        {
            return new Vector3(matrix.M30, matrix.M31, matrix.M32);
        }

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
}