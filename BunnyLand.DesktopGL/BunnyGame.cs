﻿using System;
using BunnyLand.DesktopGL.Misc;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace BunnyLand.DesktopGL
{
    public class BunnyGame : Game
    {
        private readonly GraphicsDeviceManager graphics;
        private SpriteBatch spriteBatch;
        private Texture2D blackHoleTexture;
        private Vector2 blackHolePosition;
        private float blackHoleRotation;

        public BunnyGame()
        {
            graphics = new GraphicsDeviceManager(this);
            graphics.PreferredBackBufferWidth = GlobalSettings.WindowWidth;
            graphics.PreferredBackBufferHeight = GlobalSettings.WindowHeight;
            graphics.ApplyChanges();

            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            // TODO: Add your initialization logic here
            blackHolePosition = new Vector2(graphics.PreferredBackBufferWidth / 2f,
                graphics.PreferredBackBufferHeight / 2f);

            base.Initialize();
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);

            // TODO: use this.Content to load your game content here
            blackHoleTexture = Content.Load<Texture2D>("black-hole");
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
                Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            // TODO: Add your update logic here

            blackHoleRotation += Convert.ToSingle(gameTime.ElapsedGameTime.TotalMilliseconds * 0.001);

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            var (effect1, poly1) = PolygonExperiment.CreateColoredPolygon(GraphicsDevice, true);
            var (effect2, poly2) = PolygonExperiment.CreateColoredPolygon(GraphicsDevice, false);

            foreach (EffectPass effectPass in effect1.CurrentTechnique.Passes)
            {
                effectPass.Apply();

                graphics.GraphicsDevice.DrawUserIndexedPrimitives<VertexPositionColor>(
                    PrimitiveType.TriangleList,
                    poly1.vertices, 0, poly1.vertices.Length,
                    poly1.triangleVertexOrder, 0, poly1.triangleVertexOrder.Length / 3);

            }

            foreach (EffectPass effectPass in effect2.CurrentTechnique.Passes)
            {
                effectPass.Apply();
                graphics.GraphicsDevice.DrawUserIndexedPrimitives<VertexPositionColor>(
                    PrimitiveType.TriangleList,
                    poly2.vertices, 0, poly2.vertices.Length,
                    poly2.triangleVertexOrder, 0, poly2.triangleVertexOrder.Length / 3);
            }

            // Note: Some weird color blending happening between primitives and spriteBatch

            // TODO: Add your drawing code here

            spriteBatch.Begin();
            spriteBatch.Draw(blackHoleTexture, blackHolePosition, null, Color.White, blackHoleRotation,
                new Vector2(blackHoleTexture.Width / 2f, blackHoleTexture.Height / 2f), Vector2.One, SpriteEffects.FlipHorizontally,
                0f);
            spriteBatch.End();


            base.Draw(gameTime);
        }
    }
}
