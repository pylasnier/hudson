using System;
using System.Net.Mime;
using System.Text;
using System.Timers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using static Hudson_Game.GameObjects;
using Timer = System.Threading.Timer;

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

        private SpriteFont _spriteFont;

        //private Texture2D _quizBackground;
        private Quiz _quiz;
        
        private bool _enterKeyDownRecorded;
        private bool _plusKeyDownRecorded;

        private GameState _gameState;

        private Timer _tick; // System.Threading.Timer

        private int _lives;
        private int _points;

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

            IsFixedTimeStep = true;
            TargetElapsedTime = TimeSpan.FromMilliseconds(1000 / 144);
            
            ContentLoaded += InitialLoad;
        }

        private void InitialLoad(object sender, EventArgs e)
        {
            ContentLoaded -= InitialLoad;
            //LoadLevel();
            LoadQuiz();
        }

        private void LoadLevel()
        {
            var standing = Content.Load<Texture2D>("Hudson Sprites/Standing_scaled");
            var running = Content.Load<Texture2D>("Hudson Sprites/Running");
            var starting = Content.Load <Texture2D>("Hudson Sprites/Starting");
            var stopping = Content.Load<Texture2D>("Hudson Sprites/Stopping");
            var hit = Content.Load<Texture2D>("Hudson Sprites/Hit");
            _player = new Player(standing, starting, running, stopping, hit, new Rectangle(8, 16, 48, 32), 5, 16, 8, 20,
                3, 330, 300, new Vector2(700, 700));

            var texture = Content.Load<Texture2D>("playzone");
            _camera = new Camera(new Vector2(400, 400), _player);
            _camera.CameraState = CameraState.Locked;

            var data = new Color[100 * 100];
            for (int i = 0; i < 100 * 100; i++)
            {
                data[i] = Color.Green;
            }
            var tTexture = new Texture2D(GraphicsDevice, 100, 100);
            tTexture.SetData(data);
            
            data = new Color[100 * 50];
            for (int i = 0; i < 100 * 50; i++)
            {
                data[i] = Color.Blue;
            }
            var pretendCar = new Texture2D(GraphicsDevice, 100, 50);
            pretendCar.SetData(data);
            
            var vehicleLane =
                new VehicleLane(new Vehicle(pretendCar, new Rectangle(0, 0, 100, 50), 300, HitType.Knockback), 300, 1500,
                    -500, 3000, 1000);
            
            data = new Color[20 * 20];
            for (int i = 0; i < 20 * 20; i++)
            {
                data[i] = Color.Black;
            }
            var litterthing = new Texture2D(GraphicsDevice, 20, 20);
            litterthing.SetData(data);
            
            var litter =
                new Litter(litterthing, new Rectangle(0, 0, 20, 20), new Vector2(600, 600),  100);


            _level = new Level(texture, new Rectangle(100, 100, 800, 800), _player, _camera,
                new[] {new EnvironmentObject(tTexture, new Vector2(50, 80), true, new Rectangle(0, 0, 100, 100))},
                new[] {vehicleLane}, new[] {litter}, 3f);

            _level.VehicleLanes[0].BeginSpawning();

            _gameState = GameState.Game;
            _tick = new Timer(_player.Tick, null, 10, 1000 / 24);

            _lives = 3;
            _points = 0;

            _level.VehicleHit += RemoveLife;
            _level.LitterPickedUp += AddPoints;

            IsMouseVisible = false;
        }

        private void LoadQuiz()
        {
            var idle = Content.Load<Texture2D>("Quiz Textures/idle");
            var hover = Content.Load<Texture2D>("Quiz Textures/hover");
            var click = Content.Load<Texture2D>("Quiz Textures/click");
            
             _quiz = new Quiz(Color.LimeGreen, idle, hover, click, Vector2.Zero, new Rectangle(50, 20, 984, 668), 1);

            _gameState = GameState.Quiz;

            IsMouseVisible = true;
            _points = 0;
            _quiz.CorrectlyAnswered += AddPoints;
        }

        private void LoadLogin()
        {
            
        }

        protected override void Initialize()
        {
            // TODO: Add your initialization logic here

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            
            _spriteFont = Content.Load<SpriteFont>("File");

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

                    base.Update(gameTime);
                    break;
                
                case GameState.Quiz:
                    _quiz.Update(gameTime, Mouse.GetState());
                    break;
                
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        protected override void Draw(GameTime gameTime)
        {
            // TODO: Add your drawing code here
            switch (_gameState)
            {
                case GameState.Loading:
                    
                    GraphicsDevice.Clear(Color.CornflowerBlue);


                    break;

                case GameState.Game:
                    GraphicsDevice.Clear(Color.CornflowerBlue);
                    
                    _spriteBatch.Begin();
                    _spriteBatch.Draw(_level.Texture,
                        new Vector2((int) Math.Round(_graphics.PreferredBackBufferWidth / 2f - _camera.Position.X - _level.PlayZone.Left),
                                    (int) Math.Round(_graphics.PreferredBackBufferHeight / 2f - _camera.Position.Y - _level.PlayZone.Top)));
                    if (_level.EnvironmentObjects != null)
                    {
                        foreach (var environmentObject in _level.EnvironmentObjects)
                        {
                            _spriteBatch.Draw(environmentObject.Texture, new Vector2(
                                    (int) Math.Round(_graphics.PreferredBackBufferWidth / 2f + environmentObject.Position.X - _camera.Position.X),
                                    (int) Math.Round(_graphics.PreferredBackBufferHeight / 2f + environmentObject.Position.Y - _camera.Position.Y)));
                        }
                    }
                    foreach (var vehicle in _level.VehicleLanes[0].VehicleObjects)
                    {
                        _spriteBatch.Draw(vehicle.Texture, new Vector2(
                            (int) Math.Round(_graphics.PreferredBackBufferWidth / 2f + vehicle.Position.X - _camera.Position.X),
                            (int) Math.Round(_graphics.PreferredBackBufferHeight / 2f + vehicle.Position.Y) - _camera.Position.Y));
                    }
                    _spriteBatch.Draw(_player.CurrentFrame,
                        new Vector2((int) Math.Round(_graphics.PreferredBackBufferWidth / 2f + _player.Position.X - _camera.Position.X),
                                    (int) Math.Round(_graphics.PreferredBackBufferHeight / 2f + _player.Position.Y - _camera.Position.Y)));
                    _spriteBatch.DrawString(_spriteFont, new StringBuilder(_lives.ToString()), Vector2.Zero, Color.Red);
                    _spriteBatch.DrawString(_spriteFont, new StringBuilder(_points.ToString()), new Vector2(300, 0), Color.Blue);
                    foreach (var litter in _level.LitterList)
                    {
                        _spriteBatch.Draw(litter.Texture, new Vector2(
                            (int) Math.Round(_graphics.PreferredBackBufferWidth / 2f + litter.Position.X - _camera.Position.X),
                            (int) Math.Round(_graphics.PreferredBackBufferHeight / 2f + litter.Position.Y) - _camera.Position.Y));
                    }

                    _spriteBatch.End();
                    break;
                
                case GameState.Quiz:
                    GraphicsDevice.Clear(_quiz.BackgroundColor);
                    
                    _spriteBatch.Begin();
                    _spriteBatch.DrawString(_spriteFont, _quiz.QuestionText, _quiz.QuestionPosition, Color.Black);
                    foreach (var answerBox in _quiz.AnswerBoxes)
                    {
                        _spriteBatch.Draw(answerBox.Texture, answerBox.Position);
                        _spriteBatch.DrawString(_spriteFont, answerBox.Text, answerBox.Position, Color.Black);
                    }
                    _spriteBatch.DrawString(_spriteFont, new StringBuilder(_points.ToString()), new Vector2(300, 0), Color.Blue);
                    _spriteBatch.End();
                    break;
                
                default:
                    throw new ArgumentOutOfRangeException();
            }

            base.Draw(gameTime);
        }

        private void OnContentLoaded()
        {
            ContentLoaded?.Invoke(this, EventArgs.Empty);
        }

        private void RemoveLife(object sender, EventArgs e)
        {
            _lives--;
            if (_lives <= 0)
            {
                _level.RespawnEnabled = false;
                var wait = new System.Timers.Timer(3000);
                wait.AutoReset = false;
                wait.Enabled = true;

                wait.Elapsed += BeginQuizLoad;
            }
        }

        private void AddPoints(object sender, PointsEventArgs e)
        {
            _points += e.Points;
        }

        private void BeginQuizLoad(object sender, ElapsedEventArgs e)
        {
            LoadQuiz();
        }
    }
}