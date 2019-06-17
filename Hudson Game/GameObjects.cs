using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.SymbolStore;
using System.Dynamic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Timers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Data.SQLite;

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
            public VehicleLane[] VehicleLanes { get; }
            public List<Litter> LitterList { get; private set; }
            
            public bool InvincibilityFrames { get; private set; }
            public float InvincibilityTime => (float) _invincibilityStopwatch.Elapsed.TotalSeconds;

            public bool RespawnEnabled
            {
                get => Player.RespawnEnabled;
                set => Player.RespawnEnabled = value;
            }

            public event CollisionEventHandler Collided;
            public event VehicleHitEventHandler VehicleHit;
            public event PointsEventHandler LitterPickedUp;

            private Vector2 _lastValidPlayerPosition;
            private Vector2 _lastValidCameraPosition;

            private readonly Stopwatch _invincibilityStopwatch;
            private readonly float _invincibilityTime;

            private bool _disableCollisions;

            private bool _stick;
            private Vector2 _stickPosition;
            private VehicleObject _stickVehicle;

            public Level(Texture2D texture, Rectangle playZone, Player player, Camera camera, EnvironmentObject[] environmentObjects, VehicleLane[] vehicleLanes, Litter[] litter, float invincibilityTime)
            {
                Texture = texture;
                PlayZone = playZone;
                Player = player;
                Camera = camera;
                EnvironmentObjects = environmentObjects;
                VehicleLanes = vehicleLanes;
                LitterList = litter.ToList();
                _invincibilityTime = invincibilityTime;

                Player.SetLevel(this);
                _invincibilityStopwatch = new Stopwatch();

                _lastValidPlayerPosition = Player.Position;
                _lastValidCameraPosition = Camera.Position;

                RespawnEnabled = true;
            }

            public void Update(GameTime gameTime, KeyboardState keyboardState)
            {
                var collided = false;
                
                Player.Update(gameTime, keyboardState);
                foreach (var vehicleLane in VehicleLanes)
                {
                    vehicleLane.Update(gameTime);
                }

                if (!_disableCollisions) // Collisions
                {
                    if (Player.Position.X + Player.Hitbox.Right > PlayZone.Width ||
                        Player.Position.X + Player.Hitbox.Left < 0 ||
                        Player.Position.Y + Player.Hitbox.Bottom > PlayZone.Height ||
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

                    foreach (var litter in LitterList.ToList())
                    {
                        if (new Rectangle((int) (litter.Position.X + litter.Hitbox.X),
                                (int) (litter.Position.Y + litter.Hitbox.Y), litter.Hitbox.Width, litter.Hitbox.Height)
                            .Intersects(new Rectangle((int) (Player.Position.X + Player.Hitbox.X),
                                (int) (Player.Position.Y + Player.Hitbox.Y), Player.Hitbox.Width,
                                Player.Hitbox.Height)))
                        {
                            litter.PickUp();
                            OnLitterPickedUp(litter);
                            LitterList.Remove(litter);
                        }
                    }

                    if (!InvincibilityFrames)
                    {
                        foreach (var vehicleLane in VehicleLanes)
                        {
                            if (vehicleLane.VehicleObjects != null)
                            {
                                foreach (var vehicleObject in vehicleLane.VehicleObjects.ToList())
                                {
                                    if (new Rectangle((int) (vehicleObject.Position.X + vehicleObject.Hitbox.X),
                                        (int) (vehicleObject.Position.Y + vehicleObject.Hitbox.Y),
                                        vehicleObject.Hitbox.Width,
                                        vehicleObject.Hitbox.Height).Intersects(new Rectangle(
                                        (int) (Player.Position.X + Player.Hitbox.X),
                                        (int) (Player.Position.Y + Player.Hitbox.Y),
                                        Player.Hitbox.Width, Player.Hitbox.Height)))
                                    {
                                        Vector2 direction;
                                        var pureDirection =
                                            (Player.Position + new Vector2(Player.Hitbox.Center.X,
                                                 Player.Hitbox.Center.Y)) -
                                            (vehicleObject.Position + new Vector2(vehicleObject.Hitbox.Center.X,
                                                 vehicleObject.Hitbox.Center.Y));

                                        if (Math.Abs(pureDirection.X) > Math.Abs(pureDirection.Y))
                                        {
                                            direction = Vector2.UnitX * (pureDirection.X > 0 ? 1 : -1);
                                        }
                                        else
                                        {
                                            direction = Vector2.UnitY * (pureDirection.Y > 0 ? 1 : -1);
                                        }

                                        OnVehicleHit(direction, vehicleObject.HitType);

                                        InvincibilityFrames = true;
                                        _disableCollisions = true;
                                        _invincibilityStopwatch.Reset();
                                        _invincibilityStopwatch.Start();

                                        Camera.CameraState = CameraState.Chase;
                                        Camera.ChaseFactor = 2f;
                                        Camera.SmoothFactor = 5f;
                                        Player.StopChase += StopChaseCamera;

                                        Player.Respawned += EnabledCollisions;

                                        if (vehicleObject.HitType == HitType.Stick)
                                        {
                                            _stick = true;
                                            _stickVehicle = vehicleObject;
                                            _stickPosition = Player.Position - _stickVehicle.Position;
                                            Player.Respawned += Unstick;
                                        }

                                        break;
                                    }
                                }
                            }
                        }
                    }
                }

                if (_stick)
                {
                    OnCollision(_stickVehicle.Position + _stickPosition);
                }
                
                if (InvincibilityTime > _invincibilityTime)
                {
                    _invincibilityStopwatch.Stop();
                    InvincibilityFrames = false;
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

            private void OnVehicleHit(Vector2 direction, HitType hitType)
            {
                VehicleHit?.Invoke(this, new VehicleHitEventArgs(direction, hitType));
            }

            private void OnLitterPickedUp(Litter litter)
            {
                LitterPickedUp?.Invoke(this, new PointsEventArgs(litter.Points));
            }

            private void StopChaseCamera(object sender, EventArgs e)
            {
                Camera.CameraState = CameraState.Smooth;
                Camera.SmoothFactor = 0.03f;

                Player.StopChase -= StopChaseCamera;
                Player.Respawned += RespawnedCamera;
            }

            private void RespawnedCamera(object sender, EventArgs e)
            {
                Camera.ChaseToLock();
                Camera.ChaseFactor = 3f;
                Camera.SmoothFactor = 10f;

                Player.Respawned -= RespawnedCamera;
            }

            private void EnabledCollisions(object sender, EventArgs e)
            {
                _disableCollisions = false;
                Player.Respawned -= EnabledCollisions;
            }

            private void Unstick(object sender, EventArgs e)
            {
                _stick = false;
                _stickVehicle = null;
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

        public struct Vehicle
        {
            public Texture2D Texture { get; }
            public Rectangle Hitbox { get; }
            public float Speed { get; }
            public HitType HitType { get; }

            public Vehicle(Texture2D texture, Rectangle hitbox, float speed, HitType hitType)
            {
                Texture = texture;
                Hitbox = hitbox;
                Speed = speed;
                HitType = hitType;
            }
        }

        public class VehicleLane
        {
            public List<VehicleObject> VehicleObjects { get; }

            private readonly Vehicle _vehicle;
            private readonly float _laneLevel;
            private readonly float _spawnPoint;
            private readonly float _despawnPoint;
            private readonly float _minTimeDelay;
            private readonly float _spawnWindow;

            private readonly Random _random;
            private readonly Timer _timer; // System.Timers.Timer

            public VehicleLane(Vehicle vehicle, float laneLevel, float spawnPoint, float despawnPoint, float minTimeDelay, float spawnWindow)
            {
                if (despawnPoint - spawnPoint < 0)
                {
                    var data = new Color[vehicle.Texture.Width * vehicle.Texture.Height];
                    vehicle.Texture.GetData(data);

                    var newData = new Color[vehicle.Texture.Width * vehicle.Texture.Height];
                    for (int i = 0; i < vehicle.Texture.Width * vehicle.Texture.Height; i++)
                    {
                        newData[vehicle.Texture.Width * vehicle.Texture.Height - i - 1] = data[i];
                    }

                    var texture = new Texture2D(vehicle.Texture.GraphicsDevice, vehicle.Texture.Width, vehicle.Texture.Height);
                    texture.SetData(newData);

                    var hitbox = new Rectangle(vehicle.Texture.Width - vehicle.Hitbox.Right, vehicle.Texture.Height - vehicle.Hitbox.Bottom, vehicle.Hitbox.Width, vehicle.Hitbox.Height);
                    
                    _vehicle = new Vehicle(texture, hitbox, vehicle.Speed, vehicle.HitType);
                }
                else
                {
                    _vehicle = vehicle;
                }

                _laneLevel = laneLevel;
                _spawnPoint = spawnPoint;
                _despawnPoint = despawnPoint;
                _minTimeDelay = minTimeDelay;
                _spawnWindow = spawnWindow;

                _timer = new Timer {AutoReset = false};
                _timer.Elapsed += Spawn;
                
                _random = new Random();
                
                VehicleObjects = new List<VehicleObject>();
            }

            public void BeginSpawning()
            {
                _timer.Interval = _random.NextDouble() * _spawnWindow + _minTimeDelay;
                _timer.Enabled = true;
            }

            public void EndSpawning()
            {
                _timer.Enabled = false;
            }

            public void Update(GameTime gameTime)
            {
                foreach (var vehicleObject in VehicleObjects.ToList())
                {
                    vehicleObject.Update(gameTime);
                }
            }

            private void Spawn(object sender, ElapsedEventArgs e)
            {
                var vehicle = new VehicleObject(_vehicle, new Vector2(_spawnPoint, _laneLevel), _despawnPoint);
                vehicle.GoalReached += RemoveVehicle;
                VehicleObjects.Add(vehicle);
                
                _timer.Interval = _random.NextDouble() * (_spawnWindow - _minTimeDelay) + _minTimeDelay;
                _timer.Enabled = true;
            }

            private void RemoveVehicle(object sender, EventArgs e)
            {
                var vehicle = (VehicleObject) sender;

                VehicleObjects.Remove(vehicle);
                vehicle.GoalReached -= RemoveVehicle;
            }
        }

        public class VehicleObject
        {
            public Texture2D Texture { get; }
            public Vector2 Position { get; private set; }
            public Rectangle Hitbox { get; }
            public HitType HitType { get; }

            private readonly float _speed;
            private readonly float _goal;

            public event EventHandler GoalReached;

            public VehicleObject(Vehicle vehicle, Vector2 position, float goal)
            {
                _goal = goal;
                Position = position;
                HitType = vehicle.HitType;
                _speed = vehicle.Speed * (goal - position.X > 0 ? 1 : -1);
                Texture = vehicle.Texture;
                Hitbox = vehicle.Hitbox;
            }

            public void Update(GameTime gameTime)
            {
                Position += new Vector2(_speed, 0) * (float) gameTime.ElapsedGameTime.TotalSeconds;
                if (Position.X > _goal && _speed > 0 || Position.X < _goal && _speed < 0)
                {
                    OnGoalReached();
                }
            }

            private void OnGoalReached()
            {
                GoalReached?.Invoke(this, EventArgs.Empty);
            }
        }
        
        public class Litter
        {
            public Texture2D Texture { get; }
            public Rectangle Hitbox { get; }
            public Vector2 Position { get; }
            public int Points { get; }

            public event PointsEventHandler PickedUp;

            public Litter(Texture2D texture, Rectangle hitbox, Vector2 position, int points)
            {
                Texture = texture;
                Hitbox = hitbox;
                Position = position;
                Points = points;
            }

            public void PickUp()
            {
                OnPickedUp();
            }

            private void OnPickedUp()
            {
                PickedUp?.Invoke(this, (PointsEventArgs) EventArgs.Empty);
            }
        }

        public class Player
        {
            public Vector2 Position { get; private set; }
            public Rectangle Hitbox => GetHitbox();
            public Texture2D CurrentFrame => GetFrame();
            public bool RespawnEnabled { get; set; }
            
            private readonly Texture2D _standing;
            private readonly Texture2D _starting;
            private readonly Texture2D _running;
            private readonly Texture2D _stopping;
            private readonly Texture2D _hit;
            
            private readonly int _startingFrames;
            private readonly int _runningFrames;
            private readonly int _stoppingFrames;
            private readonly int _knockbackFrames;
            private readonly int _hitChaseFrames;

            private readonly float _runningSpeed;
            private readonly float _acceleration;
            private readonly float _deceleration;
            private readonly float _knockbackSpeed;
            private readonly float _knockbackDeceleration;

            private readonly Vector2 _spawnPosition;
            private readonly Rectangle _hitbox;
            
            private Vector2 _direction = -Vector2.UnitX;
            private float _speed; // Default 0 at initialisation
            private PlayerState _playerState = PlayerState.Standing;
            private KeyboardState _keyboardState;
            private int _frame; // Default 0 at initialisation
            private int _delay; // Default 0 at initialisation

            public event EventHandler Respawned;
            public event EventHandler StopChase;

            public Player(Texture2D standing, Texture2D starting, Texture2D running, Texture2D stopping, Texture2D hit, Rectangle hitbox, int startingFrames, int runningFrames, int stoppingFrames, int knockbackFrames, int hitChaseFrames, float runningSpeed, float knockbackSpeed, Vector2 spawnPosition)
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
                _knockbackFrames = knockbackFrames;
                _hitChaseFrames = hitChaseFrames;

                _runningSpeed = runningSpeed;
                _acceleration = runningSpeed / startingFrames;
                _deceleration = runningSpeed / stoppingFrames;
                _knockbackSpeed = knockbackSpeed;
                _knockbackDeceleration = knockbackSpeed / knockbackFrames;

                _spawnPosition = spawnPosition;
                Position = spawnPosition;

                RespawnEnabled = true;
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

                if (_playerState == PlayerState.Hit)
                {
                    if (_delay < _knockbackFrames - 1)
                    {
                        _delay++;
                        _speed -= _knockbackDeceleration;
                        if (_delay >= _hitChaseFrames)
                        {
                            OnStopChase();
                        }
                    }
                    else if (RespawnEnabled)
                    {
                        _playerState = PlayerState.Standing;
                        _frame = 0;
                        _speed = 0;
                        Position = _spawnPosition;
                        OnRespawned();
                    }
                }
                
                else if (_playerState == PlayerState.Stopping)
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

            private void OnStopChase()
            {
                StopChase?.Invoke(this, EventArgs.Empty);
            }

            private void OnRespawned()
            {
                Respawned?.Invoke(this, EventArgs.Empty);
            }

            public void SetLevel(Level level)
            {
                level.Collided += Collided;
                level.VehicleHit += VehicleHit;
            }

            private Texture2D GetFrame()
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

                    case PlayerState.Hit:
                        activeSprite = _hit;
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

            private void VehicleHit(object sender, VehicleHitEventArgs e)
            {
                _playerState = PlayerState.Hit;
                _frame = 0;
                _delay = 0;
                _direction = e.Direction;
                if (e.HitType == HitType.Knockback)
                {
                    _speed = _knockbackSpeed;
                }
                else
                {
                    _speed = 0;
                }
            }
        }

        public class Camera
        {
            public Vector2 Position { get; set; }
            public CameraState CameraState { get; set; }
            public Player Target { get; set; }
            public float ChaseFactor { get; set; } = 1;
            public float SmoothFactor { get; set; } = 1;

            private Vector2 _velocity;
            private bool _chaseToLock;

            public float Speed => _velocity.Length();

            public event EventHandler LockedFromChase;

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
                if (Target != null)
                {
                    var position = Target.Position + new Vector2(Target.Hitbox.Center.X, Target.Hitbox.Center.Y);
                    
                    if (CameraState == CameraState.Locked)
                    {
                        var newPosition = position;
                        _velocity = (newPosition - Position) / (float) gameTime.ElapsedGameTime.TotalSeconds;
                        Position = newPosition;
                    }
                    else if (CameraState == CameraState.Chase)
                    {
                        var direction = (position - Position);
                        var distance = direction.Length();
                        direction.Normalize();

                        if (distance < 1f)
                        {
                            if (_chaseToLock)
                            {
                                CameraState = CameraState.Locked;
                                _chaseToLock = false;
                                OnLockedFromChase();
                            }
                            else
                            {
                                _velocity = Vector2.Zero;
                                Position = position;
                            }
                        }
                        else
                        {
                            distance /= SmoothFactor * ChaseFactor * 10000;
                            _velocity = direction * (float) (ChaseFactor * 10000 * Math.Sqrt(distance * (distance + 2)) / (distance + 1));
                            Position += _velocity * (float) gameTime.ElapsedGameTime.TotalSeconds;
                        }
                    }
                }

                if (CameraState == CameraState.Smooth)
                {
                    _velocity *= (float) Math.Pow(Math.Exp(SmoothFactor) / (Math.Exp(SmoothFactor) + 50), gameTime.ElapsedGameTime.TotalSeconds);

                    Position += _velocity * (float) gameTime.ElapsedGameTime.TotalSeconds;
                }
            }

            public void ChaseToLock()
            {
                CameraState = CameraState.Chase;
                _chaseToLock = true;
            }

            private void OnLockedFromChase()
            {
                LockedFromChase?.Invoke(this, EventArgs.Empty);
            }
        }

        public class Quiz
        {
            public Color BackgroundColor { get; }
            public string QuestionText { get; private set; }
            public Vector2 QuestionPosition { get; }
            public AnswerBox[] AnswerBoxes { get; }

            private readonly Stopwatch _answerDelay;
            private readonly Random _random;
            
            private bool _canAnswer;
            private List<QuestionAndAnswers> _questionsAndAnswers;
            private QuestionAndAnswers _activeQuestionAndAnswers;

            public event EventHandler QuizOver;
            public event PointsEventHandler CorrectlyAnswered;

            public Quiz(Color backgroundColor, Texture2D answerBoxIdle, Texture2D answerBoxHover, Texture2D answerBoxClick, Vector2 questionPosition, Rectangle answerSpace, int difficulty)
            {
                BackgroundColor = backgroundColor;
                QuestionPosition = questionPosition;
                QuestionText = "Question";

                AnswerBoxes = new AnswerBox[4];
                var rectangles = new Rectangle[4];
                rectangles[0] = new Rectangle(answerSpace.Location, new Point(answerSpace.Width / 2, answerSpace.Height / 2));
                rectangles[1] = new Rectangle(new Point(answerSpace.X + answerSpace.Width / 2, answerSpace.Y), new Point(answerSpace.Width / 2, answerSpace.Height / 2));
                rectangles[2] = new Rectangle(new Point(answerSpace.X, answerSpace.Y + answerSpace.Height / 2), new Point(answerSpace.Width / 2, answerSpace.Height / 2));
                rectangles[3] = new Rectangle(new Point(answerSpace.X + answerSpace.Width / 2, answerSpace.Y + answerSpace.Height / 2), new Point(answerSpace.Width / 2, answerSpace.Height / 2));

                for (int i = 0; i < 4; i++)
                {
                    var questionBox = new AnswerBox(answerBoxIdle, answerBoxHover, answerBoxClick,
                        new Vector2(rectangles[i].Center.X - answerBoxIdle.Bounds.Center.X,
                            rectangles[i].Center.Y - answerBoxIdle.Bounds.Center.Y));
                    AnswerBoxes[i] = questionBox;
                }
                
                _answerDelay = new Stopwatch();
                _canAnswer = true;
                _questionsAndAnswers = new List<QuestionAndAnswers>();

                var connection = new SQLiteConnection("Data Source=Content/Data;Version=3");
                connection.Open();

                var read = "SELECT * FROM Questions WHERE Difficulty = @difficulty";
                var command = new SQLiteCommand(read, connection);
                command.Parameters.AddWithValue("@difficulty", difficulty);

                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    _questionsAndAnswers.Add(new QuestionAndAnswers((string) reader["Question"], (string) reader["Answer1"],
                        (string) reader["Answer2"], (string) reader["Answer3"], (string) reader["Answer4"], (int)(long) reader["CorrectAnswer"],
                        (int)(long) reader["Points"]));
                }
                
                _random = new Random();

                if (_questionsAndAnswers.Any())
                {
                    var random = _random.Next(_questionsAndAnswers.Count);
                    UpdateQuiz(_questionsAndAnswers[random]);
                    _questionsAndAnswers.RemoveAt(random);
                }
                else
                {
                    OnQuizOver();
                }
            }

            public void Update(GameTime gameTime, MouseState mouseState)
            {
                if (_canAnswer)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        if (new Rectangle(new Point((int) AnswerBoxes[i].Position.X, (int) AnswerBoxes[i].Position.Y),
                            AnswerBoxes[i].Texture.Bounds.Size).Contains(mouseState.Position))
                        {
                            if (mouseState.LeftButton == ButtonState.Pressed)
                            {
                                AnswerBoxes[i].AnswerBoxState = AnswerBoxState.Click;
                            }
                            else if (AnswerBoxes[i].AnswerBoxState == AnswerBoxState.Click)
                            {
                                if (i == _activeQuestionAndAnswers.CorrectAnswerIndex)
                                {
                                    QuestionText = "Correct!";
                                    OnCorrectlyAnswered(_activeQuestionAndAnswers);
                                }
                                else
                                {
                                    QuestionText = "Incorrect!";
                                }
                                
                                AnswerBoxes[i].AnswerBoxState = AnswerBoxState.Hover;
                                
                                _canAnswer = false;
                                _answerDelay.Reset();
                                _answerDelay.Start();
                            }
                            else
                            {
                                AnswerBoxes[i].AnswerBoxState = AnswerBoxState.Hover;
                            }
                        }
                        else
                        {
                            AnswerBoxes[i].AnswerBoxState = AnswerBoxState.Idle;
                        }
                    }
                }
                else if (_answerDelay.IsRunning && _answerDelay.Elapsed.TotalSeconds > 1)
                {
                    if (_questionsAndAnswers.Any())
                    {
                        var random = _random.Next(_questionsAndAnswers.Count);
                        UpdateQuiz(_questionsAndAnswers[random]);
                        _questionsAndAnswers.RemoveAt(random);
                        _canAnswer = true;
                    }
                    else
                    {
                        OnQuizOver();
                    }
                }
            }

            private void UpdateQuiz(QuestionAndAnswers questionAndAnswers)
            {
                _activeQuestionAndAnswers = questionAndAnswers;
                
                QuestionText = questionAndAnswers.Question;
                for (int i = 0; i < 4; i++)
                {
                    AnswerBoxes[i].Text = questionAndAnswers.Answers[i];
                }
            }

            private void OnQuizOver()
            {
                QuizOver?.Invoke(this, EventArgs.Empty);
            }

            private void OnCorrectlyAnswered(QuestionAndAnswers questionAndAnswers)
            {
                CorrectlyAnswered?.Invoke(this, new PointsEventArgs(questionAndAnswers.Points));
            }

            private struct QuestionAndAnswers
            {
                public string Question { get; }
                public string[] Answers { get; }
                public int CorrectAnswerIndex { get; }
                public int Points { get; }

                public QuestionAndAnswers(string question, string answer1, string answer2, string answer3, string answer4, int correctAnswerIndex, int points)
                {
                    Question = question;
                    Answers = new[] {answer1, answer2, answer3, answer4};
                    CorrectAnswerIndex = correctAnswerIndex;
                    Points = points;
                }
            }
        }

        public class AnswerBox
        {
            public AnswerBoxState AnswerBoxState { get; set; }
            public Texture2D Texture => GetTexture();
            public Vector2 Position { get; }
            public string Text { get; set; }

            private readonly Texture2D _idleTexture;
            private readonly Texture2D _hoverTexture;
            private readonly Texture2D _clickTexture;

            public AnswerBox(Texture2D idleTexture, Texture2D hoverTexture, Texture2D clickTexture, Vector2 position)
            {
                _idleTexture = idleTexture;
                _hoverTexture = hoverTexture;
                _clickTexture = clickTexture;
                Position = position;
                AnswerBoxState = AnswerBoxState.Idle;
            }

            private Texture2D GetTexture()
            {
                Texture2D texture;
                
                switch (AnswerBoxState)
                {
                    case AnswerBoxState.Idle:
                        texture = _idleTexture;
                        break;
                    
                    case AnswerBoxState.Hover:
                        texture = _hoverTexture;
                        break;
                    
                    case AnswerBoxState.Click:
                        texture = _clickTexture;
                        break;
                    
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                return texture;
            }
        }

        private enum PlayerState
        {
            Standing = 0,
            Starting = 1,
            Running = 2,
            Stopping = 3,
            Hit = 4
        }

        public enum HitType
        {
            Knockback = 0,
            Stick = 1
        }

        public enum CameraState
        {
            Free = 0,
            Locked = 1,
            Chase = 2,
            Smooth = 3
        }

        public enum GameState
        {
            Loading = 0,
            Game = 1,
            Quiz = 2
        }

        public enum AnswerBoxState
        {
            Idle = 0,
            Hover = 1,
            Click = 2
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

        public class VehicleHitEventArgs : EventArgs
        {
            public Vector2 Direction { get; }
            public HitType HitType { get; }

            public VehicleHitEventArgs(Vector2 direction, HitType hitType)
            {
                Direction = direction;
                HitType = hitType;
            }
        }

        public delegate void VehicleHitEventHandler(object sender, VehicleHitEventArgs e);

        public class PointsEventArgs : EventArgs
        {
            public int Points { get; }

            public PointsEventArgs(int points)
            {
                Points = points;
            }
        }

        public delegate void PointsEventHandler(object sender, PointsEventArgs e);
    }
}