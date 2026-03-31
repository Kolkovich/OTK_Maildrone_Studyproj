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
        private Mesh _sphereMesh;
        private CargoSphere _cargoSphere;
        private GameObject _block1; // Отдельно чтобы не искать среди sceneObjects
        // временные показатели
        private float _cargoReleaseCooldown = 0f;
        private float _cargoLostIndicatorTimer = 0f;
        private Mesh _importedDroneBodyMesh;

        private Vector2 _lastMousePosition;
        private bool _firstMove = true;

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

        protected override void OnLoad()
        {
            base.OnLoad();

            GL.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);
            GL.Enable(EnableCap.DepthTest);

            _shader = new Shader("Shaders/shader.vert", "Shaders/shader.frag");

            var (cubeVertices, cubeIndices) = MeshFactory.CreateCubeVertices();
            _cubeMesh = new Mesh(cubeVertices, cubeIndices);
            _cubeMesh.Load();

            CreateScene();

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

            if (input.IsKeyPressed(Keys.F))
            {
                ToggleCargoAttachment();
            }

            var rotation = _drone.Rotation;

            if (input.IsKeyDown(Keys.Q))
            {
                rotation.Y += _drone.RotationSpeed * dt;
            }

            if (input.IsKeyDown(Keys.E))
            {
                rotation.Y -= _drone.RotationSpeed * dt;
            }

            _drone.Rotation = rotation;

            Vector3 forward = _drone.GetForward();
            Vector3 right = new Vector3(forward.Z, 0f, -forward.X);
            right = Vector3.Normalize(right);

            Vector3 movement = Vector3.Zero;

            if (input.IsKeyDown(Keys.W))
            {
                movement += forward * _drone.MoveSpeed * dt;
            }

            if (input.IsKeyDown(Keys.S))
            {
                movement -= forward * _drone.MoveSpeed * dt;
            }

            if (input.IsKeyDown(Keys.A))
            {
                movement += right * _drone.MoveSpeed * dt;
            }

            if (input.IsKeyDown(Keys.D))
            {
                movement -= right * _drone.MoveSpeed * dt;
            }

            if (input.IsKeyDown(Keys.Space))
            {
                movement += Vector3.UnitY * _drone.VerticalSpeed * dt;
            }

            if (input.IsKeyDown(Keys.LeftShift))
            {
                movement -= Vector3.UnitY * _drone.VerticalSpeed * dt;
            }

            if (_drone.Position.Y < -0.2f)
            {
                var position = _drone.Position;
                position.Y = -0.2f;
                _drone.Position = position;
            }

            // Логика с грузом: если везётся, то не катится
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

            Title = $"Pos: X={_drone.Position.X:F2}, Y={_drone.Position.Y:F2}, Z={_drone.Position.Z:F2} | RotY={MathHelper.RadiansToDegrees(_drone.Rotation.Y):F1}";
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            Vector3 forward = _drone.GetForward();
            float lookAheadDistance = 2.0f;
            Vector3 lookTarget = _drone.Position + forward * lookAheadDistance;

            var view = Matrix4.LookAt(_camera.Position, lookTarget, Vector3.UnitY);

            var projection = Matrix4.CreatePerspectiveFieldOfView(
                MathHelper.DegreesToRadians(45f),
                Size.X / (float)Size.Y,
                0.1f,
                100.0f);

            foreach (var obj in _sceneObjects)
            {
                obj.Render(view, projection);
            }

            _drone.RenderComposite(view, projection);

            RenderCargoIndicator();

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

            base.OnUnload();
        }

        private void CreateScene()
        {
            _sceneObjects = new List<GameObject>();
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

            _sceneObjects.Add(floor);
            _sceneObjects.Add(wallLeft);
            _sceneObjects.Add(wallRight);
            _sceneObjects.Add(wallBack);
            _sceneObjects.Add(pillar1);
            _sceneObjects.Add(pillar2);
            _sceneObjects.Add(_block1);

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
                position.Y = bestSupportY + _cargoSphere.Radius;
                velocity.Y = 0f;
                _cargoSphere.IsGrounded = true;
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

        private void UpdateAttachedCargo()
        {
            Vector3 grabPoint = GetDroneGrabPoint();

            _cargoSphere.Position = grabPoint - new Vector3(0f, _cargoSphere.Radius, 0f);
            _cargoSphere.Velocity = Vector3.Zero;
            _cargoSphere.IsGrounded = false;
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

            Matrix4 model = Matrix4.CreateTranslation(640f, 120f, 0f);

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

            var innerBody = new DronePart(_importedDroneBodyMesh, _shader)
            {
                LocalPosition = Vector3.Zero,
                LocalScale = new Vector3(1.2f, 2.0f, 1.2f)
            };

            float frameThickness = 0.12f;
            float frameHeight = 0.36f;
            float frameOuterSize = 1.4f;
            float half = frameOuterSize / 2.0f;

            var frameFront = new DronePart(_cubeMesh, _shader)
            {
                LocalPosition = new Vector3(0f, 0f, half),
                LocalScale = new Vector3(frameOuterSize, frameHeight, frameThickness)
            };

            var frameBack = new DronePart(_cubeMesh, _shader)
            {
                LocalPosition = new Vector3(0f, 0f, -half),
                LocalScale = new Vector3(frameOuterSize, frameHeight, frameThickness)
            };

            var frameLeft = new DronePart(_cubeMesh, _shader)
            {
                LocalPosition = new Vector3(-half, 0f, 0f),
                LocalScale = new Vector3(frameThickness, frameHeight, frameOuterSize)
            };

            var frameRight = new DronePart(_cubeMesh, _shader)
            {
                LocalPosition = new Vector3(half, 0f, 0f),
                LocalScale = new Vector3(frameThickness, frameHeight, frameOuterSize)
            };

            _drone.Parts.Add(innerBody);
            _drone.Parts.Add(frameFront);
            _drone.Parts.Add(frameBack);
            _drone.Parts.Add(frameLeft);
            _drone.Parts.Add(frameRight);
        }
    }
}