using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OGLonOTK.Graphics;
using OGLonOTK.World;
using OGLonOTK.Physics;

namespace OGLonOTK.Core
{
    public class Game : GameWindow
    {
        private Shader _shader;
        private Mesh _cubeMesh;
        private Drone _drone;
        private Camera _camera;
        private Shader _overlayShader;
        private Shader _overlayShapeShader;
        private OverlayMesh _circleOverlayMesh;
        private OverlayMesh _crossOverlayMesh;
        private List<GameObject> _sceneObjects = [];
        private List<GameObject> _obstacles = []; // препятствия для дрона
        private List<GameObject> _supportObjects = []; // объекты для держания сферы
        private List<TexturedObject> _texturedSceneObjects = []; // объекты текстурирования (так-то дублируют, но исправлю когда-нибудь "потом")
        private Mesh _sphereMesh;
        private CargoSphere _cargoSphere;
        private GameObject _block1; // Отдельно чтобы не искать среди sceneObjects
        private Shader _texturedShader;
        private Texture _wallTexture; // Надо бы завести словарь что ли для текстур...
        private Texture _floorTexture;
        private Texture _pillarTexture;
        private Texture _blockTexture;
        private TexturedMesh _texturedCubeMesh;
        private TexturedMesh _wallTexturedCubeMesh;
        private TexturedMesh _floorTexturedCubeMesh;
        private TexturedMesh _objectTexturedCubeMesh;

        // временные показатели
        private float _cargoReleaseCooldown = 0f;
        private float _cargoLostIndicatorTimer = 0f;
        private float _droneInnerAngularVelocity = 0f;
        private float _droneInnerRotationY = 0f;

        private Mesh _importedDroneBodyMesh;
        private Texture _ironTexture2;
        private Texture _ironTexture4;
        private Texture _stoneTexture;
        private List<TexturedDronePart> _droneTexturedInnerParts = []; // части дрона, на будущее

        private Mesh _revolutionMesh;
        private GameObject _revolutionObject;
        private Shader _billboardShader;
        private Texture _markerTexture;
        private TexturedMesh _billboardMesh;
        private BillboardObject _revolutionMarker;

        private List<Particle> _particles = new();
        private Random _random = new Random();

        private CameraMode _cameraMode = CameraMode.Follow;
        private bool _cursorCaptured = false;

        private float _orbitYaw = 0f;
        private float _orbitPitch = -15f;
        private float _orbitDistance = 6f;

        private Vector2 _lastMousePosition;
        private bool _firstMouseMove = true;
        private float _mouseSensitivity = 0.2f;

