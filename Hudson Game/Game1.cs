using System;
using System.Net.Mime;
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
            var texture = Content.Load<Texture2D>("Hudson Sprites/Standing_scaled");
            _player = new Player(texture, texture, texture, texture, 1, 1, 1, new Vector2(400, 400));

            texture = Content.Load<Texture2D>("playzone");
            _level = new Level(texture, new Rectangle(100, 100, 800, 800));
            _camera = new Camera(new Vector2(400, 400), _player);

            _enterKeyDownRecorded = false;

            _gameState = GameState.Game;
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

                    if (!_enterKeyDownRecorded && Keyboard.GetState().IsKeyDown(Keys.Enter))
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
                    }
            
                    _camera.Update(gameTime);

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
                        new Vector2(_graphics.PreferredBackBufferWidth / 2f - _camera.Position.X - _level.PlayZone.X,
                            _graphics.PreferredBackBufferHeight / 2f - _camera.Position.Y - _level.PlayZone.Y));
                    _spriteBatch.Draw(_player.Standing,
                        new Vector2(_graphics.PreferredBackBufferWidth / 2f + _player.Position.X - _camera.Position.X,
                            _graphics.PreferredBackBufferHeight / 2f + _player.Position.Y - _camera.Position.Y), null,
                        Color.White, 0f, new Vector2(_player.Standing.Width / 2f, _player.Standing.Height / 2f), 1f,
                        SpriteEffects.None, 0);
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