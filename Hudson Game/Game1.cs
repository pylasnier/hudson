﻿using System;
using System.Net.Mime;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using static Hudson_Game.GameObjects;

namespace Hudson_Game
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        //private Texture2D _playZoneTexture;

        private Level _level;
        private Camera _camera;
        //private Texture2D _playerTexture;
        private Player _player;

        //private Texture2D _quizBackground;
        private Quiz _quiz;
        
        private bool _enterKeyDownRecorded;
        private bool _plusKeyDownRecorded;

        private GameState _gameState;

        private Timer frameAdvance;

        public event EventHandler ContentLoaded;

        public Game1()
        {
            _gameState = GameState.Loading;
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            _graphics.PreferredBackBufferWidth = 1024;
            _graphics.PreferredBackBufferHeight = 768;
            _graphics.ApplyChanges();
            
            ContentLoaded += InitialLoad;
        }

        private void InitialLoad(object sender, EventArgs e)
        {
            ContentLoaded -= InitialLoad;
            LoadLevel();
        }

        private void LoadLevel()
        {
            var standing = Content.Load<Texture2D>("Hudson Sprites/Standing_scaled");
            var running = Content.Load<Texture2D>("Hudson Sprites/Running");
            _player = new Player(standing, standing, running, standing, new Rectangle(8, 8, 48, 48),  1, 16, 1, new Vector2(400, 400));

            var texture = Content.Load<Texture2D>("playzone");
            _camera = new Camera(new Vector2(400, 400), _player);

            var data = new Color[100 * 100];
            for (int i = 0; i < 100 * 100; i++)
            {
                data[i] = Color.Green;
            }
            var tTexture = new Texture2D(GraphicsDevice, 100, 100);
            tTexture.SetData(data);

            _level = new Level(texture, new Rectangle(100, 100, 800, 800), _player, _camera,
                new[] {new EnvironmentObject(tTexture, new Vector2(50, 80), true, new Rectangle(0, 0, 100, 100))});

            _enterKeyDownRecorded = false;

            _gameState = GameState.Game;
            frameAdvance = new Timer(_player.FrameAdvance, null, 10, 1000 / 24);
        }

        private void LoadQuiz()
        {
            var data = new Color[500 * 500];
            for (int i = 0; i < 500 * 500; i++)
            {
                data[i] = Color.IndianRed;
            }
            var texture = new Texture2D(GraphicsDevice, 500, 500);
            texture.SetData(data);
            
            _quiz = new Quiz(texture);

            _gameState = GameState.Quiz;
        }

        protected override void Initialize()
        {
            // TODO: Add your initialization logic here

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // TODO: use this.Content to load your game content here

            OnContentLoaded();
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
                Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            // TODO: Add your update logic here

            switch (_gameState)
            {
                case GameState.Game:
                    var moveVector = Vector2.Zero;
            
                    if (Keyboard.GetState().IsKeyDown(Keys.W)) moveVector += -Vector2.UnitY;
                    if (Keyboard.GetState().IsKeyDown(Keys.S)) moveVector += Vector2.UnitY;
                    if (Keyboard.GetState().IsKeyDown(Keys.A)) moveVector += -Vector2.UnitX;
                    if (Keyboard.GetState().IsKeyDown(Keys.D)) moveVector += Vector2.UnitX;

                    /*if (!_enterKeyDownRecorded && Keyboard.GetState().IsKeyDown(Keys.Enter))
                    {
                        _enterKeyDownRecorded = true;
                        if (_camera.CameraState == CameraState.Free)
                            _camera.CameraState = CameraState.Locked;
                        else
                            _camera.CameraState = CameraState.Free;
                    }
                    else if (_enterKeyDownRecorded && Keyboard.GetState().IsKeyUp(Keys.Enter))
                    {
                        _enterKeyDownRecorded = false;
                    }

                    if (!_plusKeyDownRecorded && Keyboard.GetState().IsKeyDown(Keys.OemPlus))
                    {
                        _plusKeyDownRecorded = true;
                        LoadQuiz();
                    }
                    else if (_plusKeyDownRecorded && Keyboard.GetState().IsKeyUp(Keys.OemPlus))
                    {
                        _plusKeyDownRecorded = false;
                    }
                    
                    if (moveVector != Vector2.Zero)
                    {
                        switch (_camera.CameraState)
                        {
                            case CameraState.Free:
                                _camera.Move(Vector2.Normalize(moveVector) * 100f, gameTime);
                                break;
                    
                            case CameraState.Locked:
                                _player.Move(Vector2.Normalize(moveVector) * 100f, gameTime);
                                break;
                    
                            case CameraState.Chase:
                                break;
                    
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }*/

                    _level.Update(gameTime, Keyboard.GetState());
                    //_camera.Update(gameTime);

                    base.Update(gameTime);
                    break;
                
                case GameState.Quiz:

                    if (!_plusKeyDownRecorded && Keyboard.GetState().IsKeyDown(Keys.OemPlus))
                    {
                        _plusKeyDownRecorded = true;
                        LoadLevel();
                    }
                    else if (_plusKeyDownRecorded && Keyboard.GetState().IsKeyUp(Keys.OemPlus))
                    {
                        _plusKeyDownRecorded = false;
                    }
                    break;
                
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            // TODO: Add your drawing code here
            switch (_gameState)
            {
                case GameState.Loading:
                    break;

                case GameState.Game:
                    _spriteBatch.Begin();
                    _spriteBatch.Draw(_level.Texture,
                        new Vector2(_graphics.PreferredBackBufferWidth / 2f - _camera.Position.X - _level.PlayZone.Left,
                            _graphics.PreferredBackBufferHeight / 2f - _camera.Position.Y - _level.PlayZone.Top));
                    if (_level.EnvironmentObjects != null)
                    {
                        foreach (var environmentObject in _level.EnvironmentObjects)
                        {
                            _spriteBatch.Draw(environmentObject.Texture,
                                new Vector2(
                                    _graphics.PreferredBackBufferWidth / 2f + environmentObject.Position.X - _camera.Position.X,
                                    _graphics.PreferredBackBufferHeight / 2f + environmentObject.Position.Y -
                                    _camera.Position.Y));
                        }
                    }
                    _spriteBatch.Draw(_player.CurrentFrame,
                        new Vector2(_graphics.PreferredBackBufferWidth / 2f + _player.Position.X - _camera.Position.X,
                            _graphics.PreferredBackBufferHeight / 2f + _player.Position.Y - _camera.Position.Y));
                    _spriteBatch.End();
                    break;
                
                case GameState.Quiz:
                    _spriteBatch.Begin();
                    _spriteBatch.Draw(_quiz.Texture, Vector2.Zero);
                    _spriteBatch.End();
                    break;
                
                default:
                    throw new ArgumentOutOfRangeException();
            }

            base.Draw(gameTime);
        }

        protected virtual void OnContentLoaded()
        {
            ContentLoaded?.Invoke(this, EventArgs.Empty);
        }
    }
}