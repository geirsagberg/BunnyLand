using BunnyLand.DesktopGL.Extensions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGame.Extended.Animations;
using MonoGame.Extended.Entities;
using MonoGame.Extended.Entities.Systems;
using MonoGame.Extended.Sprites;

namespace BunnyLand.DesktopGL.Systems
{
    public class RenderSystem : EntityDrawSystem
    {
        private readonly SpriteBatch spriteBatch;
        private ComponentMapper<AnimatedSprite> animatedSpriteMapper = null!;
        private ComponentMapper<Sprite> spriteMapper = null!;
        private ComponentMapper<Transform2> transformMapper = null!;

        public RenderSystem(SpriteBatch spriteBatch) : base(Aspect.All(typeof(Transform2))
            .One(typeof(AnimatedSprite), typeof(Sprite)))
        {
            this.spriteBatch = spriteBatch;
        }

        public override void Initialize(IComponentMapperService mapperService)
        {
            transformMapper = mapperService.GetMapper<Transform2>();
            animatedSpriteMapper = mapperService.GetMapper<AnimatedSprite>();
            spriteMapper = mapperService.GetMapper<Sprite>();
        }

        public override void Draw(GameTime gameTime)
        {
            spriteBatch.Begin();
            foreach (var entity in ActiveEntities) {
                var sprite = animatedSpriteMapper.GetOrNull(entity)
                    ?? spriteMapper.Get(entity);
                if (sprite is AnimatedSprite animatedSprite)
                    animatedSprite.Update(gameTime.GetElapsedSeconds());

                var transform = transformMapper.Get(entity);

                spriteBatch.Draw(sprite, transform);
            }

            spriteBatch.End();
        }
    }
}
