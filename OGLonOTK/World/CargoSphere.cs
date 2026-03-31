using OGLonOTK.Graphics;
using OGLonOTK.World;
using OpenTK.Mathematics;

public class CargoSphere : GameObject
{
    public Vector3 Velocity { get; set; } = Vector3.Zero;
    public float Radius { get; }
    public bool IsGrounded { get; set; }

    public float Gravity { get; set; } = 9.81f;
    public float HorizontalDamping { get; set; } = 0.95f;

    public CargoSphere(Mesh mesh, Shader shader, float radius) : base(mesh, shader)
    {
        Radius = radius;
    }
}