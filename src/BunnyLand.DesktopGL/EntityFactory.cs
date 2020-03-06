using System;
using System.Collections.Generic;
using System.Linq;
using BunnyLand.DesktopGL.Components;
using BunnyLand.DesktopGL.Enums;
using BunnyLand.DesktopGL.Resources;
using Microsoft.Xna.Framework;
using MonoGame.Extended;
using MonoGame.Extended.Entities;
using MonoGame.Extended.Sprites;
using MonoGame.Extended.TextureAtlases;

namespace BunnyLand.DesktopGL
{
    public class EntityFactory
    {
        private readonly Textures textures;
        private readonly Variables variables;

        public EntityFactory(Textures textures, Variables variables)
        {
            this.textures = textures;
            this.variables = variables;
        }

        private Sprite GetAnkiSprite()
        {
            var sprite = new Sprite(textures.miniAnki);
            return sprite;
        }

        private AnimatedSprite GetPlayerSprite()
        {
            var atlas = TextureAtlas.Create("bunny", textures.PlayerAnimation, 35, 50);
            var spriteSheet = new SpriteSheet {
                TextureAtlas = atlas
            };
            spriteSheet.Cycles.Add("idle", new SpriteSheetAnimationCycle {
                Frames = new List<SpriteSheetAnimationFrame> {
                    new SpriteSheetAnimationFrame(0)
                }
            });
            spriteSheet.Cycles.Add("running",
                new SpriteSheetAnimationCycle
                    {Frames = Enumerable.Range(1, 8).Select(i => new SpriteSheetAnimationFrame(1)).ToList()});
            var animatedSprite = new AnimatedSprite(spriteSheet);
            return animatedSprite;
        }

        public Entity CreatePlayer(Entity entity, Vector2 position, PlayerIndex playerIndex)
        {
            var transform = new Transform2(position);
            entity.Attach(transform);
            var sprite = GetAnkiSprite();
            entity.Attach(sprite);
            entity.Attach(new CollisionBody(new CircleF(Point2.Zero, 15), transform, ColliderTypes.Player,
                ColliderTypes.Player | ColliderTypes.Projectile | ColliderTypes.Static));
            entity.Attach(new Player(playerIndex));
            entity.Attach(new Movable(transform));
            entity.Attach(new Health(100));

            var emitter = new Emitter {
                EmitInterval = TimeSpan.FromSeconds(0.1)
            };
            entity.Attach(emitter);

            return entity;
        }

        public Entity CreatePlanet(Entity entity, Vector2 position, float mass, float scale = 1)
        {
            var transform = new Transform2(position, scale: new Vector2(scale));
            entity.Attach(transform);
            var sprite = new Sprite(textures.redplanet);
            entity.Attach(sprite);
            var boundingRectangle = sprite.GetBoundingRectangle(position, 0, new Vector2(scale));
            entity.Attach(new CollisionBody(new CircleF(Point2.Zero, boundingRectangle.Width / 2f), transform,
                ColliderTypes.Static, ColliderTypes.Player | ColliderTypes.Projectile));
            entity.Attach(new GravityPoint(transform, mass));
            return entity;
        }

        public Entity CreateLevel(Entity entity, float width, float height)
        {
            entity.Attach(new Level(new RectangleF(0, 0, width, height)));
            return entity;
        }

        public Entity CreateBlock(Entity entity, RectangleF rectangleF)
        {
            var transform = new Transform2(rectangleF.Position);
            entity.Attach(transform);
            entity.Attach(new CollisionBody(rectangleF, transform, ColliderTypes.Static,
                ColliderTypes.Player | ColliderTypes.Projectile));
            entity.Attach(new SolidColor(Color.LightCoral, rectangleF));
            return entity;
        }

        public Entity CreateBullet(Entity entity, Vector2 position, Vector2 velocity,
            TimeSpan lifeSpan)
        {
            var transform = new Transform2(position);
            entity.Attach(transform);
            var movable = new Movable(transform) {
                Velocity = velocity
            };
            entity.Attach(movable);
            var collisionBody = new CollisionBody(new CircleF(Point2.Zero, 1), transform,
                ColliderTypes.Projectile, ColliderTypes.Player | ColliderTypes.Static);
            entity.Attach(collisionBody);
            var projectile = new Projectile(movable, collisionBody);
            entity.Attach(projectile);

            var sprite = new Sprite(textures.bullet);
            entity.Attach(sprite);
            var lifetime = new Lifetime(lifeSpan);
            entity.Attach(lifetime);
            entity.Attach(new Damaging(30));
            return entity;
        }
    }
}
