﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using BunnyLand.DesktopGL.Components;
using BunnyLand.DesktopGL.Extensions;
using LanguageExt;
using Microsoft.Xna.Framework;
using MonoGame.Extended;
using MonoGame.Extended.Collections;
using MonoGame.Extended.Entities;
using MonoGame.Extended.Entities.Systems;

namespace BunnyLand.DesktopGL.Systems
{
    /// <summary>
    ///     Handles adding <see cref="CollisionBody" /> components to the <see cref="CollisionComponent" />, and resetting
    ///     their collision info. Collision checking itself is handled within <see cref="CollisionComponent" />.
    /// </summary>
    public class CollisionSystem : EntityProcessingSystem
    {
        private readonly Variables variables;
        private const int LogCollisionDetectionEveryNthFrame = 60;
        private ComponentMapper<CollisionBody> bodyMapper;
        private ComponentMapper<Level> levelMapper;
        private ComponentMapper<Movable> movableMapper;

        public Option<Level> Level { get; set; }

        public Bag<CollisionBody> Bodies { get; } = new Bag<CollisionBody>();

        public CollisionSystem(Variables variables) : base(Aspect.All(typeof(CollisionBody)))
        {
            this.variables = variables;
        }

        public override void Initialize(IComponentMapperService mapperService)
        {
            bodyMapper = mapperService.GetMapper<CollisionBody>();
            movableMapper = mapperService.GetMapper<Movable>();
            levelMapper = mapperService.GetMapper<Level>();
        }

        protected override void OnEntityAdded(int entityId)
        {
            levelMapper.TryGet(entityId).IfSome(level => {
                Level = level;


                // var component = new CollisionComponent(level.Bounds);
                // foreach (var entity in ActiveEntities) {
                //     component.Insert(bodyMapper.Get(entity));
                // }

                // CollisionComponent = component;
            });
            bodyMapper.TryGet(entityId).IfSome(body => Bodies.Add(body));
            // CollisionComponent.IfSome(collisionComponent =>
            //     bodyMapper.TryGet(entityId).IfSome(collisionComponent.Insert));
        }

        protected override void OnEntityRemoved(int entityId)
        {
            // CollisionComponent.IfSome(collisionComponent =>
            //     bodyMapper.TryGet(entityId).IfSome(collisionComponent.Remove));
            bodyMapper.TryGet(entityId).IfSome(body => Bodies.Remove(body));
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            // CollisionComponent.IfSome(component => component.Update(gameTime));
        }

        private readonly Stopwatch stopwatch = new Stopwatch();

        public override void Begin()
        {
            stopwatch.Restart();
        }

        public override void End()
        {
            stopwatch.Stop();
            timeSpans[timeSpanCounter] = stopwatch.Elapsed;
            timeSpanCounter = (timeSpanCounter + 1) % LogCollisionDetectionEveryNthFrame;
            if (timeSpanCounter == 0) {
                Console.WriteLine($"Avg time collision detection: {timeSpans.Average(ts => ts.TotalMilliseconds):N} ms");
            }
        }

        private int timeSpanCounter = 0;
        private readonly TimeSpan[] timeSpans = new TimeSpan[LogCollisionDetectionEveryNthFrame];

        public override void Process(GameTime gameTime, int entityId)
        {
            var elapsedTicks = gameTime.GetElapsedTicks(variables);

            bodyMapper.TryGet(entityId).IfSome(body => {
                movableMapper.TryGet(entityId).IfSome(movable => {
                    var collisionBounds = body.Bounds switch {
                        CircleF circle => FindCollisionBounds(movable, circle.ToRectangleF(), elapsedTicks),
                        RectangleF rectangle => FindCollisionBounds(movable, rectangle, elapsedTicks),
                        _ => throw new Exception("Unknown shape")
                    };
                    body.CollisionBounds = collisionBounds;

                    // TODO: If performance becomes a problem, look into broadphase algorithms like SAP or Dynamic tree, or separate collision tables per collidertype
                    var potentialCollisions = Bodies.Where(b =>
                        b.CollidesWith.HasFlag(body.ColliderType) && b != body && b.Bounds.Intersects(collisionBounds));

                    body.Collisions = potentialCollisions
                        .Select(other => (other, body.CalculatePenetrationVector(other, elapsedTicks)))
                        .Where(t => t.Item2 != Vector2.Zero).ToList();
                });

            });
        }


        private static RectangleF FindCollisionBounds(Option<Movable> maybeMovable, RectangleF rectangle,
            float elapsedTicks)
        {
            // If it is moving, we have to include the path from here to there in the potential collision
            return maybeMovable.Some(movable => rectangle.Expand(movable.Velocity * elapsedTicks))
                .None(rectangle);
        }
    }
}
