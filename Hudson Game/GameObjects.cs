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
            private readonly Texture2D _standing;
            private readonly Texture2D _starting;
            private readonly Texture2D _running;
            private readonly Texture2D _stopping;
            public Rectangle Hitbox { get; }

            private readonly int _startingFrames;
            private readonly int _runningFrames;
            private readonly int _stoppingFrames;

            public Vector2 Position { get; private set; }
            public Vector2 Direction { get; private set; }

            public PlayerState PlayerState { get; private set; }
            public Texture2D CurrentFrame => GetFrame();

            private KeyboardState _keyboardState;
            private int _frame;

            public Player(Texture2D standing, Texture2D starting, Texture2D running, Texture2D stopping, Rectangle hitbox, int startingFrames, int runningFrames, int stoppingFrames, Vector2 position)
            {
                _standing = standing;
                _starting = starting;
                _running = running;
                _stopping = stopping;
                Hitbox = hitbox;
                _startingFrames = startingFrames;
                _runningFrames = runningFrames;
                _stoppingFrames = stoppingFrames;
                Position = position;

                PlayerState = PlayerState.Standing;
                Direction = -Vector2.UnitY;
                _frame = 0;
            }

            public void Update(GameTime gameTime, KeyboardState keyboardState)
            {
                Position += Direction * (float) gameTime.ElapsedGameTime.TotalSeconds;
                _keyboardState = keyboardState;
            }

            public void FrameAdvance(object stateInfo)
            {
                if (_keyboardState.IsKeyDown(Keys.W))
                {
                    if (PlayerState == PlayerState.Standing)
                    {
                        PlayerState = PlayerState.Starting;
                        _frame = 0;
                    }
                    else if (PlayerState == PlayerState.Starting)
                    {
                        if (_frame < _startingFrames)
                        {
                            _frame++;
                        }
                        else
                        {
                            PlayerState = PlayerState.Running;
                            _frame = 0;
                            Direction = -Vector2.UnitY;
                        }
                    }
                    else if (PlayerState == PlayerState.Running)
                    {
                        _frame++;
                        _frame %= _runningFrames;
                    }
                }
                if (_keyboardState.IsKeyUp(Keys.W))
                {
                    if (PlayerState == PlayerState.Starting)
                    {
                        PlayerState = PlayerState.Standing;
                        _frame = 0;
                    }
                    else if (PlayerState == PlayerState.Running)
                    {
                        PlayerState = PlayerState.Stopping;
                        _frame = 0;
                    }
                    else if (PlayerState == PlayerState.Stopping)
                    {
                        if (_frame < _stoppingFrames)
                        {
                            _frame++;
                        }
                        else
                        {
                            PlayerState = PlayerState.Standing;
                            Direction = Vector2.Zero;
                            _frame = 0;
                        }
                    }
                }
            }

            public void SetLevel(Level level)
            {
                level.Collided += Collided;
            }

            public Texture2D GetFrame()
            {
                Texture2D texture;
                
                switch (PlayerState)
                {
                    case PlayerState.Standing:
                        texture = _standing;
                        break;
                    
                    case PlayerState.Starting:
                        texture = _standing;
                        break;
                    
                    case PlayerState.Running:
                        texture = new Texture2D(_running.GraphicsDevice, _standing.Width, _standing.Height);
                        var data = new Color[_standing.Width * _standing.Height];
                        var source = new Rectangle(0, _standing.Height * _frame, _standing.Width, _standing.Height);
                        _running.GetData(0, source, data, 0, _standing.Width * _standing.Height);
                        texture.SetData(data);
                        break;
                    
                    case PlayerState.Stopping:
                        texture = _standing;
                        break;
                    
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                return texture;
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