using OpenTK.Mathematics;

namespace OGLonOTK.Graphics
{
    public class Camera
    {
        public Vector3 Position { get; set; }
        public Vector3 Front { get; set; } = -Vector3.UnitZ;
        public Vector3 Up { get; set; } = Vector3.UnitY;

        public float Speed { get; set; } = 2.5f;
        public float Sensitivity { get; set; } = 0.2f;

        public float Pitch { get; set; } = 0.0f;
        public float Yaw { get; set; } = -90.0f;

        public Camera(Vector3 position)
        {
            Position = position;
        }

        public Matrix4 GetViewMatrix()
        {
            return Matrix4.LookAt(Position, Position + Front, Up);
        }

        public void MoveForward(float deltaTime)
        {
            Position += Front * Speed * deltaTime;
        }

        public void MoveBackward(float deltaTime)
        {
            Position -= Front * Speed * deltaTime;
        }

        public void MoveRight(float deltaTime)
        {
            var right = Vector3.Normalize(Vector3.Cross(Front, Up));
            Position += right * Speed * deltaTime;
        }

        public void MoveLeft(float deltaTime)
        {
            var right = Vector3.Normalize(Vector3.Cross(Front, Up));
            Position -= right * Speed * deltaTime;
        }

        public void AddRotation(float deltaX, float deltaY)
        {
            Yaw += deltaX * Sensitivity;
            Pitch -= deltaY * Sensitivity;

            Pitch = MathHelper.Clamp(Pitch, -89f, 89f);

            Vector3 direction;
            direction.X = MathF.Cos(MathHelper.DegreesToRadians(Yaw)) * MathF.Cos(MathHelper.DegreesToRadians(Pitch));
            direction.Y = MathF.Sin(MathHelper.DegreesToRadians(Pitch));
            direction.Z = MathF.Sin(MathHelper.DegreesToRadians(Yaw)) * MathF.Cos(MathHelper.DegreesToRadians(Pitch));

            Front = Vector3.Normalize(direction);
        }
    }
}