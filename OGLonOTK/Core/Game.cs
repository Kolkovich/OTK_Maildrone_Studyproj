using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Windowing.Desktop;
using OGLonOTK.Graphics;

namespace OGLonOTK.Core
{
    public class Game : GameWindow
    {
        private Shader _shader;
        private Mesh _rectangleMesh;

        public Game(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
            : base(gameWindowSettings, nativeWindowSettings)
        {
        }

        protected override void OnLoad()
        {
            base.OnLoad();

            GL.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);

            float[] vertices =
            {
                // positions
                -0.5f, -0.5f, 0.0f, // bottom-left
                 0.5f, -0.5f, 0.0f, // bottom-right
                 0.5f,  0.5f, 0.0f, // top-right
                -0.5f,  0.5f, 0.0f  // top-left
            };

            uint[] indices =
            {
                0, 1, 2, // first triangle
                2, 3, 0  // second triangle
            };

            _shader = new Shader("Shaders/shader.vert", "Shaders/shader.frag");

            _rectangleMesh = new Mesh(vertices, indices);
            _rectangleMesh.Load();
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);

            GL.Clear(ClearBufferMask.ColorBufferBit);

            _shader.Use();
            _rectangleMesh.Render();

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
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);
            GL.Viewport(0, 0, Size.X, Size.Y);
        }

        protected override void OnUnload()
        {
            _rectangleMesh.Unload();
            GL.DeleteProgram(_shader.Handle);

            base.OnUnload();
        }
    }
}