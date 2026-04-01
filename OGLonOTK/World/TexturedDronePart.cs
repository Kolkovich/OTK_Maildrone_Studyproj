using OpenTK.Mathematics;
using OGLonOTK.Graphics;

namespace OGLonOTK.World
{
    public class TexturedDronePart
    {
        public TexturedMesh Mesh { get; }
        public Shader Shader { get; }
        public Texture Texture { get; }

        public Vector3 LocalPosition { get; set; } = Vector3.Zero;
        public Vector3 LocalRotation { get; set; } = Vector3.Zero;
        public Vector3 LocalScale { get; set; } = Vector3.One;

        public TexturedDronePart(TexturedMesh mesh, Shader shader, Texture texture)
        {
            Mesh = mesh;
            Shader = shader;
            Texture = texture;
        }

        public Matrix4 GetLocalModelMatrix()
        {
            var scaleMatrix = Matrix4.CreateScale(LocalScale);
            var rotationX = Matrix4.CreateRotationX(LocalRotation.X);
            var rotationY = Matrix4.CreateRotationY(LocalRotation.Y);
            var rotationZ = Matrix4.CreateRotationZ(LocalRotation.Z);
            var translationMatrix = Matrix4.CreateTranslation(LocalPosition);

            return scaleMatrix * rotationX * rotationY * rotationZ * translationMatrix;
        }

        public void Render(Matrix4 parentModel, Matrix4 view, Matrix4 projection)
        {
            Shader.Use();
            Texture.Use();

            Shader.SetMatrix4("model", GetLocalModelMatrix() * parentModel);
            Shader.SetMatrix4("view", view);
            Shader.SetMatrix4("projection", projection);
            Shader.SetInt("texture0", 0);

            Mesh.Render();
        }
    }
}