        public Game(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
            : base(gameWindowSettings, nativeWindowSettings)
        {
        }
        private enum CargoIndicatorState
        {
            None,
            CanAttach,
            Attached,
            Lost
        }

        private enum CameraMode
        {
            Follow,
            Orbit,
            Free
        }

        protected override void OnLoad()
        {
            base.OnLoad();

            GL.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);
            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            _shader = new Shader("Shaders/shader.vert", "Shaders/shader.frag");

            var (cubeVertices, cubeIndices) = MeshFactory.CreateCubeVertices();
            _cubeMesh = new Mesh(cubeVertices, cubeIndices);
            _cubeMesh.Load();

            var (wallVertices, wallIndices) = MeshFactory.CreateTexturedCubeVertices(4.0f);
            _wallTexturedCubeMesh = new TexturedMesh(wallVertices, wallIndices);
            _wallTexturedCubeMesh.Load();

            var (floorVertices, floorIndices) = MeshFactory.CreateTexturedCubeVertices(9.0f);
            _floorTexturedCubeMesh = new TexturedMesh(floorVertices, floorIndices);
            _floorTexturedCubeMesh.Load();

            var (objectVertices, objectIndices) = MeshFactory.CreateTexturedCubeVertices(0.25f);
            _objectTexturedCubeMesh = new TexturedMesh(objectVertices, objectIndices);
            _objectTexturedCubeMesh.Load();

            var (texturedVertices, texturedIndices) = MeshFactory.CreateTexturedCubeVertices(1.0f);
            _texturedCubeMesh = new TexturedMesh(texturedVertices, texturedIndices);
            _texturedCubeMesh.Load();

            _texturedShader = new Shader("Shaders/textured.vert", "Shaders/textured.frag");
            _wallTexture = new Texture("Textures/wall.png");
            _floorTexture = new Texture("Textures/desertstone.png");
            _pillarTexture = new Texture("Textures/wood.png");
            _blockTexture = new Texture("Textures/stone.png");

            _ironTexture2 = new Texture("Textures/IronNoRzavoeTexture_2.jpg");
            _ironTexture4 = new Texture("Textures/IronNoRzavoeTexture_4.jpg");
            _stoneTexture = new Texture("Textures/stone_texture.jpg");

            var materialMap = new Dictionary<string, Texture>
            {
                { "Материал", _ironTexture2 },
                { "Материал.001", _ironTexture4 },
                { "Материал.002", _stoneTexture }
            };

            var subMeshes = ObjTexturedLoader.Load("Assets/Vint.obj", materialMap);

            _droneTexturedInnerParts.Clear();

            foreach (var subMesh in subMeshes)
            {
                var part = new TexturedDronePart(subMesh.Mesh, _texturedShader, subMesh.Texture)
                {
                    LocalPosition = Vector3.Zero,
                    LocalRotation = Vector3.Zero,
                    LocalScale = new Vector3(1.2f, 2.0f, 1.2f)
                };

                _droneTexturedInnerParts.Add(part);
            }

            // Проверка перед CreateScene()
            Console.WriteLine(_texturedShader == null);
            Console.WriteLine(_wallTexture == null);
            Console.WriteLine(_texturedCubeMesh == null);
            CreateScene();

            // Поверхность вращения
            var controlPoints = new List<Vector2>
            {
                new Vector2(0.0f, 0.0f),
                new Vector2(0.25f, 0.2f),
                new Vector2(0.55f, 0.8f),
                new Vector2(0.35f, 1.3f),
                new Vector2(0.6f, 1.9f),
                new Vector2(0.2f, 2.4f)
            };

            var (revolutionVertices, revolutionIndices) = MeshFactory.CreateSurfaceOfRevolution(
                controlPoints,
                samplesPerSegment: 12,
                radialSegments: 32,
                color: new Vector3(0.8f, 0.4f, 0.2f));

            _revolutionMesh = new Mesh(revolutionVertices, revolutionIndices);
            _revolutionMesh.Load();

            _revolutionObject = new GameObject(_revolutionMesh, _shader)
            {
                Position = new Vector3(0.0f, 0.0f, 36.0f),
                Rotation = Vector3.Zero,
                Scale = Vector3.One
            };

            _sceneObjects.Add(_revolutionObject);

            // Указатель спрайтовый
            _billboardShader = new Shader("Shaders/billboard.vert", "Shaders/billboard.frag");
            _markerTexture = new Texture("Textures/marker.png");

            var (quadVertices, quadIndices) = MeshFactory.CreateTexturedQuad();
            _billboardMesh = new TexturedMesh(quadVertices, quadIndices);
            _billboardMesh.Load();

            _revolutionMarker = new BillboardObject(_billboardMesh, _billboardShader, _markerTexture)
            {
                Position = _revolutionObject.Position + new Vector3(0f, 3.0f, 0f),
                Size = new Vector2(1.2f, 1.2f)
            };

            float sphereRadius = 0.4f;

            var (sphereVertices, sphereIndices) = MeshFactory.CreateUvSphere(
                sphereRadius,
                16,
                24,
                new Vector3(1.0f, 0.8f, 0.2f));

            _sphereMesh = new Mesh(sphereVertices, sphereIndices);
            _sphereMesh.Load();

            _cargoSphere = new CargoSphere(_sphereMesh, _shader, sphereRadius)
            {
                Position = new Vector3(
                    _block1.Position.X,
                    _block1.Position.Y + _block1.Scale.Y / 2.0f + sphereRadius,
                    _block1.Position.Z),
                Rotation = Vector3.Zero,
                Scale = Vector3.One,
                IsGrounded = true
            };

            _sceneObjects.Add(_cargoSphere);

            _drone = new Drone(_cubeMesh, _shader)
            {
                Position = Vector3.Zero,
                Rotation = Vector3.Zero,
                Scale = new Vector3(1.0f, 0.3f, 1.0f)
            };

            var (importedVertices, importedIndices) = ObjLoader.LoadAsColoredMesh("Assets/Vint.obj", new Vector3(0.7f, 0.7f, 0.7f));

            _importedDroneBodyMesh = new Mesh(importedVertices, importedIndices);
            _importedDroneBodyMesh.Load();

            BuildDroneParts();

            // Оверлей
            _overlayShapeShader = new Shader("Shaders/overlay.vert", "Shaders/overlay.frag");

            var (circleVertices, circleIndices) = MeshFactory.CreateCircle2D(20f, 32);
            _circleOverlayMesh = new OverlayMesh(circleVertices, circleIndices);
            _circleOverlayMesh.Load();

            var (crossVertices, crossIndices) = MeshFactory.CreateCross2D(18f, 4f);
            _crossOverlayMesh = new OverlayMesh(crossVertices, crossIndices);
            _crossOverlayMesh.Load();

            _camera = new Camera(new Vector3(0.0f, 2.0f, 5.0f));

            SetCursorCaptured(false);
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);

            float dt = (float)e.Time;
            var input = KeyboardState;

            // Часть костыля убирания скорости вперёд у груза из ниоткуда при его отпускании
            if (_cargoReleaseCooldown > 0f)
            {
                _cargoReleaseCooldown -= dt;
                if (_cargoReleaseCooldown < 0f)
                    _cargoReleaseCooldown = 0f;
            }

            if (_cargoLostIndicatorTimer > 0f)
            {
                _cargoLostIndicatorTimer -= dt;
                if (_cargoLostIndicatorTimer < 0f)
                    _cargoLostIndicatorTimer = 0f;
            }

            if (input.IsKeyDown(Keys.Escape))
            {
                Close();
            }

            if (input.IsKeyPressed(Keys.F1))
            {
                _cameraMode = CameraMode.Follow;
                SetCursorCaptured(false);
            }

            if (input.IsKeyPressed(Keys.F2))
            {
                _cameraMode = CameraMode.Orbit;
                SetCursorCaptured(true);
                SyncOrbitCameraFromCurrentPosition();
            }

