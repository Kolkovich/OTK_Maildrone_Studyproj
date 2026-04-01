using OpenTK.Mathematics;
using OGLonOTK.Graphics;

namespace OGLonOTK.World
{
    public class Particle : GameObject
    {
        public Vector3 Velocity { get; set; } = Vector3.Zero;
        public float Life { get; set; } = 0f;
        public bool IsAlive => Life > 0f;

        public Particle(Mesh mesh, Shader shader) : base(mesh, shader)
        {
        }

        public void Update(float dt)
        {
            if (!IsAlive)
                return;

            Life -= dt;
            if (Life <= 0f)
                return;

            Velocity += new Vector3(0f, -9.81f, 0f) * dt;
            Position += Velocity * dt;
        }
    }
}