using OpenTK.Mathematics;

namespace OGLonOTK.Physics
{
    public struct Aabb
    {
        public Vector3 Min { get; }
        public Vector3 Max { get; }

        public Aabb(Vector3 min, Vector3 max)
        {
            Min = min;
            Max = max;
        }

        public bool Intersects(Aabb other)
        {
            return
                Min.X <= other.Max.X && Max.X >= other.Min.X &&
                Min.Y <= other.Max.Y && Max.Y >= other.Min.Y &&
                Min.Z <= other.Max.Z && Max.Z >= other.Min.Z;
        }
    }
}