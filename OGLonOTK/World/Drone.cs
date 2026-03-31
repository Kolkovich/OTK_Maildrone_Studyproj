using OpenTK.Mathematics;
using OGLonOTK.Graphics;

namespace OGLonOTK.World
{
    public class Drone : GameObject
    {
        public float MoveSpeed { get; set; } = 2.5f;
        public float VerticalSpeed { get; set; } = 2.0f;
        public float RotationSpeed { get; set; } = MathHelper.DegreesToRadians(90f);

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
    }
}