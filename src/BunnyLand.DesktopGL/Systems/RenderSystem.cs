using System;
using System.Collections.Generic;
using System.Linq;
using BunnyLand.DesktopGL.Components;
using BunnyLand.DesktopGL.Enums;
using BunnyLand.DesktopGL.Extensions;
using LanguageExt;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGame.Extended.BitmapFonts;
using MonoGame.Extended.Entities;
using MonoGame.Extended.Entities.Systems;
using MonoGame.Extended.Sprites;

namespace BunnyLand.DesktopGL.Systems
{
    public class RenderSystem : EntityDrawSystem
    {
        private readonly BitmapFont font;

        private readonly LinkedList<int> fpsList = new LinkedList<int>();
        private readonly SpriteBatch spriteBatch;
        private readonly Variables variables;
        private ComponentMapper<AnimatedSprite> animatedSpriteMapper;
        private ComponentMapper<CollisionBody> collisionMapper;
        private ComponentMapper<Health> healthMapper;
        private ComponentMapper<Level> levelMapper;
        private ComponentMapper<Movable> movableMapper;
        private ComponentMapper<Player> playerMapper;
        private ComponentMapper<SolidColor> solidColorMapper;
        private ComponentMapper<Sprite> spriteMapper;
        private ComponentMapper<Transform2> transformMapper;

        public Option<Level> Level { get; set; }

        private GraphicsDevice GraphicsDevice => spriteBatch.GraphicsDevice;

        public Option<Player> Player { get; set; }

        public RenderSystem(SpriteBatch spriteBatch, ContentManager contentManager, Variables variables) : base(Aspect
            .All(typeof(Transform2))
            .One(typeof(AnimatedSprite), typeof(Sprite), typeof(SolidColor), typeof(Player)))
        {
            this.spriteBatch = spriteBatch;
            this.variables = variables;
            font = contentManager.Load<BitmapFont>("Fonts/bryndan-medium");
        }

        public override void Initialize(IComponentMapperService mapperService)
        {
            transformMapper = mapperService.GetMapper<Transform2>();
            animatedSpriteMapper = mapperService.GetMapper<AnimatedSprite>();
            spriteMapper = mapperService.GetMapper<Sprite>();
            collisionMapper = mapperService.GetMapper<CollisionBody>();
            movableMapper = mapperService.GetMapper<Movable>();
            levelMapper = mapperService.GetMapper<Level>();
            playerMapper = mapperService.GetMapper<Player>();
            solidColorMapper = mapperService.GetMapper<SolidColor>();
            healthMapper = mapperService.GetMapper<Health>();
        }

        protected override void OnEntityAdded(int entityId)
        {
            levelMapper.TryGet(entityId).IfSome(level => Level = level);
            playerMapper.TryGet(entityId).IfSome(player => Player = player);
        }

        public override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);
            var elapsedSeconds = gameTime.GetElapsedSeconds();

            spriteBatch.Begin();
            foreach (var entity in ActiveEntities) {
                animatedSpriteMapper.TryGet(entity).IfSome(animatedSprite => {
                    animatedSprite.Update(elapsedSeconds);
                    RenderSprite(entity, animatedSprite);
                });
                spriteMapper.TryGet(entity).IfSome(sprite => RenderSprite(entity, sprite));
                solidColorMapper.TryGet(entity).IfSome(solidColor => {
                    spriteBatch.DrawRectangle(solidColor.Bounds, solidColor.Color);
                });
                transformMapper.TryGet(entity).IfSome(transform => {
                    DrawCollisionBoundsAndInfo(entity, transform);
                    DrawGravityPull(entity, transform);

                    playerMapper.TryGet(entity).IfSome(player => {
                        spriteBatch.DrawLine(transform.Position,
                            transform.Position + player.DirectionalInputs.AimDirection * 100, Color.White);
                    });
                });
            }

            spriteBatch.DrawString(font, "AWSD: Move, Space: Boost, Shift: Toggle Brake/Glide, Ctrl: Shoot",
                Vector2.One, Color.White);
            Player.IfSome(player => spriteBatch.DrawString(font,
                "Brakes: " + (player.IsBraking ? "On" : "Off"), new Vector2(1, 30), Color.White));

            var smoothedFps = GetSmoothedFps(gameTime);

            spriteBatch.DrawString(font, $"FPS: {smoothedFps}", new Vector2(1, 60),
                Color.White);

            spriteBatch.DrawString(font, $"IsRunningSlowly: {gameTime.IsRunningSlowly}", new Vector2(1, 90),
                Color.White);