            if (input.IsKeyPressed(Keys.F3))
            {
                _cameraMode = CameraMode.Free;
                SetCursorCaptured(true);
            }

            if (_cameraMode != CameraMode.Free)
            {
                if (input.IsKeyPressed(Keys.F))
                {
                    ToggleCargoAttachment();
                }

                var rotation = _drone.Rotation;

                bool rotateLeft = input.IsKeyDown(Keys.Q);
                bool rotateRight = input.IsKeyDown(Keys.E);

                if (rotateLeft)
                {
                    rotation.Y += _drone.RotationSpeed * dt;
                }

                if (rotateRight)
                {
                    rotation.Y -= _drone.RotationSpeed * dt;
                }

                _drone.Rotation = rotation;

                Vector3 forward = _drone.GetForward();
                Vector3 right = new Vector3(forward.Z, 0f, -forward.X);
                right = Vector3.Normalize(right);

                Vector3 movement = Vector3.Zero;

                if (input.IsKeyDown(Keys.W))
                    movement += forward * _drone.MoveSpeed * dt;

                if (input.IsKeyDown(Keys.S))
                    movement -= forward * _drone.MoveSpeed * dt;

                if (input.IsKeyDown(Keys.A))
                    movement += right * _drone.MoveSpeed * dt;

                if (input.IsKeyDown(Keys.D))
                    movement -= right * _drone.MoveSpeed * dt;

                if (input.IsKeyDown(Keys.Space))
                    movement += Vector3.UnitY * _drone.VerticalSpeed * dt;

                if (input.IsKeyDown(Keys.LeftShift))
                    movement -= Vector3.UnitY * _drone.VerticalSpeed * dt;

                if (_drone.Position.Y < -0.2f)
                {
                    var position = _drone.Position;
                    position.Y = -0.2f;
                    _drone.Position = position;
                }

                float animationTarget = ComputeDroneAnimationTarget(
                    movement,
                    forward,
                    right,
                    rotateLeft,
                    rotateRight);

                UpdateDroneInnerAnimation(dt, animationTarget);

                MoveDroneWithCollision(movement);

                if (_cargoSphere.IsAttached)
                {
                    UpdateAttachedCargo(dt);
                }
                else
                {
                    if (_cargoReleaseCooldown <= 0f)
                    {
                        HandleDroneCargoInteraction();
                    }

                    UpdateCargoSphere(dt);
                }

                UpdateParticles(dt);
            }
            else
            {
                UpdateDroneInnerAnimation(dt, 0f);
            }

            switch (_cameraMode)
            {
                case CameraMode.Follow:
                    UpdateFollowCamera(dt);
                    break;

                case CameraMode.Orbit:
                    UpdateOrbitCamera();
                    break;

                case CameraMode.Free:
                    UpdateFreeCamera(dt, input);
                    break;
            }

            Title = $"Pos: X={_drone.Position.X:F2}, Y={_drone.Position.Y:F2}, Z={_drone.Position.Z:F2} | RotY={MathHelper.RadiansToDegrees(_drone.Rotation.Y):F1}";
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            Matrix4 view;

            switch (_cameraMode)
            {
                case CameraMode.Follow:
                    {
                        Vector3 forward = _drone.GetForward();
                        float lookAheadDistance = 2.0f;
                        Vector3 lookTarget = _drone.Position + forward * lookAheadDistance;
                        view = Matrix4.LookAt(_camera.Position, lookTarget, Vector3.UnitY);
                        break;
                    }

                case CameraMode.Orbit:
                    view = Matrix4.LookAt(_camera.Position, _drone.Position, Vector3.UnitY);
                    break;

                case CameraMode.Free:
                    view = _camera.GetViewMatrix();
                    break;

                default:
                    view = _camera.GetViewMatrix();
                    break;
            }

            var projection = Matrix4.CreatePerspectiveFieldOfView(
                MathHelper.DegreesToRadians(45f),
                Size.X / (float)Size.Y,
                0.1f,
                100.0f);

            foreach (var obj in _sceneObjects)
            {
                obj.Render(view, projection);
            }

            foreach (var obj in _texturedSceneObjects)
            {
                obj.Render(view, projection);
            }

            _drone.RenderComposite(view, projection);

            Matrix4 droneModel = _drone.GetModelMatrix();

            foreach (var part in _droneTexturedInnerParts)
            {
                part.Render(droneModel, view, projection);
            }

            RenderParticles(view, projection);

            RenderCargoIndicator();

            _revolutionMarker.Render(view, projection, _camera.Position);

