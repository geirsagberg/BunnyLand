﻿using BunnyLand.DesktopGL.Components;
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
        private ComponentMapper<CollisionBody> bodyMapper;
        private ComponentMapper<Level> levelMapper;
        private ComponentMapper<Movable> movableMapper;
        private ComponentMapper<Transform2> transformMapper;

        public Option<Level> Level { get; set; }

        public PhysicsSystem() : base(Aspect.All(typeof(Transform2), typeof(Movable)))
        {
        }

        protected override void OnEntityAdded(int entityId)
        {
            levelMapper.MaybeGet(entityId).IfSome(level => Level = level);
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
            var body = bodyMapper.MaybeGet(entityId);
            var collisionVector = body.Some(someBody => someBody.CollisionInfo
                .Some(info => info.PenetrationVector)
                .None(Vector2.Zero)
            ).None(Vector2.Zero);

            const float bounce = 0.5f;
            movable.Velocity += movable.Acceleration + movable.GravityPull - collisionVector
                - collisionVector.NormalizedOrZero() * movable.Velocity.Length() * bounce;
            const float maxSpeed = 100;
            const float inertiaRatio = 0.99f;
            movable.Velocity = movable.Velocity.Truncate(maxSpeed) * inertiaRatio;
            const int fps = 60;
            transform.Position += (movable.Velocity - collisionVector) * gameTime.GetElapsedSeconds() * fps;

            Level.IfSome(level => transform.Wrap(level.Bounds));
        }
    }
}
