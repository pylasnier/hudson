using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

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

            public void Update(GameTime gameTime)
            {
                var collided = false;
                
                //Player.Update();

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
            public Texture2D Standing { get; }
            public Texture2D Starting { get; }
            public Texture2D Running { get; }
            public Texture2D Stopping { get; }
            public Rectangle Hitbox { get; }

            private readonly int _startingFrames;
            private readonly int _runningFrames;
            private readonly int _stoppingFrames;

            public Vector2 Position { get; private set; }
            public Vector2 Direction { get; private set; }

            public PlayerState PlayerState { get; private set; }
            public int Frame { get; private set; }

            public Player(Texture2D standing, Texture2D starting, Texture2D running, Texture2D stopping, Rectangle hitbox, int startingFrames, int runningFrames, int stoppingFrames, Vector2 position)
            {
                Standing = standing;
                Starting = starting;
                Running = running;
                Stopping = stopping;
                Hitbox = hitbox;
                _startingFrames = startingFrames;
                _runningFrames = runningFrames;
                _stoppingFrames = stoppingFrames;
                Position = position;

                PlayerState = PlayerState.Standing;
                Direction = -Vector2.UnitY;
                Frame = 0;
            }

            public void Move(Vector2 moveVector, GameTime gameTime)
            {
                Position += moveVector * (float) gameTime.ElapsedGameTime.TotalSeconds;
            }

            public void Update()
            {
                throw new System.NotImplementedException();
            }

            public void SetLevel(Level level)
            {
                level.Collided += Collided;
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