            SwapBuffers();
        }


        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);
            GL.Viewport(0, 0, Size.X, Size.Y);
        }

        protected override void OnUnload()
        {
            _cubeMesh.Unload();
            _sphereMesh.Unload();
            _circleOverlayMesh.Unload();
            _crossOverlayMesh.Unload();
            _importedDroneBodyMesh.Unload();
            GL.DeleteProgram(_overlayShapeShader.Handle);
            GL.DeleteProgram(_shader.Handle);
            _texturedCubeMesh.Unload();
            _wallTexturedCubeMesh.Unload();
            _floorTexturedCubeMesh.Unload();
            _objectTexturedCubeMesh.Unload();
            GL.DeleteProgram(_texturedShader.Handle);
            _revolutionMesh.Unload();
            _billboardMesh.Unload();
            GL.DeleteProgram(_billboardShader.Handle);


            foreach (var part in _droneTexturedInnerParts)
            {
                part.Mesh.Unload();
            }

            base.OnUnload();
        }

        private void CreateScene()
        {
            _sceneObjects = new List<GameObject>();
            _texturedSceneObjects = new List<TexturedObject>();
            _obstacles = new List<GameObject>();
            _supportObjects = new List<GameObject>();

            var floor = new GameObject(_cubeMesh, _shader)
            {
                Position = new Vector3(0.0f, -1.0f, 0.0f),
                Scale = new Vector3(20.0f, 0.2f, 20.0f),
                Rotation = Vector3.Zero
            };

            var wallLeft = new GameObject(_cubeMesh, _shader)
            {
                Position = new Vector3(-10.0f, 1.0f, 0.0f),
                Scale = new Vector3(0.5f, 4.0f, 20.0f),
                Rotation = Vector3.Zero
            };

            var wallRight = new GameObject(_cubeMesh, _shader)
            {
                Position = new Vector3(10.0f, 1.0f, 0.0f),
                Scale = new Vector3(0.5f, 4.0f, 20.0f),
                Rotation = Vector3.Zero
            };

            var wallBack = new GameObject(_cubeMesh, _shader)
            {
                Position = new Vector3(0.0f, 1.0f, -10.0f),
                Scale = new Vector3(20.0f, 4.0f, 0.5f),
                Rotation = Vector3.Zero
            };

            var pillar1 = new GameObject(_cubeMesh, _shader)
            {
                Position = new Vector3(-4.0f, 1.0f, -3.0f),
                Scale = new Vector3(1.0f, 4.0f, 1.0f),
                Rotation = Vector3.Zero
            };

            var pillar2 = new GameObject(_cubeMesh, _shader)
            {
                Position = new Vector3(4.0f, 1.0f, 2.0f),
                Scale = new Vector3(1.0f, 4.0f, 1.0f),
                Rotation = Vector3.Zero
            };

            _block1 = new GameObject(_cubeMesh, _shader)
            {
                Position = new Vector3(0.0f, -0.3f, 4.0f),
                Scale = new Vector3(2.0f, 1.0f, 2.0f),
                Rotation = Vector3.Zero
            };

            var texturedFloor = new TexturedObject(_floorTexturedCubeMesh, _texturedShader, _floorTexture)
            {
                Position = floor.Position,
                Scale = floor.Scale,
                Rotation = floor.Rotation
            };

            var texturedWallLeft = new TexturedObject(_wallTexturedCubeMesh, _texturedShader, _wallTexture)
            {
                Position = wallLeft.Position,
                Scale = wallLeft.Scale,
                Rotation = wallLeft.Rotation
            };

            var texturedWallRight = new TexturedObject(_wallTexturedCubeMesh, _texturedShader, _wallTexture)
            {
                Position = wallRight.Position,
                Scale = wallRight.Scale,
                Rotation = wallRight.Rotation
            };

            var texturedWallBack = new TexturedObject(_wallTexturedCubeMesh, _texturedShader, _wallTexture)
            {
                Position = wallBack.Position,
                Scale = wallBack.Scale,
                Rotation = wallBack.Rotation
            };

            var texturedPillar1 = new TexturedObject(_objectTexturedCubeMesh, _texturedShader, _pillarTexture)
            {
                Position = pillar1.Position,
                Scale = pillar1.Scale,
                Rotation = pillar1.Rotation
            };

            var texturedPillar2 = new TexturedObject(_objectTexturedCubeMesh, _texturedShader, _pillarTexture)
            {
                Position = pillar2.Position,
                Scale = pillar2.Scale,
                Rotation = pillar2.Rotation
            };

            var texturedBlock1 = new TexturedObject(_texturedCubeMesh, _texturedShader, _blockTexture)
            {
                Position = _block1.Position,
                Scale = _block1.Scale,
                Rotation = _block1.Rotation
            };

            _texturedSceneObjects.Add(texturedFloor);
            _texturedSceneObjects.Add(texturedWallLeft);
            _texturedSceneObjects.Add(texturedWallRight);
            _texturedSceneObjects.Add(texturedWallBack);
            _texturedSceneObjects.Add(texturedPillar1);
            _texturedSceneObjects.Add(texturedPillar2);
            _texturedSceneObjects.Add(texturedBlock1);

            _obstacles.Add(wallLeft);
            _obstacles.Add(wallRight);
            _obstacles.Add(wallBack);
            _obstacles.Add(pillar1);
            _obstacles.Add(pillar2);
            _obstacles.Add(_block1);

            _supportObjects.Add(floor);
            _supportObjects.Add(_block1);
            _supportObjects.Add(pillar1);
            _supportObjects.Add(pillar2);
        }

        private bool IsCollidingWithObstacles(Aabb droneAabb)
        {
            foreach (var obstacle in _obstacles)
            {
                if (droneAabb.Intersects(obstacle.GetAabb()))
                {
                    return true;
                }
            }

            return false;
        }


        private void MoveDroneWithCollision(Vector3 movement)
        {
            Vector3 position = _drone.Position;

            if (movement.X != 0f)
            {
                Vector3 newPositionX = position + new Vector3(movement.X, 0f, 0f);
                if (!IsCollidingWithObstacles(_drone.GetAabbAt(newPositionX)))
                {
                    position = newPositionX;
                }
            }

            if (movement.Y != 0f)
            {
                Vector3 newPositionY = position + new Vector3(0f, movement.Y, 0f);
                if (!IsCollidingWithObstacles(_drone.GetAabbAt(newPositionY)))
                {
                    position = newPositionY;
                }
            }

            if (movement.Z != 0f)
            {
                Vector3 newPositionZ = position + new Vector3(0f, 0f, movement.Z);
                if (!IsCollidingWithObstacles(_drone.GetAabbAt(newPositionZ)))
                {
                    position = newPositionZ;
                }
            }

            _drone.Position = position;
        }

        private void UpdateCargoSphere(float dt)
        {
            _cargoSphere.IsGrounded = false;

            Vector3 position = _cargoSphere.Position;
            Vector3 velocity = _cargoSphere.Velocity;

            velocity.Y -= _cargoSphere.Gravity * dt;

            // Движение по X
            if (velocity.X != 0f)
            {
                Vector3 newPositionX = position + new Vector3(velocity.X * dt, 0f, 0f);

                if (!CargoSphereCollidesWithObstacles(newPositionX))
                {
                    position = newPositionX;
                }
                else
                {
                    velocity.X = 0f;
                }
            }

            // Движение по Z
            if (velocity.Z != 0f)
            {
                Vector3 newPositionZ = position + new Vector3(0f, 0f, velocity.Z * dt);

                if (!CargoSphereCollidesWithObstacles(newPositionZ))
                {
                    position = newPositionZ;
                }
                else
                {
                    velocity.Z = 0f;
                }
            }

            // Движение по Y
            Vector3 newPositionY = position + new Vector3(0f, velocity.Y * dt, 0f);

            float bestSupportY = float.NegativeInfinity;
            bool foundSupport = false;

            foreach (var support in _supportObjects)
            {
                float supportMinX = support.Position.X - support.Scale.X / 2.0f;
                float supportMaxX = support.Position.X + support.Scale.X / 2.0f;
                float supportMinZ = support.Position.Z - support.Scale.Z / 2.0f;
                float supportMaxZ = support.Position.Z + support.Scale.Z / 2.0f;

                bool insideX = position.X >= supportMinX && position.X <= supportMaxX;
                bool insideZ = position.Z >= supportMinZ && position.Z <= supportMaxZ;

                if (!insideX || !insideZ)
                    continue;

                float supportTopY = support.Position.Y + support.Scale.Y / 2.0f;
                float sphereBottomY = newPositionY.Y - _cargoSphere.Radius;
                float currentBottomY = position.Y - _cargoSphere.Radius;

                bool crossingDownOntoSupport =
                    currentBottomY >= supportTopY &&
                    sphereBottomY <= supportTopY;

                if (crossingDownOntoSupport && supportTopY > bestSupportY)
                {
                    bestSupportY = supportTopY;
                    foundSupport = true;
                }
            }

            if (foundSupport)
            {
                float impactSpeed = MathF.Abs(velocity.Y);

                position.Y = bestSupportY + _cargoSphere.Radius;
                velocity.Y = 0f;
                _cargoSphere.IsGrounded = true;

                if (impactSpeed > 3.5f)
                {
                    Vector3 impactPosition = new Vector3(
                        position.X,
                        bestSupportY,
                        position.Z);

                    SpawnImpactParticles(impactPosition, impactSpeed);
                }
            }
            else
            {
                position = newPositionY;
            }

            if (_cargoSphere.IsGrounded)
            {
                velocity.X *= _cargoSphere.GroundDamping;
                velocity.Z *= _cargoSphere.GroundDamping;

                if (MathF.Abs(velocity.X) < 0.01f) velocity.X = 0f;
                if (MathF.Abs(velocity.Z) < 0.01f) velocity.Z = 0f;
            }
            else
            {
                velocity.X *= _cargoSphere.AirDamping;
                velocity.Z *= _cargoSphere.AirDamping;
            }

            _cargoSphere.Position = position;
            _cargoSphere.Velocity = velocity;
        }

        private void HandleDroneCargoInteraction()
        {
            Vector3 offset = _cargoSphere.Position - _drone.Position;

            float distance = offset.Length;
            float contactDistance = 0.9f;

            if (distance > 0.001f && distance < contactDistance)
            {
                Vector3 pushDirection = Vector3.Normalize(offset);

                Vector3 droneHorizontalVelocity = _drone.GetForward() * _drone.MoveSpeed;
                Vector3 impulse = new Vector3(pushDirection.X, 0f, pushDirection.Z) * 2.0f;

                if (droneHorizontalVelocity.LengthSquared > 0.001f)
                {
                    impulse += new Vector3(droneHorizontalVelocity.X, 0f, droneHorizontalVelocity.Z) * 0.35f;
                }

                _cargoSphere.AddImpulse(impulse);
            }
        }

        private static float Clamp(float value, float min, float max)
        {
            return MathF.Max(min, MathF.Min(max, value));
        }

        private bool SphereIntersectsAabb(Vector3 sphereCenter, float sphereRadius, Aabb aabb)
        {
            float closestX = Clamp(sphereCenter.X, aabb.Min.X, aabb.Max.X);
            float closestY = Clamp(sphereCenter.Y, aabb.Min.Y, aabb.Max.Y);
            float closestZ = Clamp(sphereCenter.Z, aabb.Min.Z, aabb.Max.Z);

            Vector3 closestPoint = new Vector3(closestX, closestY, closestZ);
            Vector3 difference = sphereCenter - closestPoint;

            return difference.LengthSquared < sphereRadius * sphereRadius;
        }

        private bool CargoSphereCollidesWithObstacles(Vector3 sphereCenter)
        {
            foreach (var obstacle in _obstacles)
            {
                if (SphereIntersectsAabb(sphereCenter, _cargoSphere.Radius, obstacle.GetAabb()))
                {
                    return true;
                }
            }

            return false;
        }

        private bool CanAttachCargo()
        {
            Vector3 grabPoint = GetDroneGrabPoint();
            Vector3 cargoPos = _cargoSphere.Position;

            Vector2 grabXZ = new Vector2(grabPoint.X, grabPoint.Z);
            Vector2 cargoXZ = new Vector2(cargoPos.X, cargoPos.Z);

            float horizontalDistance = (grabXZ - cargoXZ).Length;
            float verticalOffset = grabPoint.Y - cargoPos.Y;

            float maxHorizontalDistance = 0.85f;
            float minVerticalOffset = -0.15f;
            float maxVerticalOffset = 0.60f;

            bool closeEnoughHorizontally = horizontalDistance <= maxHorizontalDistance;
            bool goodVerticalAlignment = verticalOffset >= minVerticalOffset && verticalOffset <= maxVerticalOffset;
            bool droneAboveCargo = _drone.Position.Y > cargoPos.Y;

            return closeEnoughHorizontally && goodVerticalAlignment && droneAboveCargo;
        }

        private void ToggleCargoAttachment()
        {
            if (_cargoSphere.IsAttached)
            {
                _cargoSphere.IsAttached = false;
                _cargoSphere.IsGrounded = false;
                _cargoSphere.Velocity = Vector3.Zero;
                _cargoReleaseCooldown = 0.15f; // костыль против скорости груза при отпускании
                return;
            }

            if (_cargoReleaseCooldown <= 0f && CanAttachCargo())
            {
                _cargoSphere.IsAttached = true;
                _cargoSphere.Velocity = Vector3.Zero;
                _cargoSphere.IsGrounded = false;
            }
        }

        private Vector3 GetDroneGrabPoint()
        {
            return _drone.Position + new Vector3(0f, -0.35f, 0f);
        }
        private GameObject? GetCollidingObstacleForCargo(Vector3 sphereCenter)
        {
            foreach (var obstacle in _obstacles)
            {
                if (SphereIntersectsAabb(sphereCenter, _cargoSphere.Radius, obstacle.GetAabb()))
                {
                    return obstacle;
                }
            }

            return null;
        }

        private void UpdateAttachedCargo(float dt)
        {
            Vector3 grabPoint = GetDroneGrabPoint();
            Vector3 desiredPosition = grabPoint - new Vector3(0f, _cargoSphere.Radius, 0f);

            GameObject? obstacle = GetCollidingObstacleForCargo(desiredPosition);

            if (obstacle == null)
            {
                _cargoSphere.Position = desiredPosition;
                _cargoSphere.Velocity = Vector3.Zero;
                _cargoSphere.IsGrounded = false;
                return;
            }

            var aabb = obstacle.GetAabb();

            bool horizontallyInside =
                desiredPosition.X >= aabb.Min.X && desiredPosition.X <= aabb.Max.X &&
                desiredPosition.Z >= aabb.Min.Z && desiredPosition.Z <= aabb.Max.Z;

            float obstacleTop = aabb.Max.Y;
            float sphereBottom = desiredPosition.Y - _cargoSphere.Radius;
            float currentBottom = _cargoSphere.Position.Y - _cargoSphere.Radius;

            bool collisionFromBelow =
                horizontallyInside &&
                currentBottom >= obstacleTop &&
                sphereBottom <= obstacleTop;

            if (collisionFromBelow)
            {
                _cargoSphere.Position = new Vector3(
                    desiredPosition.X,
                    obstacleTop + _cargoSphere.Radius,
                    desiredPosition.Z);

                _cargoSphere.Velocity = new Vector3(0f, 1.0f, 0f);
                _cargoSphere.IsGrounded = false;
                return;
            }

            ForceDetachCargoFromObstacle(obstacle, dt);
        }

        private void ForceDetachCargoFromObstacle(GameObject obstacle, float dt)
        {
            _cargoSphere.IsAttached = false;
            _cargoSphere.IsGrounded = false;
            _cargoReleaseCooldown = 0.15f;
            _cargoLostIndicatorTimer = 3.0f;

            Vector3 obstacleCenter = obstacle.Position;
            Vector3 pushDirection = _cargoSphere.Position - obstacleCenter;

            pushDirection.Y = 0f;

            if (pushDirection.LengthSquared < 0.0001f)
            {
                pushDirection = _drone.GetForward();
            }
            else
            {
                pushDirection = Vector3.Normalize(pushDirection);
            }

            Vector3 droneForward = _drone.GetForward();
            float droneSpeedFactor = _drone.MoveSpeed * 0.6f;

            _cargoSphere.Velocity =
                pushDirection * droneSpeedFactor +
                droneForward * (droneSpeedFactor * 0.35f) +
                new Vector3(0f, 1.0f, 0f);
        }

        private CargoIndicatorState GetCargoIndicatorState()
        {
            if (_cargoLostIndicatorTimer > 0f)
                return CargoIndicatorState.Lost;

            if (_cargoSphere.IsAttached)
                return CargoIndicatorState.Attached;

            if (CanAttachCargo())
                return CargoIndicatorState.CanAttach;

            return CargoIndicatorState.None;
        }

        private void RenderCargoIndicator()
        {
            CargoIndicatorState state = GetCargoIndicatorState();

            if (state == CargoIndicatorState.None)
                return;

            var projection = Matrix4.CreateOrthographicOffCenter(
                0f, Size.X,
                Size.Y, 0f,
                -1f, 1f);

            Matrix4 model = Matrix4.CreateTranslation(683f, 120f, 0f);

            _overlayShapeShader.Use();
            _overlayShapeShader.SetMatrix4("projection", projection);
            _overlayShapeShader.SetMatrix4("model", model);

            GL.Disable(EnableCap.DepthTest);

            switch (state)
            {
                case CargoIndicatorState.CanAttach:
                    _overlayShapeShader.SetVector3("overlayColor", new OpenTK.Mathematics.Vector3(0f, 1f, 0f));
                    _circleOverlayMesh.Render();
                    break;

                case CargoIndicatorState.Attached:
                    _overlayShapeShader.SetVector3("overlayColor", new OpenTK.Mathematics.Vector3(1f, 0f, 0f));
                    _circleOverlayMesh.Render();
                    break;

                case CargoIndicatorState.Lost:
                    _overlayShapeShader.SetVector3("overlayColor", new OpenTK.Mathematics.Vector3(1f, 0f, 0f));
                    _crossOverlayMesh.Render();
                    break;
            }

            GL.Enable(EnableCap.DepthTest);
        }

        private void BuildDroneParts()
        {
            _drone.Parts.Clear();

            float frameThickness = 0.12f;
            float frameHeight = 0.24f;
            float frameOuterSize = 1.4f;
            float half = frameOuterSize / 2.0f;

            var frameFront = new DronePart(_cubeMesh, _shader)
            {
                LocalPosition = new Vector3(0f, 0f, half),
                LocalRotation = Vector3.Zero,
                LocalScale = new Vector3(frameOuterSize, frameHeight, frameThickness)
            };

            var frameBack = new DronePart(_cubeMesh, _shader)
            {
                LocalPosition = new Vector3(0f, 0f, -half),
                LocalRotation = Vector3.Zero,
                LocalScale = new Vector3(frameOuterSize, frameHeight, frameThickness)
            };

            var frameLeft = new DronePart(_cubeMesh, _shader)
            {
                LocalPosition = new Vector3(-half, 0f, 0f),
                LocalRotation = Vector3.Zero,
                LocalScale = new Vector3(frameThickness, frameHeight, frameOuterSize)
            };

            var frameRight = new DronePart(_cubeMesh, _shader)
            {
                LocalPosition = new Vector3(half, 0f, 0f),
                LocalRotation = Vector3.Zero,
                LocalScale = new Vector3(frameThickness, frameHeight, frameOuterSize)
            };

            _drone.Parts.Add(frameFront);
            _drone.Parts.Add(frameBack);
            _drone.Parts.Add(frameLeft);
            _drone.Parts.Add(frameRight);
        }

        private float ComputeDroneAnimationTarget(
            Vector3 movement,
            Vector3 forward,
            Vector3 right,
            bool rotateLeft,
            bool rotateRight)
        {
            float forwardAmount = Vector3.Dot(movement, forward);
            float rightAmount = Vector3.Dot(movement, right);
            float upAmount = movement.Y;

            float signedInput = 0.125f;

            // Вперёд / назад
            signedInput += forwardAmount * 20.0f;

            // Влево / вправо
            signedInput -= rightAmount * 20.0f;

            // Вверх / вниз
            signedInput += upAmount * 15.0f;

            // Повороты Q / E
            if (rotateLeft)
                signedInput += 1.0f;

            if (rotateRight)
                signedInput -= 1.0f;

            return signedInput;
        }

        private void UpdateDroneInnerAnimation(float dt, float targetInput)
        {
            float maxAngularSpeed = 8.0f;
            float acceleration = 102.0f;
            float damping = 5.0f;

            float clampedInput = Clamp(targetInput, -1f, 1f);
            float targetAngularVelocity = clampedInput * maxAngularSpeed;

            if (MathF.Abs(clampedInput) > 0.001f)
            {
                _droneInnerAngularVelocity = MoveTowards(
                    _droneInnerAngularVelocity,
                    targetAngularVelocity,
                    acceleration * dt);
            }
            else
            {
                _droneInnerAngularVelocity = MoveTowards(
                    _droneInnerAngularVelocity,
                    0f,
                    damping * dt);
            }

            _droneInnerRotationY += _droneInnerAngularVelocity * dt;

            foreach (var part in _droneTexturedInnerParts)
            {
                var rotation = part.LocalRotation;
                rotation.Y = _droneInnerRotationY;
                part.LocalRotation = rotation;
            }
        }
        private float MoveTowards(float current, float target, float maxDelta)
        {
            if (MathF.Abs(target - current) <= maxDelta)
                return target;

            return current + MathF.Sign(target - current) * maxDelta;
        }

        private void SpawnImpactParticles(Vector3 impactPosition, float impactStrength)
        {
            int particleCount = 12;

            for (int i = 0; i < particleCount; i++)
            {
                float angle = (float)(_random.NextDouble() * Math.PI * 2.0);
                float horizontalSpeed = 0.8f + (float)_random.NextDouble() * 1.8f;
                float verticalSpeed = 1.2f + (float)_random.NextDouble() * 1.6f;

                Vector3 velocity = new Vector3(
                    MathF.Cos(angle) * horizontalSpeed,
                    verticalSpeed,
                    MathF.Sin(angle) * horizontalSpeed);

                var particle = new Particle(_cubeMesh, _shader)
                {
                    Position = impactPosition,
                    Velocity = velocity,
                    Scale = new Vector3(0.08f, 0.08f, 0.08f),
                    Rotation = Vector3.Zero,
                    Life = 0.4f + (float)_random.NextDouble() * 0.4f
                };

                _particles.Add(particle);
            }
        }

        private void UpdateParticles(float dt)
        {
            for (int i = _particles.Count - 1; i >= 0; i--)
            {
                _particles[i].Update(dt);

                if (!_particles[i].IsAlive)
                {
                    _particles.RemoveAt(i);
                }
            }
        }

        private void RenderParticles(Matrix4 view, Matrix4 projection)
        {
            foreach (var particle in _particles)
            {
                particle.Render(view, projection);
            }
        }

        private void SetCursorCaptured(bool captured)
        {
            _cursorCaptured = captured;
            CursorState = captured ? CursorState.Grabbed : CursorState.Normal;
            _firstMouseMove = true;
        }

        private Vector2 GetMouseDelta()
        {
            var mouse = MouseState;

            if (_firstMouseMove)
            {
                _lastMousePosition = mouse.Position;
                _firstMouseMove = false;
                return Vector2.Zero;
            }

            Vector2 delta = mouse.Position - _lastMousePosition;
            _lastMousePosition = mouse.Position;
            return delta;
        }

        private void SyncOrbitCameraFromCurrentPosition()
        {
            Vector3 offset = _drone.Position - _camera.Position;
            _orbitDistance = offset.Length;

            if (_orbitDistance > 0.001f)
            {
                offset = Vector3.Normalize(offset);
                _orbitPitch = MathHelper.RadiansToDegrees(MathF.Asin(offset.Y));
                _orbitYaw = MathHelper.RadiansToDegrees(MathF.Atan2(offset.X, offset.Z));
            }
        }

        private void UpdateFollowCamera(float dt)
        {
            Vector3 cameraForward = _drone.GetForward();

            float followDistance = 5.0f;
            float followHeight = 2.0f;

            Vector3 targetCameraPosition =
                _drone.Position
                - cameraForward * followDistance
                + Vector3.UnitY * followHeight;

            float followSpeed = 4.0f;
            float t = MathF.Min(followSpeed * dt, 1.0f);

            _camera.Position = Vector3.Lerp(_camera.Position, targetCameraPosition, t);
        }

        private void UpdateOrbitCamera()
        {
            Vector2 mouseDelta = GetMouseDelta();

            _orbitYaw += mouseDelta.X * _mouseSensitivity;
            _orbitPitch -= mouseDelta.Y * _mouseSensitivity;
            _orbitPitch = MathHelper.Clamp(_orbitPitch, -80f, 80f);

            float yawRad = MathHelper.DegreesToRadians(_orbitYaw);
            float pitchRad = MathHelper.DegreesToRadians(_orbitPitch);

            Vector3 offset;
            offset.X = MathF.Cos(pitchRad) * MathF.Sin(yawRad);
            offset.Y = MathF.Sin(pitchRad);
            offset.Z = MathF.Cos(pitchRad) * MathF.Cos(yawRad);

            offset = Vector3.Normalize(offset) * _orbitDistance;

            _camera.Position = _drone.Position - offset;
        }

        private void UpdateFreeCamera(float dt, KeyboardState input)
        {
            Vector2 mouseDelta = GetMouseDelta();
            _camera.AddRotation(mouseDelta.X, mouseDelta.Y);

            if (input.IsKeyDown(Keys.W))
                _camera.MoveForward(dt);

            if (input.IsKeyDown(Keys.S))
                _camera.MoveBackward(dt);

            if (input.IsKeyDown(Keys.A))
                _camera.MoveLeft(dt);

            if (input.IsKeyDown(Keys.D))
                _camera.MoveRight(dt);

            if (input.IsKeyDown(Keys.Space))
                _camera.Position += Vector3.UnitY * _camera.Speed * dt;

            if (input.IsKeyDown(Keys.LeftShift))
                _camera.Position -= Vector3.UnitY * _camera.Speed * dt;
        }
    }
}