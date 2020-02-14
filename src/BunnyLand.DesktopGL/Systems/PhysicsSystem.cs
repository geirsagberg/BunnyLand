﻿using System;
using BunnyLand.DesktopGL.Components;
using BunnyLand.DesktopGL.Enums;
using BunnyLand.DesktopGL.Extensions;
using LanguageExt;
using Microsoft.Xna.Framework;
using MonoGame.Extended;
using MonoGame.Extended.Entities;
using MonoGame.Extended.Entities.Systems;

namespace BunnyLand.DesktopGL.Systems
{
    public class PhysicsSystem : EntityProcessingSystem
    {
        private readonly Variables variables;
        private ComponentMapper<CollisionBody> bodyMapper;
        private ComponentMapper<Level> levelMapper;
        private ComponentMapper<Movable> movableMapper;
        private ComponentMapper<Transform2> transformMapper;

        public Option<Level> Level { get; set; }

        public PhysicsSystem(Variables variables) : base(Aspect.All(typeof(Transform2), typeof(Movable)))
        {
            this.variables = variables;
        }

        protected override void OnEntityAdded(int entityId)
        {
            levelMapper.TryGet(entityId).IfSome(level => Level = level);
        }

        public override void Initialize(IComponentMapperService mapperService)
        {
            transformMapper = mapperService.GetMapper<Transform2>();
            movableMapper = mapperService.GetMapper<Movable>();
            bodyMapper = mapperService.GetMapper<CollisionBody>();
            levelMapper = mapperService.GetMapper<Level>();
        }

        public override void Process(GameTime gameTime, int entityId)
        {
            var transform = transformMapper.Get(entityId);
            var movable = movableMapper.Get(entityId);
            var body = bodyMapper.TryGet(entityId);
            var elapsedSeconds = gameTime.GetElapsedSeconds() * variables.Global[GlobalVariable.GameSpeed] * 60f;

            // Penetration vector is how far into another entity this entity has collided
            var penetrationVector = body.Some(someBody => someBody.CollisionInfo
                .Some(info => info.PenetrationVector)
                .None(Vector2.Zero)
            ).None(Vector2.Zero);

            var bounceBack = penetrationVector.NormalizedOrZero() * movable.Velocity.Length()
                * variables.Global[GlobalVariable.BounceFactor];

            // Calculate change in velocity
            var deltaVelocity = elapsedSeconds * movable.Acceleration
                + elapsedSeconds * movable.GravityPull
                - penetrationVector
                - bounceBack;
            movable.Velocity += deltaVelocity;

            // Apply braking if any
            movable.Velocity =
                movable.Velocity.SubtractLength(Math.Min(movable.Velocity.Length(), movable.BrakingForce));


            // Limit to max speed
            movable.Velocity = movable.Velocity.Truncate(variables.Global[GlobalVariable.GlobalMaxSpeed])
                * variables.Global[GlobalVariable.InertiaRatio];

            // Update position
            transform.Position += (movable.Velocity - penetrationVector) * elapsedSeconds;

            Level.IfSome(level => transform.Wrap(level.Bounds));
        }
    }
}
