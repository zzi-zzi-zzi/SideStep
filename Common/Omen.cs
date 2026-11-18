/*
SideStep is licensed under a
Creative Commons Attribution-NonCommercial-ShareAlike 4.0 International License.
You should have received a copy of the license along with this
work. If not, see <http://creativecommons.org/licenses/by-nc-sa/4.0/>.
Original work done by zzi
                                                                                 */

using System;
using System.Collections.Generic;
using System.Linq;
using Clio.Common;
using Clio.Utilities;
using ff14bot;
using ff14bot.Managers;
using ff14bot.Objects;
using ff14bot.Pathing.Avoidance;
using Sidestep.Logging;

namespace Sidestep.Common
{
    public static class Omen
    {
        private static readonly Vector3 One = new Vector3(0, 0, 1);

        /// <summary>
        /// gets the range information using the omen data
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static float Range(this BattleCharacter spellCaster, out Vector3 center, Matrix44? forcedMatrix = null)
        {
            center = Vector3.Zero;
            try
            {
                var m4X4 = forcedMatrix ?? spellCaster.OmenMatrix;

                center = m4X4.Center;
                m4X4.Transform(One, out var edge);

                return center.Distance2D(edge);
            }
            catch (Exception ex)
            {
                Logger.Warn(ex.ToString());
                return spellCaster.SpellCastInfo.SpellData.EffectRange + spellCaster.CombatReach;
            }
        }


        /// <summary>
        /// makes a donut thing...
        /// </summary>
        /// <param name="outerRadius"></param>
        /// <param name="innerRadius"></param>
        /// <param name="pointCount"></param>
        /// <returns></returns>
        public static Vector2[] Torus(double outerRadius, double innerRadius, int pointCount = 64)
        {
            List<Vector2> outerPoints = new((pointCount * 2) + 1);
            List<Vector2> innerPoints = new(pointCount + 1);

            var tau = 2.0 * Math.PI; // No official Math.Tau before .NET 5
            var step = tau / pointCount;

            for (double theta = 0; theta < tau; theta += step)
            {
                outerPoints.Add(new Vector2((float)(outerRadius * Math.Cos(theta)),
                    (float)(outerRadius * Math.Sin(theta))));
                innerPoints.Add(new Vector2((float)(innerRadius * Math.Cos(theta)),
                    (float)(innerRadius * Math.Sin(theta))));
            }

            return outerPoints.Concat(innerPoints).ToArray();
        }

        /// <summary>
        /// Points of a cone with its tip at the origin, opening towards +Y.
        /// </summary>
        /// <param name="radius">How far the cone reaches</param>
        /// <param name="arcRadians">The full angle of the cone</param>
        /// <returns></returns>
        public static Vector2[] Cone(float radius, float arcRadians)
        {
            List<Vector2> points = new() { new Vector2(0, 0) };

            var halfArc = arcRadians / 2;
            var step = MathEx.DegreesToRadians(5);
            for (var angle = -halfArc; angle < halfArc; angle += step)
            {
                points.Add(new Vector2((float)(radius * Math.Sin(angle)), (float)(radius * Math.Cos(angle))));
            }

            points.Add(new Vector2((float)(radius * Math.Sin(halfArc)), (float)(radius * Math.Cos(halfArc))));
            return points.ToArray();
        }

        public static Vector2[] Square(this BattleCharacter spellCaster)
        {
            try
            {
                var m4x4 = spellCaster.OmenMatrix;

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

        public static IEnumerable<AvoidInfo> AddCone(this BattleCharacter spellCaster, float arcDegrees,
            Matrix44? forcedMatrix = null)
        {
            try
            {
                var cachedSpell = spellCaster.CastingSpellId;
                var m4x4 = forcedMatrix ?? spellCaster.OmenMatrix;

                //sin(0), 0, cos(0)
                m4x4.Transform(new Vector3(0, 0, 1), out var transformed);
                var center = m4x4.Center;

                var depth = transformed.Distance2D(center);
                var d = transformed - center;

                var rot = MathEx.Rotation(d);

                var me = Core.Me.Location;
                Logger.Info("Debug: Rotation: {0} vs Mob heading: {1}", rot, spellCaster.Heading);

                // The marker on the ground is where the attack lands. It can point away from the way the
                // mob faces, and it can stay put while the mob turns, so the cone follows the marker and
                // not the mob's heading.
                var direction = rot;
                var points = Cone(depth, MathEx.DegreesToRadians(Math.Min(360f, arcDegrees * 1.55f)));
                var height = Math.Min(30f, Math.Max(16f, depth));

                return new[]
                {
                    AvoidanceManager.AddAvoidPolygon<BattleCharacter>(
                        () => spellCaster.IsValid && spellCaster.CastingSpellId == cachedSpell, //can run
                        () => me, //LeashPoint
                        120, //leash size
                        bc =>
                        {
                            try
                            {
                                var current = forcedMatrix ?? bc.OmenMatrix;
                                current.Transform(One, out var edge);
                                var towards = edge - current.Center;
                                if (towards.X != 0 || towards.Z != 0)
                                    direction = MathEx.Rotation(towards);
                            }
                            catch (Exception)
                            {
                                // the marker could not be read: keep the direction it last had
                            }

                            return -MathEx.NormalizeRadian(direction);
                        }, //rotation
                        bc => 1.0f, //scale
                        bc => height, //height
                        bc => points, //points
                        bc => bc.Location, //locationProducer
                        () => new[] { spellCaster }, //collectionProducer
                        bc => bc != null && bc.IsValid //objectValidator
                    ),

                    AvoidanceManager.AddAvoidLocation(
                        () => spellCaster.IsValid && spellCaster.CastingSpellId == cachedSpell, //can run
                        c => c.CombatReach / 2, //radiusProducer
                        c => c.Location, //locationProducer
                        () => new[] { spellCaster },
                        c => c.IsValid && c.CastingSpellId == cachedSpell
                    )
                };
            }
            catch (Exception ex)
            {
                Logger.Error("failed to make cone: {0}", ex);
                return null;
            }
        }
    }
}