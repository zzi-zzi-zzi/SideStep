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

                var rad = (float)Math.Round(MathEx.NormalizeRadian(rot - spellCaster.Heading), 2);

                var me = Core.Me.Location;
                Logger.Info("Debug: Rotation: {0} vs Mob heading: {1} = {2}", rot, spellCaster.Heading, rad);


                return new[]
                {
                    AvoidanceManager.AddAvoidUnitCone<BattleCharacter>(
                        () => spellCaster.IsValid && spellCaster.CastingSpellId == cachedSpell, //can run
                        bc => bc.ObjectId == spellCaster.ObjectId, //object selector
                        () => me, //LeashPoint
                        120, //leash size
                        rad, //rotation
                        depth, //radius / Depth
                        arcDegrees * 1.55f, //arcDegrees
                        bc => bc.Location
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