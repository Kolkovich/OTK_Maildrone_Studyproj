using System.Collections.Generic;
using OpenTK.Mathematics;
using OGLonOTK.Graphics;

namespace OGLonOTK.World
{
    public class Drone : GameObject
    {
        public float MoveSpeed { get; set; } = 2.5f;
        public float VerticalSpeed { get; set; } = 2.0f;
        public float RotationSpeed { get; set; } = MathHelper.DegreesToRadians(90f);

        public List<DronePart> Parts { get; } = new();

        public Drone(Mesh mesh, Shader shader) : base(mesh, shader)
        {
        }

        public Vector3 GetForward()
        {
            Vector3 forward = new Vector3(
                MathF.Sin(Rotation.Y),
                0f,
                MathF.Cos(Rotation.Y)
            );

            return Vector3.Normalize(forward);
        }

        public void RenderComposite(Matrix4 view, Matrix4 projection)
        {
            Matrix4 parentModel = GetModelMatrix();

            foreach (var part in Parts)
            {
                part.Render(parentModel, view, projection);
            }
        }
    }
}