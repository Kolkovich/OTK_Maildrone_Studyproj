using OpenTK.Mathematics;

namespace OGLonOTK.Graphics
{
    public class Camera
    {
        public Vector3 Position { get; set; }
        public Vector3 Front { get; set; } = -Vector3.UnitZ;
        public Vector3 Up { get; set; } = Vector3.UnitY;

        public Camera(Vector3 position)
        {
            Position = position;
        }

        public Matrix4 GetViewMatrix()
        {
            return Matrix4.LookAt(Position, Position + Front, Up);
        }
    }
}