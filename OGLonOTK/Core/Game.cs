using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OGLonOTK.Graphics;
using OGLonOTK.World;

namespace OGLonOTK.Core
{
    public class Game : GameWindow
    {
        private Shader _shader;
        private Mesh _cubeMesh;
        private GameObject _cubeObject;
        private Camera _camera;

        public Game(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
            : base(gameWindowSettings, nativeWindowSettings)
        {
        }

        protected override void OnLoad()
        {
            base.OnLoad();

            _camera = new Camera(new Vector3(0.0f, 0.0f, 3.0f));

            GL.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);
            GL.Enable(EnableCap.DepthTest);

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

            _cubeObject = new GameObject(_cubeMesh, _shader)
            {
                Position = Vector3.Zero,
                Rotation = new Vector3(
                    MathHelper.DegreesToRadians(20f),
                    MathHelper.DegreesToRadians(30f),
                    0f),
                Scale = Vector3.One
            };
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            var view = _camera.GetViewMatrix();
            var projection = Matrix4.CreatePerspectiveFieldOfView(
                MathHelper.DegreesToRadians(45f),
                Size.X / (float)Size.Y,
                0.1f,
                100.0f);

            _cubeObject.Render(view, projection);

            SwapBuffers();
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);

            var input = KeyboardState;

            if (input.IsKeyDown(Keys.Escape))
            {
                Close();
            }

            if (input.IsKeyDown(Keys.W))
            {
                _camera.MoveForward((float)e.Time);
            }

            if (input.IsKeyDown(Keys.S))
            {
                _camera.MoveBackward((float)e.Time);
            }

            if (input.IsKeyDown(Keys.A))
            {
                _camera.MoveLeft((float)e.Time);
            }

            if (input.IsKeyDown(Keys.D))
            {
                _camera.MoveRight((float)e.Time);
            }
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
    }
}