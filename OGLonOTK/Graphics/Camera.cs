using OpenTK.Mathematics;

namespace OGLonOTK.Graphics
{
    public class Camera
    {
        public Vector3 Position { get; set; }
        public Vector3 Front { get; set; } = -Vector3.UnitZ;
        public Vector3 Up { get; set; } = Vector3.UnitY;

        public float Speed { get; set; } = 2.5f;

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
    }
}