using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using static Hudson_Game.GameObjects;

namespace Hudson_Game
{
    public class Game1 : Game
    {
        GraphicsDeviceManager _graphics;
        SpriteBatch _spriteBatch;
        Texture2D _playZoneTexture;

        private Level _level;
        private Camera _camera;
        private Texture2D _playerTexture;
        private Player _player;
        private bool _keyDownRecorded;

        public event EventHandler ContentLoaded;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            _graphics.PreferredBackBufferWidth = 1024;
            _graphics.PreferredBackBufferHeight = 768;
            _graphics.ApplyChanges();
            
            ContentLoaded += UseContent;
        }

        private void UseContent(object sender, EventArgs e)
        {
            _level = new Level(_playZoneTexture, new Rectangle(100, 100, 800, 800));
            _player = new Player(_playerTexture, _playerTexture, _playerTexture, _playerTexture, 1, 1, new Vector2(400, 400));
            _camera.Target = _player;
        }

        protected override void Initialize()
        {
            // TODO: Add your initialization logic here
            _camera = new Camera(new Vector2(400, 400), null);
            _keyDownRecorded = false;

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // TODO: use this.Content to load your game content here
            _playZoneTexture = Content.Load<Texture2D>("playzone");
            
            _playerTexture = new Texture2D(GraphicsDevice, 80, 80);
            
            var data = new Color[80 * 80];
            for (int i = 0; i < 80; i++)
            {
                for (int j = 0; j < 80; j++)
                {
                    if ((i - 40) * (i - 40) + (j - 40) * (j - 40) <= 40 * 40)
                        data[80 * i + j] = Color.Blue;
                    else
                        data[80 * i + j] = Color.Transparent;
                }
            }
            
            _playerTexture.SetData(data);

            OnContentLoaded();
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
                Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            // TODO: Add your update logic here
            var moveVector = Vector2.Zero;
            
            if (Keyboard.GetState().IsKeyDown(Keys.W)) moveVector += -Vector2.UnitY;
            if (Keyboard.GetState().IsKeyDown(Keys.S)) moveVector += Vector2.UnitY;
            if (Keyboard.GetState().IsKeyDown(Keys.A)) moveVector += -Vector2.UnitX;
            if (Keyboard.GetState().IsKeyDown(Keys.D)) moveVector += Vector2.UnitX;

            if (!_keyDownRecorded && Keyboard.GetState().IsKeyDown(Keys.Enter))
            {
                _keyDownRecorded = true;
                if (_camera.CameraState == CameraState.Free)
                    _camera.CameraState = CameraState.Locked;
                else
                    _camera.CameraState = CameraState.Free;
            }
            else if (_keyDownRecorded && Keyboard.GetState().IsKeyUp(Keys.Enter))
            {
                _keyDownRecorded = false;
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
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            // TODO: Add your drawing code here
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

            base.Draw(gameTime);
        }

        protected virtual void OnContentLoaded()
        {
            ContentLoaded?.Invoke(this, EventArgs.Empty);
        }
    }
}