            spriteBatch.End();
        }

        private void RenderSprite(int entity, Sprite sprite)
        {
            var transform = transformMapper.Get(entity);

            playerMapper.TryGet(entity).IfSome(player => {
                sprite.Color = player.PlayerIndex switch {
                    PlayerIndex.One => new Color(128, 128, 255),
                    PlayerIndex.Two => new Color(255, 128, 128),
                    PlayerIndex.Three => Color.Yellow,
                    PlayerIndex.Four => Color.Green,
                    _ => Color.White
                };
                healthMapper.TryGet(entity).IfSome(health => {
                    transform.Scale = Vector2.One * health.CurrentHealth / health.MaxHealth;
                });
            });

            spriteBatch.Draw(sprite, transform);
            // spriteBatch.DrawRectangle(sprite.GetBoundingRectangle(transform), Color.Beige);

            movableMapper.TryGet(entity).IfSome(movable => {
                if (movable.WrapAround) {
                    DrawLevelWrapping(sprite, transform);
                }
            });
        }

        private int GetSmoothedFps(GameTime gameTime)
        {
            var fps = (int) (Math.Abs(gameTime.ElapsedGameTime.TotalSeconds) < 0.0000001
                ? 0
                : 1 / gameTime.ElapsedGameTime.TotalSeconds);

            fpsList.AddLast(fps);
            if (fpsList.Count > 3)
                fpsList.RemoveFirst();

            var smoothedFps = fpsList.Sum() / fpsList.Count;
            return smoothedFps;
        }

        private void DrawLevelWrapping(Sprite sprite, Transform2 transform)
        {
            Level.IfSome(level => {
                var bounds = sprite.GetBoundingRectangle(transform);

                var overlappingTop = bounds.Top < 0;
                var overlappingBottom = bounds.Bottom >= level.Bounds.Bottom;

                if (overlappingTop) {
                    spriteBatch.Draw(sprite, transform.Position + level.Bounds.HeightVector(),
                        transform.Rotation, transform.Scale);
                } else if (overlappingBottom) {
                    spriteBatch.Draw(sprite, transform.Position - level.Bounds.HeightVector(),
                        transform.Rotation, transform.Scale);
                }

                var overlappingLeft = bounds.Left < 0;
                var overlappingRight = bounds.Right >= level.Bounds.Right;

                if (overlappingLeft) {
                    spriteBatch.Draw(sprite, transform.Position + level.Bounds.WidthVector(),
                        transform.Rotation, transform.Scale);
                } else if (overlappingRight) {
                    spriteBatch.Draw(sprite, transform.Position - level.Bounds.WidthVector(),
                        transform.Rotation, transform.Scale);
                }

                if (overlappingTop && overlappingLeft) {
                    spriteBatch.Draw(sprite, transform.Position + level.Bounds.BottomRight,
                        transform.Rotation, transform.Scale);
                } else if (overlappingTop && overlappingRight) {
                    spriteBatch.Draw(sprite,
                        transform.Position + level.Bounds.HeightVector() - level.Bounds.WidthVector(),
                        transform.Rotation, transform.Scale);
                } else if (overlappingBottom && overlappingLeft) {
                    spriteBatch.Draw(sprite,
                        transform.Position + level.Bounds.WidthVector() - level.Bounds.HeightVector(),
                        transform.Rotation, transform.Scale);
                } else if (overlappingBottom && overlappingRight) {
                    spriteBatch.Draw(sprite, transform.Position - (Vector2) level.Bounds.BottomRight,
                        transform.Rotation, transform.Scale);
                }
            });
        }

        private void DrawCollisionBoundsAndInfo(int entity, Transform2 transform)
        {
            collisionMapper.TryGet(entity).IfSome(body => {
                var color = body.Collisions.Any() ? Color.Red : Color.Aqua;
                if (body.Bounds is CircleF circle) {
                    spriteBatch.DrawCircle(circle, 32, color);
                } else if (body.Bounds is RectangleF rectangle) {
                    spriteBatch.DrawRectangle(rectangle, color);
                }

                if (body.CollisionBounds != default) {
                    spriteBatch.DrawRectangle(body.CollisionBounds, Color.Chocolate);
                }

                foreach (var collision in body.Collisions) {
                    spriteBatch.DrawLine(transform.WorldPosition, transform.WorldPosition + collision.penetrationVector,
                        Color.Aquamarine);
                }

                // body.CollisionInfo.IfSome(info => {
                //     spriteBatch.DrawLine(transform.WorldPosition, transform.WorldPosition + info.PenetrationVector,
                //         Color.Aquamarine);
                // });
            });
        }

        private void DrawGravityPull(int entity, Transform2 transform)
        {
            movableMapper.TryGet(entity).IfSome(movable => spriteBatch.DrawLine(transform.WorldPosition,
                transform.WorldPosition + movable.GravityPull * variables.Global[GlobalVariable.DebugVectorMultiplier],
                Color.Azure));
        }
    }
}
