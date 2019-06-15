using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Hudson_Game
{
    public static class GameObjects
    {
        public class Level
        {
            public Texture2D Texture { get; }
            public Rectangle PlayZone { get; }
            public Player Player { get; }
            public Camera Camera { get; }
            public EnvironmentObject[] EnvironmentObjects { get; }

            public event CollisionEventHandler Collided;

            private Vector2 _lastValidPlayerPosition;
            private Vector2 _lastValidCameraPosition;

            public Level(Texture2D texture, Rectangle playZone, Player player, Camera camera, EnvironmentObject[] environmentObjects)
            {
                Texture = texture;
                PlayZone = playZone;
                Player = player;
                Camera = camera;
                EnvironmentObjects = environmentObjects;
                
                Player.SetLevel(this);

                _lastValidPlayerPosition = Player.Position;
                _lastValidCameraPosition = Camera.Position;
            }

            public void Update(GameTime gameTime, KeyboardState keyboardState)
            {
                var collided = false;
                
                Player.Update(gameTime, keyboardState);

                if (Player.Position.X + Player.Hitbox.Right > PlayZone.Width ||
                    Player.Position.X + Player.Hitbox.Left < 0 ||
                    Player.Position.Y + Player.Hitbox.Bottom  > PlayZone.Height ||
                    Player.Position.Y + Player.Hitbox.Top < 0)
                {
                    collided = true;
                    OnCollision(_lastValidPlayerPosition);
                }
                
                else if (EnvironmentObjects != null)
                {
                    foreach (var environmentObject in EnvironmentObjects)
                    {
                        if (environmentObject.CollisionEnabled &&
                            new Rectangle((int) (environmentObject.Position.X + environmentObject.Hitbox.X),
                            (int) (environmentObject.Position.Y + environmentObject.Hitbox.Y),
                            environmentObject.Hitbox.Width, environmentObject.Hitbox.Height).Intersects(
                            new Rectangle((int) (Player.Position.X + Player.Hitbox.X),
                                (int) (Player.Position.Y + Player.Hitbox.Y),
                                Player.Hitbox.Width, Player.Hitbox.Height)))
                        {
                            collided = true;
                            OnCollision(_lastValidPlayerPosition);
                            break;
                        }
                    }
                }

                if (!collided)
                {
                    _lastValidPlayerPosition = Player.Position;
                }
                
                Camera.Update(gameTime);
            }

            private void OnCollision(Vector2 lastValidPosition)
            {
                Collided?.Invoke(this, new CollisionEventArgs(lastValidPosition));
            }
        }

        public struct EnvironmentObject
        {
            public Texture2D Texture { get; }
            public Vector2 Position { get; }
            public bool CollisionEnabled { get; }
            public Rectangle Hitbox { get; }

            public EnvironmentObject(Texture2D texture, Vector2 position, bool collisionEnabled, Rectangle hitbox)
            {
                Texture = texture;
                Position = position;
                CollisionEnabled = collisionEnabled;
                Hitbox = hitbox;
            }
        }

        public class Player
        {
            public Vector2 Position { get; private set; }
            public Rectangle Hitbox => GetHitbox();
            public Texture2D CurrentFrame => GetFrame();
            
            private readonly Texture2D _standing;
            private readonly Texture2D _starting;
            private readonly Texture2D _running;
            private readonly Texture2D _stopping;
            private readonly Texture2D _hit;
            
            private readonly int _startingFrames;
            private readonly int _runningFrames;
            private readonly int _stoppingFrames;

            private readonly float _runningSpeed;
            private readonly float _acceleration;
            private readonly float _deceleration;
            
            private Vector2 _direction = -Vector2.UnitX;
            private float _speed; // Default 0 at initialisation
            private PlayerState _playerState = PlayerState.Standing;
            private Rectangle _hitbox;
            private KeyboardState _keyboardState;
            private int _frame; // Default 0 at initialisation
            private int _delay; // Default 0 at initialisation

            public Player(Texture2D standing, Texture2D starting, Texture2D running, Texture2D stopping, Texture2D hit, Rectangle hitbox, int startingFrames, int runningFrames, int stoppingFrames, float runningSpeed, Vector2 position)
            {
                _standing = standing;
                _starting = starting;
                _running = running;
                _stopping = stopping;
                _hit = hit;
                _hitbox = hitbox;
                _startingFrames = startingFrames;
                _runningFrames = runningFrames;
                _stoppingFrames = stoppingFrames;
                _runningSpeed = runningSpeed;
                _acceleration = runningSpeed / startingFrames;
                _deceleration = runningSpeed / stoppingFrames;
                Position = position;
            }

            public void Update(GameTime gameTime, KeyboardState keyboardState)
            {
                Position += _direction * _speed * (float) gameTime.ElapsedGameTime.TotalSeconds;
                _keyboardState = keyboardState;
            }

            public void Tick(object stateInfo)
            {
                Vector2 moveVector;
                
                var w = _keyboardState.IsKeyDown(Keys.W);
                var a = _keyboardState.IsKeyDown(Keys.A);
                var s = _keyboardState.IsKeyDown(Keys.S);
                var d = _keyboardState.IsKeyDown(Keys.D);

                if (a ^ d)
                {
                    moveVector = new Vector2((d ? 1 : 0) - (a ? 1 : 0), 0);
                }
                else
                {
                    moveVector = new Vector2(0, (s ? 1 : 0) - (w ? 1 : 0));
                }
                
                if (_playerState == PlayerState.Stopping)
                {
                    if (_frame < _stoppingFrames - 1)
                    {
                        _frame++;
                        _speed -= _deceleration;
                    }
                    else
                    {
                        _playerState = PlayerState.Standing;
                        _frame = 0;
                        _speed = 0;
                    }
                }

                else if (moveVector != Vector2.Zero)
                {
                    if (_playerState == PlayerState.Standing)
                    {
                        _playerState = PlayerState.Starting;
                        _direction = moveVector;
                        _frame = 0;
                    }
                    else if (_direction + moveVector == Vector2.Zero)
                    {
                        if (_playerState == PlayerState.Starting)
                        {
                            _playerState = PlayerState.Standing;
                            _speed = 0;
                            _frame = 0;
                        }
                        else // _playerState == PlayerState.Running
                        {
                            _playerState = PlayerState.Stopping;
                            _frame = 0;
                        }
                    }
                    else
                    {
                        _direction = moveVector;
                        if (_playerState == PlayerState.Starting)
                        {
                            if (_frame < _startingFrames - 1)
                            {
                                _frame++;
                                _speed += _acceleration;
                            }
                            else
                            {
                                _playerState = PlayerState.Running;
                                _frame = 0;
                                _delay = 0;
                                _speed = _runningSpeed;
                            }
                        }
                        else // _playerState == PlayerState.Running
                        {
                            _frame++;
                            _frame %= _runningFrames;
                        }
                    }
                }
                
                else
                {
                    if (_playerState == PlayerState.Running)
                    {
                        if (_delay < 3)
                        {
                            _delay++;
                            _frame++;
                            _frame %= _runningFrames;
                        }
                        else
                        {
                            _playerState = PlayerState.Stopping;
                            _frame = 0;
                        }
                    }
                    else
                    {
                        _playerState = PlayerState.Standing;
                        _frame = 0;
                        _speed = 0;
                    }
                }
            }

            public void SetLevel(Level level)
            {
                level.Collided += Collided;
            }

            public Texture2D GetFrame()
            {
                Texture2D activeSprite;
                Texture2D texture;
                
                switch (_playerState)
                {
                    case PlayerState.Standing:
                        activeSprite = _standing;
                        break;
                    
                    case PlayerState.Starting:
                        activeSprite = _starting;
                        break;
                    
                    case PlayerState.Running:
                        activeSprite = _running;
                        break;
                    
                    case PlayerState.Stopping:
                        activeSprite = _stopping;
                        break;
                    
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                var data = new Color[_standing.Width * _standing.Height];
                var source = new Rectangle(0, _standing.Height * _frame, _standing.Width, _standing.Height);
                
                activeSprite.GetData(0, source, data, 0, _standing.Width * _standing.Height);

                var rotatedData = new Color[_standing.Width * _standing.Height];
                if (_direction == Vector2.UnitY) // Downwards
                {
                    texture = new Texture2D(activeSprite.GraphicsDevice, _standing.Width, _standing.Height);
                    for (int i = 0; i < _standing.Width * _standing.Height; i++)
                    {
                        rotatedData[_standing.Width * _standing.Height - i - 1] = data[i];
                    }
                }
                else if (_direction == Vector2.UnitX) // Right
                {
                    texture = new Texture2D(activeSprite.GraphicsDevice, _standing.Height, _standing.Width);
                    for (int i = 0; i < _standing.Width; i++)
                    {
                        for (int j = 0; j < _standing.Height; j++)
                        {
                            rotatedData[i * _standing.Height + _standing.Width - j - 1] = data[j * _standing.Width + i];
                        }
                    }
                }
                else if (_direction == -Vector2.UnitX) // Left
                {
                    texture = new Texture2D(activeSprite.GraphicsDevice, _standing.Height, _standing.Width);
                    for (int i = 0; i < _standing.Width; i++)
                    {
                        for (int j = 0; j < _standing.Height; j++)
                        {
                            rotatedData[(_standing.Height - i - 1) * _standing.Height + j] = data[j * _standing.Width + i];
                        }
                    }
                }
                else
                {
                    texture = new Texture2D(activeSprite.GraphicsDevice, _standing.Width, _standing.Height);
                    rotatedData = data;
                }
                
                texture.SetData(rotatedData);

                return texture;
            }

            private Rectangle GetHitbox()
            {
                Rectangle hitbox;
                
                if (_direction == Vector2.UnitY) // Downwards
                {
                    hitbox = new Rectangle(_standing.Width - _hitbox.Right, _standing.Height - _hitbox.Bottom, _hitbox.Width, _hitbox.Height);
                }
                else if (_direction == Vector2.UnitX) // Right
                {
                    hitbox = new Rectangle(_standing.Height - _hitbox.Bottom, _hitbox.Left, _hitbox.Height, _hitbox.Width);
                }
                else if (_direction == -Vector2.UnitX) // Left
                {
                    hitbox = new Rectangle(_hitbox.Top, _standing.Width - _hitbox.Right, _hitbox.Height, _hitbox.Width);
                }
                else // Upwards (default)
                {
                    hitbox = _hitbox;
                }

                return hitbox;
            }

            private void Collided(object sender, CollisionEventArgs e)
            {
                Position = e.LastValidPosition;
            }
        }

        public class Camera
        {
            public Vector2 Position { get; set; }
            public CameraState CameraState { get; set; }
            public Player Target { get; set; }

            public Camera()
            {
                Position = Vector2.Zero;
                CameraState = CameraState.Free;
            }

            public Camera(Vector2 position, Player target)
            {
                Position = position;
                Target = target;
                CameraState = CameraState.Locked;
            }

            public void Move(Vector2 moveVector, GameTime gameTime)
            {
                if (CameraState == CameraState.Free)
                {
                    Position += moveVector * (float) gameTime.ElapsedGameTime.TotalSeconds;
                }
            }

            public void Update(GameTime gameTime)
            {
                if (Target != null && CameraState == CameraState.Locked)
                {
                    Position = Target.Position + new Vector2(Target.Hitbox.Center.X, Target.Hitbox.Center.Y);
                }
            }
        }
        
        public class Quiz
        {
            public Texture2D Texture { get; }

            public Quiz(Texture2D texture)
            {
                Texture = texture;
            }
        }

        public enum PlayerState
        {
            Standing = 0,
            Starting = 1,
            Running = 2,
            Stopping = 3
        }

        public enum CameraState
        {
            Free = 0,
            Locked = 1,
            Chase = 2
        }

        public enum GameState
        {
            Loading = 0,
            Game = 1,
            Quiz = 2
        }

        public class CollisionEventArgs : EventArgs
        {
            public Vector2 LastValidPosition { get; }

            public CollisionEventArgs(Vector2 lastValidPosition)
            {
                LastValidPosition = lastValidPosition;
            }
        }

        public delegate void CollisionEventHandler(object sender, CollisionEventArgs e);
    }
}