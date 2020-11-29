﻿using System;
using System.IO;
using System.Threading.Tasks;
using Apos.Gui;
using FontStashSharp;
using Microsoft.Xna.Framework;
using MonoGame.Extended;
using MonoGame.Extended.Entities;
using MultiPong.DesktopGL.Systems;

namespace MultiPong.DesktopGL
{
    public class MainGame : Game
    {
        private static ComponentFocus _focus;

        private readonly Action<Component> _grabFocus = c => { _focus.Focus = c; };

        private World _gameModule;
        private GuiModule _guiModule;
        private ScreenPanel _screen;

        public MainGame()
        {
            Graphics = new GraphicsDeviceManager(this);
            IsMouseVisible = true;
        }

        public GraphicsDeviceManager Graphics { get; }

        protected override void LoadContent()
        {
            SetupGui();
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);
            base.Draw(gameTime);
        }

        private void SetupGui()
        {
            var fontSystem = FontSystemFactory.Create(GraphicsDevice, 2048, 2048);
            fontSystem.AddFont(File.ReadAllBytes("Content/Fonts/Goldman-Regular.ttf"));
            GuiHelper.Setup(this, fontSystem);
            _screen = new ScreenPanel { Layout = new LayoutVerticalCenter() };
            _focus = new ComponentFocus(_screen, Default.ConditionPrevFocus, Default.ConditionNextFocus);
            _guiModule = new GuiModule(_focus);
            Components.Add(_guiModule);
        }

        protected override void BeginRun()
        {
            ShowMenu();
        }

        private void ShowMenu()
        {
            _screen.Clear();
            var container = GetVerticalContainer();
            container.Add(Default.CreateButton("Start Game", _ => {
                StartGame();
                return true;
            }, _grabFocus));
            container.Add(Default.CreateButton("Join Game", _ => {
                ShowServers();
                return true;
            }, _grabFocus));
            container.Add(Default.CreateButton("Exit Game", _ => {
                Exit();
                return true;
            }, _grabFocus));
            _screen.Add(container);
        }

        private static Panel GetVerticalContainer()
        {
            var container = new Panel { Layout = new LayoutVerticalCenter() };
            container.AddHoverCondition(Default.ConditionHoverMouse);
            container.AddAction(Default.IsScrolled, Default.ScrollVertically);
            return container;
        }

        private void ShowServers()
        {
            _screen.Clear();
            var container = GetVerticalContainer();
            container.Add(new Label("Loading..."));
            container.Add(Default.CreateButton("Cancel", _ => {
                ShowMenu();
                return true;
            }, _grabFocus));
            _screen.Add(container);

            Task.Run(() => {

            });
        }

        private void StartGame()
        {
            _screen.Clear();
            _gameModule = new WorldBuilder()
                .AddSystem(new PhysicsSystem())
                .Build();

            _gameModule.CreatePaddle(0);
            _gameModule.CreatePaddle(1);
            Components.Add(_gameModule);


        }

        private void SetupWorld()
        {
        }
    }

    internal class GameModule : SimpleDrawableGameComponent
    {
        public override void Update(GameTime gameTime)
        {
            throw new NotImplementedException();
        }

        public override void Draw(GameTime gameTime)
        {
            throw new NotImplementedException();
        }
    }

    internal class GuiModule : SimpleDrawableGameComponent
    {
        private readonly ComponentFocus _componentFocus;

        public GuiModule(ComponentFocus componentFocus)
        {
            _componentFocus = componentFocus;
        }

        public override void Update(GameTime gameTime)
        {
            GuiHelper.UpdateSetup();

            _componentFocus.UpdateSetup();
            _componentFocus.UpdateInput();
            _componentFocus.Update();

            GuiHelper.UpdateCleanup();
        }

        public override void Draw(GameTime gameTime)
        {
            _componentFocus.Draw();
        }
    }
}
