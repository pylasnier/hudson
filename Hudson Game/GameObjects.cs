using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Hudson_Game
{
    public static class GameObjects
    {
        public struct Level
        {
            public Texture2D Texture { get; }
            public Rectangle PlayZone { get; }

            public Level(Texture2D texture, Rectangle playZone)
            {
                Texture = texture;
                PlayZone = playZone;
            }
        }

        public struct EnvironmentObject
        {
            public Texture2D Texture { get; }
            public Vector2 Position { get; }
            public Vector2 Scale { get; }
            public bool CollisionEnabled { get; }

            public EnvironmentObject(Texture2D texture, Vector2 position, Vector2 scale, bool collisionEnabled)
            {
                Texture = texture;
                Position = position;
                Scale = scale;
                CollisionEnabled = collisionEnabled;
            }
        }

        public class Player
        {
            public Texture2D Standing { get; }
            public Texture2D Starting { get; }
            public Texture2D Running { get; }
            public Texture2D Stopping { get; }

            private readonly int _startingFrames;
            private readonly int _runningFrames;
            private readonly int _stoppingFrames;

            public Vector2 Position { get; private set; }
            public Vector2 Direction { get; private set; }

            public PlayerState PlayerState { get; private set; }
            public int Frame { get; private set; }

            public Player(Texture2D standing, Texture2D starting, Texture2D running, Texture2D stopping, int startingFrames, int runningFrames, int stoppingFrames, Vector2 position)
            {
                Standing = standing;
                Starting = starting;
                Running = running;
                Stopping = stopping;
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
                    Position = Target.Position;
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
    }
}