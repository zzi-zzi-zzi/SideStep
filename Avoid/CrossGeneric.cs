using System;
using System.Collections.Generic;
using System.Linq;
using Clio.Utilities;
using ff14bot.Managers;
using ff14bot.Objects;
using ff14bot.Pathing.Avoidance;
using Sidestep.Common;
using Sidestep.Interfaces;
using Sidestep.Logging;

namespace Sidestep.Avoid
{

    [Avoider(AvoiderType.Omen, 188)]
    public class CrossGeneric : Omen
    {
        
        public override IEnumerable<AvoidInfo> OmenHandle(BattleCharacter spellCaster)
        {
            var cached = spellCaster.CastingSpellId;
            //var rotation = Rotation(spellCaster);
            //var cl = spellCaster.SpellCastInfo.CastLocation;
            var square = Square(spellCaster);
            var result = new List<AvoidInfo>();
            var range = Range(spellCaster, out var center);
            
            Logger.Info($"Avoid Cross: [{center}][Range: {range}] EffectiveRange [{spellCaster.SpellCastInfo.SpellData.EffectRange}]");

            result.Add(AvoidanceManager.AddAvoidPolygon(
                () => spellCaster.IsValid && spellCaster.CastingSpellId == cached,
                null,
                40f,
                bc => bc.Heading, //rotation
                bc => 1.0f, //scale
                bc => 15.0f, //height
                bc => square, //points
                bc => spellCaster.Location,
                () => new[] {spellCaster} //objs
            ) );
            
            result.Add(AvoidanceManager.AddAvoidPolygon(
                () => spellCaster.IsValid && spellCaster.CastingSpellId == cached,
                null,
                40f,
                bc =>  bc.Heading - (float)Math.PI, //rotation
                bc => 1.0f, //scale
                bc => 15.0f, //height
                bc => square, //points
                bc => spellCaster.Location,
                () => new[] {spellCaster} //objs
            ) );
            result.Add(AvoidanceManager.AddAvoidPolygon(
                () => spellCaster.IsValid && spellCaster.CastingSpellId == cached,
                null,
                40f,
                bc =>  bc.Heading - ((float)Math.PI/2), //rotation
                bc => 1.0f, //scale
                bc => 15.0f, //height
                bc => square, //points
                bc => spellCaster.Location,
                () => new[] {spellCaster} //objs
            ) );
            result.Add(AvoidanceManager.AddAvoidPolygon(
                () => spellCaster.IsValid && spellCaster.CastingSpellId == cached,
                null,
                40f,
                bc =>  bc.Heading + ((float)Math.PI/2), //rotation
                bc => 1.0f, //scale
                bc => 15.0f, //height
                bc => square, //points
                bc => spellCaster.Location,
                () => new[] {spellCaster} //objs
            ) );
            
            //Add circle right under mob
            result.Add(AvoidanceManager.AddAvoidLocation(
                () => spellCaster.IsValid && spellCaster.CastingSpellId == cached, //can run
                3f,
                () => spellCaster.Location
            ));
            
            
            
/*            result.Add(AddAvoidLine(
                () => spellCaster.IsValid && spellCaster.CastingSpellId == cached,
                x => x.Location,
                x => x.Heading,
                x => 15f,
                x => 20f,
                () => new[] {spellCaster}, //can be created more than once?
                () => GameObjectManager.GetObjectByNPCId(6037).Location,
                40f
            ));
            
            result.Add(AddAvoidLine(
                () => spellCaster.IsValid && spellCaster.CastingSpellId == cached,
                x => x.Location,
                x => x.Heading - (float)Math.PI,
                x => 15f,
                x => 20f,
                () => new[] {spellCaster}, //can be created more than once?
                () => spellCaster.Location,
                40f
            ));
            
            result.Add(AddAvoidLine(
                () => spellCaster.IsValid && spellCaster.CastingSpellId == cached,
                x => x.Location,
                x => x.Heading - ((float)Math.PI/2),
                x => 15f,
                x => 20f,
                () => new[] {spellCaster}, //can be created more than once?
                () => GameObjectManager.GetObjectByNPCId(6037).Location,
                40f
            ));
            
            result.Add(AddAvoidLine(
                () => spellCaster.IsValid && spellCaster.CastingSpellId == cached,
                x => x.Location,
                x => x.Heading + ((float)Math.PI/2),
                x => 15f,
                x => 20f,
                () => new[] {spellCaster}, //can be created more than once?
                () => spellCaster.Location,
                40f
            ));*/



            return result.ToArray();

        }
        
              /// <summary>
        /// create a line that we need to avoid
        /// </summary>
        /// <typeparam name="T">GameObject type</typeparam>
        /// <param name="canRun">if this avoid should run</param>
        /// <param name="startLocationProducer">where the line starts</param>
        /// <param name="rotationProducer">what the rotation of the line is IN RADIANS</param>
        /// <param name="lengthProducer">how long the line is</param>
        /// <param name="thicknessProducer">how thick the line is</param>
        /// <param name="collectionProducer">GameObjects this should be run against</param>
        /// <param name="leashPointProducer">where the leash point should be</param>
        /// <param name="leashRadius">how far we are allowed to avoid away</param>
        /// <param name="objectValidator">optional</param>
        /// <param name="ignoreIfBlocking">optional</param>
        /// <param name="priority">optional</param>
        private AvoidInfo AddAvoidLine<T>(Func<bool> canRun, Func<T, Vector3> startLocationProducer, Func<T, float> rotationProducer, Func<T, float> lengthProducer, Func<T, float> thicknessProducer, Func<IEnumerable<T>> collectionProducer, Func<Vector3> leashPointProducer, float leashRadius, Func<T, bool> objectValidator = null, bool ignoreIfBlocking = false, AvoidancePriority priority = AvoidancePriority.Medium)
        {
            var poly = new AvoidPolygonInfo<T>(canRun,
                startLocationProducer,
                rotationProducer,
                x => 1f,
                x => Math.Min(15f, lengthProducer(x)),
                x => {
                    float num = thicknessProducer(x) / 2f;
                    float z = lengthProducer(x);
                    return new Vector2[]
                    {
                        new Vector2(0f, num),
                        new Vector2(0f, -num),
                        new Vector2(z, -num),
                        new Vector2(z, num)
                    };
                },
                collectionProducer,
                leashPointProducer,
                leashRadius,
                ignoreIfBlocking,
                objectValidator,
                priority);

            return(poly);
        }

        
    }
}