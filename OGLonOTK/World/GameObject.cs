using OpenTK.Mathematics;
using OGLonOTK.Graphics;

namespace OGLonOTK.World
{
    public class GameObject
    {
        public Vector3 Position { get; set; } = Vector3.Zero;
        public Vector3 Rotation { get; set; } = Vector3.Zero; // В радианах
        public Vector3 Scale { get; set; } = Vector3.One;

        public Mesh Mesh { get; }
        public Shader Shader { get; }

        public GameObject(Mesh mesh, Shader shader)
        {
            Mesh = mesh;
            Shader = shader;
        }

        public Matrix4 GetModelMatrix()
        {
            var scaleMatrix = Matrix4.CreateScale(Scale);
            var rotationX = Matrix4.CreateRotationX(Rotation.X);
            var rotationY = Matrix4.CreateRotationY(Rotation.Y);
            var rotationZ = Matrix4.CreateRotationZ(Rotation.Z);
            var translationMatrix = Matrix4.CreateTranslation(Position);

            return scaleMatrix * rotationX * rotationY * rotationZ * translationMatrix;
        }

        public void Render(Matrix4 view, Matrix4 projection)
        {
            Shader.Use();
            Shader.SetMatrix4("model", GetModelMatrix());
            Shader.SetMatrix4("view", view);
            Shader.SetMatrix4("projection", projection);

            Mesh.Render();
        }
    }
}