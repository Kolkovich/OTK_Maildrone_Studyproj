using OpenTK.Mathematics;
using OGLonOTK.Graphics;

namespace OGLonOTK.World
{
    public class CargoSphere : GameObject
    {
        public Vector3 Velocity { get; set; } = Vector3.Zero;
        public float Radius { get; }
        public bool IsGrounded { get; set; }

        public float Gravity { get; set; } = 9.81f;
        public float GroundDamping { get; set; } = 0.92f;
        public float AirDamping { get; set; } = 0.99f;

        public CargoSphere(Mesh mesh, Shader shader, float radius) : base(mesh, shader)
        {
            Radius = radius;
        }

        public void AddImpulse(Vector3 impulse)
        {
            Velocity += impulse;
        }
    }
}