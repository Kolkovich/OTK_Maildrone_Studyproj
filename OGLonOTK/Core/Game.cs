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
        private OverlayMesh _overlayMesh;
        private List<GameObject> _sceneObjects = [];
        private List<GameObject> _obstacles = [];

        private Vector2 _lastMousePosition;
        private bool _firstMove = true;

        public Game(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
            : base(gameWindowSettings, nativeWindowSettings)
        {
        }

        protected override void OnLoad()
        {
            base.OnLoad();

            GL.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);
            GL.Enable(EnableCap.DepthTest);

            CursorState = CursorState.Grabbed;

            float[] vertices =
            {
                // Front face (red)
                -0.5f, -0.5f,  0.5f,   1.0f, 0.0f, 0.0f,
                 0.5f, -0.5f,  0.5f,   1.0f, 0.0f, 0.0f,
                 0.5f,  0.5f,  0.5f,   1.0f, 0.0f, 0.0f,
                -0.5f,  0.5f,  0.5f,   1.0f, 0.0f, 0.0f,

                // Back face (green)
                -0.5f, -0.5f, -0.5f,   0.0f, 1.0f, 0.0f,
                 0.5f, -0.5f, -0.5f,   0.0f, 1.0f, 0.0f,
                 0.5f,  0.5f, -0.5f,   0.0f, 1.0f, 0.0f,
                -0.5f,  0.5f, -0.5f,   0.0f, 1.0f, 0.0f,

                // Left face (blue)
                -0.5f, -0.5f, -0.5f,   0.0f, 0.0f, 1.0f,
                -0.5f, -0.5f,  0.5f,   0.0f, 0.0f, 1.0f,
                -0.5f,  0.5f,  0.5f,   0.0f, 0.0f, 1.0f,
                -0.5f,  0.5f, -0.5f,   0.0f, 0.0f, 1.0f,

                // Right face (yellow)
                 0.5f, -0.5f, -0.5f,   1.0f, 1.0f, 0.0f,
                 0.5f, -0.5f,  0.5f,   1.0f, 1.0f, 0.0f,
                 0.5f,  0.5f,  0.5f,   1.0f, 1.0f, 0.0f,
                 0.5f,  0.5f, -0.5f,   1.0f, 1.0f, 0.0f,

                // Top face (magenta)
                -0.5f,  0.5f, -0.5f,   1.0f, 0.0f, 1.0f,
                -0.5f,  0.5f,  0.5f,   1.0f, 0.0f, 1.0f,
                 0.5f,  0.5f,  0.5f,   1.0f, 0.0f, 1.0f,
                 0.5f,  0.5f, -0.5f,   1.0f, 0.0f, 1.0f,

                // Bottom face (cyan)
                -0.5f, -0.5f, -0.5f,   0.0f, 1.0f, 1.0f,
                -0.5f, -0.5f,  0.5f,   0.0f, 1.0f, 1.0f,
                 0.5f, -0.5f,  0.5f,   0.0f, 1.0f, 1.0f,
                 0.5f, -0.5f, -0.5f,   0.0f, 1.0f, 1.0f
            };

            uint[] indices =
            {
                0, 1, 2,  2, 3, 0,
                4, 5, 6,  6, 7, 4,
                8, 9, 10, 10, 11, 8,
                12, 13, 14, 14, 15, 12,
                16, 17, 18, 18, 19, 16,
                20, 21, 22, 22, 23, 20
            };

            _shader = new Shader("Shaders/shader.vert", "Shaders/shader.frag");
            _cubeMesh = new Mesh(vertices, indices);
            _cubeMesh.Load();

            _drone = new Drone(_cubeMesh, _shader)
            {
                Position = Vector3.Zero,
                Rotation = Vector3.Zero,
                Scale = new Vector3(1.0f, 0.3f, 1.0f)
            };

            CreateScene();

            // Оверлей
            _overlayShader = new Shader("Shaders/overlay.vert", "Shaders/overlay.frag");

            float[] overlayVertices =
            {
                10f,  10f,   0.2f, 0.8f, 0.2f,
                160f, 10f,   0.2f, 0.8f, 0.2f,
                160f, 40f,   0.2f, 0.8f, 0.2f,
                10f,  40f,   0.2f, 0.8f, 0.2f
            };

            uint[] overlayIndices =
            {
                0, 1, 2,
                2, 3, 0
            };

            _overlayMesh = new OverlayMesh(overlayVertices, overlayIndices);
            _overlayMesh.Load();

            _camera = new Camera(new Vector3(0.0f, 2.0f, 5.0f));
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);

            float dt = (float)e.Time;
            var input = KeyboardState;

            if (input.IsKeyDown(Keys.Escape))
            {
                Close();
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

            MoveDroneWithCollision(movement);

            if (_drone.Position.Y < -0.2f)
            {
                var position = _drone.Position;
                position.Y = -0.2f;
                _drone.Position = position;
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

            _drone.Render(view, projection);

            var overlayProjection = Matrix4.CreateOrthographicOffCenter(
                0f, Size.X,
                Size.Y, 0f,
                -1f, 1f);

            _overlayShader.Use();
            _overlayShader.SetMatrix4("projection", overlayProjection);

            // Чтобы overlay не конфликтовал с depth
            GL.Disable(EnableCap.DepthTest);
            _overlayMesh.Render();
            GL.Enable(EnableCap.DepthTest);

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
            GL.DeleteProgram(_shader.Handle);

            base.OnUnload();
        }

        private void CreateScene()
        {
            _sceneObjects = new List<GameObject>();
            _obstacles = new List<GameObject>();

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

            var block1 = new GameObject(_cubeMesh, _shader)
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
            _sceneObjects.Add(block1);

            _obstacles.Add(wallLeft);
            _obstacles.Add(wallRight);
            _obstacles.Add(wallBack);
            _obstacles.Add(pillar1);
            _obstacles.Add(pillar2);
            _obstacles.Add(block1);
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
    }
}