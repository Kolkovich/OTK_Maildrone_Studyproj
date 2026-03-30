using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
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

        public void Update(float deltaTime, KeyboardState input)
        {
            var position = Position;
            var rotation = Rotation;

            if (input.IsKeyDown(Keys.Q))
            {
                rotation.Y += RotationSpeed * deltaTime;
            }

            if (input.IsKeyDown(Keys.E))
            {
                rotation.Y -= RotationSpeed * deltaTime;
            }

            Vector3 forward = new Vector3(
                MathF.Sin(rotation.Y),
                0f,
                MathF.Cos(rotation.Y)
            );

            forward = Vector3.Normalize(forward);

            Vector3 right = new Vector3(forward.Z, 0f, -forward.X);
            right = Vector3.Normalize(right);

            if (input.IsKeyDown(Keys.W))
            {
                position += forward * MoveSpeed * deltaTime;
            }

            if (input.IsKeyDown(Keys.S))
            {
                position -= forward * MoveSpeed * deltaTime;
            }

            if (input.IsKeyDown(Keys.A))
            {
                position -= right * MoveSpeed * deltaTime;
            }

            if (input.IsKeyDown(Keys.D))
            {
                position += right * MoveSpeed * deltaTime;
            }

            if (input.IsKeyDown(Keys.Space))
            {
                position.Y += VerticalSpeed * deltaTime;
            }

            if (input.IsKeyDown(Keys.LeftShift))
            {
                position.Y -= VerticalSpeed * deltaTime;
            }

            Position = position;
            Rotation = rotation;
        }
    }
}