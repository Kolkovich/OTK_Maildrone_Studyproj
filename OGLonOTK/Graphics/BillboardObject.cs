using OpenTK.Mathematics;

namespace OGLonOTK.Graphics
{
    public class BillboardObject
    {
        public Vector3 Position { get; set; } = Vector3.Zero;
        public Vector2 Size { get; set; } = new Vector2(1f, 1f);

        public TexturedMesh Mesh { get; }
        public Shader Shader { get; }
        public Texture Texture { get; }

        public BillboardObject(TexturedMesh mesh, Shader shader, Texture texture)
        {
            Mesh = mesh;
            Shader = shader;
            Texture = texture;
        }

        public void Render(Matrix4 view, Matrix4 projection, Vector3 cameraPosition)
        {
            Vector3 toCamera = Vector3.Normalize(cameraPosition - Position);
            float angleY = MathF.Atan2(toCamera.X, toCamera.Z);

            Matrix4 scale = Matrix4.CreateScale(Size.X, Size.Y, 1f);
            Matrix4 rotation = Matrix4.CreateRotationY(angleY);
            Matrix4 translation = Matrix4.CreateTranslation(Position);

            Matrix4 model = scale * rotation * translation;

            Shader.Use();
            Texture.Use();

            Shader.SetMatrix4("model", model);
            Shader.SetMatrix4("view", view);
            Shader.SetMatrix4("projection", projection);
            Shader.SetInt("texture0", 0);

            Mesh.Render();
        }
    }
}