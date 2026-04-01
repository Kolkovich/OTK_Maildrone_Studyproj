using System;
using System.Collections.Generic;
using OpenTK.Mathematics;

namespace OGLonOTK.Graphics
{
    public static class MeshFactory
    {
        public static (float[] vertices, uint[] indices) CreateCubeVertices()
        {
            float[] vertices =
            {
                // Front face (red)
                -0.5f, -0.5f,  0.5f,   1.0f, 0.0f, 0.0f,
                 0.5f, -0.5f,  0.5f,   1.0f, 0.0f, 0.0f,
                 0.5f,  0.5f,  0.5f,   1.0f, 0.0f, 0.0f,
                -0.5f,  0.5f,  0.5f,   1.0f, 0.0f, 0.0f,

                // Back face (green)
                -0.5f, -0.5f, -0.5f,   0.0f, 1.0f, 0.0f,
                 0.5f, -0.5f, -0.5f,   0.0f, 1.0f, 0.0f,
                 0.5f,  0.5f, -0.5f,   0.0f, 1.0f, 0.0f,
                -0.5f,  0.5f, -0.5f,   0.0f, 1.0f, 0.0f,

                // Left face (blue)
                -0.5f, -0.5f, -0.5f,   0.0f, 0.0f, 1.0f,
                -0.5f, -0.5f,  0.5f,   0.0f, 0.0f, 1.0f,
                -0.5f,  0.5f,  0.5f,   0.0f, 0.0f, 1.0f,
                -0.5f,  0.5f, -0.5f,   0.0f, 0.0f, 1.0f,

                // Right face (yellow)
                 0.5f, -0.5f, -0.5f,   1.0f, 1.0f, 0.0f,
                 0.5f, -0.5f,  0.5f,   1.0f, 1.0f, 0.0f,
                 0.5f,  0.5f,  0.5f,   1.0f, 1.0f, 0.0f,
                 0.5f,  0.5f, -0.5f,   1.0f, 1.0f, 0.0f,

                // Top face (magenta)
                -0.5f,  0.5f, -0.5f,   1.0f, 0.0f, 1.0f,
                -0.5f,  0.5f,  0.5f,   1.0f, 0.0f, 1.0f,
                 0.5f,  0.5f,  0.5f,   1.0f, 0.0f, 1.0f,
                 0.5f,  0.5f, -0.5f,   1.0f, 0.0f, 1.0f,

                // Bottom face (cyan)
                -0.5f, -0.5f, -0.5f,   0.0f, 1.0f, 1.0f,
                -0.5f, -0.5f,  0.5f,   0.0f, 1.0f, 1.0f,
                 0.5f, -0.5f,  0.5f,   0.0f, 1.0f, 1.0f,
                 0.5f, -0.5f, -0.5f,   0.0f, 1.0f, 1.0f
            };

            uint[] indices =
            {
                0, 1, 2,  2, 3, 0,
                4, 5, 6,  6, 7, 4,
                8, 9, 10, 10, 11, 8,
                12, 13, 14, 14, 15, 12,
                16, 17, 18, 18, 19, 16,
                20, 21, 22, 22, 23, 20
            };

            return (vertices, indices);
        }

        public static (float[] vertices, uint[] indices) CreateUvSphere(
            float radius,
            int stacks,
            int slices,
            Vector3 color)
        {
            var vertices = new List<float>();
            var indices = new List<uint>();

            for (int stack = 0; stack <= stacks; stack++)
            {
                float v = stack / (float)stacks;
                float phi = MathF.PI * v;

                float y = MathF.Cos(phi);
                float ringRadius = MathF.Sin(phi);

                for (int slice = 0; slice <= slices; slice++)
                {
                    float u = slice / (float)slices;
                    float theta = 2.0f * MathF.PI * u;

                    float x = ringRadius * MathF.Cos(theta);
                    float z = ringRadius * MathF.Sin(theta);

                    vertices.Add(x * radius);
                    vertices.Add(y * radius);
                    vertices.Add(z * radius);

                    vertices.Add(color.X);
                    vertices.Add(color.Y);
                    vertices.Add(color.Z);
                }
            }

            for (int stack = 0; stack < stacks; stack++)
            {
                for (int slice = 0; slice < slices; slice++)
                {
                    uint first = (uint)(stack * (slices + 1) + slice);
                    uint second = first + (uint)slices + 1;

                    indices.Add(first);
                    indices.Add(second);
                    indices.Add(first + 1);

                    indices.Add(second);
                    indices.Add(second + 1);
                    indices.Add(first + 1);
                }
            }

            return (vertices.ToArray(), indices.ToArray());
        }

        public static (float[] vertices, uint[] indices) CreateCircle2D(float radius, int segments)
        {
            var vertices = new List<float>();
            var indices = new List<uint>();

            vertices.Add(0f);
            vertices.Add(0f);

            for (int i = 0; i <= segments; i++)
            {
                float angle = i / (float)segments * MathF.PI * 2f;
                float x = MathF.Cos(angle) * radius;
                float y = MathF.Sin(angle) * radius;

                vertices.Add(x);
                vertices.Add(y);
            }

            for (uint i = 1; i <= segments; i++)
            {
                indices.Add(0);
                indices.Add(i);
                indices.Add(i + 1);
            }

            return (vertices.ToArray(), indices.ToArray());
        }

        public static (float[] vertices, uint[] indices) CreateCross2D(float halfLength, float halfThickness)
        {
            float d = halfLength;
            float t = halfThickness;

            float[] vertices =
            {
                // Первая диагональ
                -d, -d + t,
                -d + t, -d,
                d,  d - t,
                d - t,  d,

                // Вторая диагональ
                -d,  d - t,
                -d + t,  d,
                d, -d + t,
                d - t, -d
            };

            uint[] indices =
            {
                0, 1, 2,
                2, 3, 0,

                4, 5, 6,
                6, 7, 4
            };

            return (vertices, indices);
        }

        public static (float[] vertices, uint[] indices) CreateTexturedCubeVertices(float uvScale = 1.0f)
        {
            float u0 = 0f;
            float v0 = 0f;
            float u1 = uvScale;
            float v1 = uvScale;

            float[] vertices =
            {
        // Front
        -0.5f, -0.5f,  0.5f,   u0, v0,
         0.5f, -0.5f,  0.5f,   u1, v0,
         0.5f,  0.5f,  0.5f,   u1, v1,
        -0.5f,  0.5f,  0.5f,   u0, v1,

        // Back
        -0.5f, -0.5f, -0.5f,   u1, v0,
         0.5f, -0.5f, -0.5f,   u0, v0,
         0.5f,  0.5f, -0.5f,   u0, v1,
        -0.5f,  0.5f, -0.5f,   u1, v1,

        // Left
        -0.5f, -0.5f, -0.5f,   u0, v0,
        -0.5f, -0.5f,  0.5f,   u1, v0,
        -0.5f,  0.5f,  0.5f,   u1, v1,
        -0.5f,  0.5f, -0.5f,   u0, v1,

        // Right
         0.5f, -0.5f, -0.5f,   u1, v0,
         0.5f, -0.5f,  0.5f,   u0, v0,
         0.5f,  0.5f,  0.5f,   u0, v1,
         0.5f,  0.5f, -0.5f,   u1, v1,

        // Top
        -0.5f,  0.5f, -0.5f,   u0, v1,
        -0.5f,  0.5f,  0.5f,   u0, v0,
         0.5f,  0.5f,  0.5f,   u1, v0,
         0.5f,  0.5f, -0.5f,   u1, v1,

        // Bottom
        -0.5f, -0.5f, -0.5f,   u0, v0,
        -0.5f, -0.5f,  0.5f,   u0, v1,
         0.5f, -0.5f,  0.5f,   u1, v1,
         0.5f, -0.5f, -0.5f,   u1, v0
    };

            uint[] indices =
            {
        0, 1, 2,  2, 3, 0,
        4, 5, 6,  6, 7, 4,
        8, 9, 10, 10, 11, 8,
        12, 13, 14, 14, 15, 12,
        16, 17, 18, 18, 19, 16,
        20, 21, 22, 22, 23, 20
    };

            return (vertices, indices);
        }

        // Всё что ниже для поверхности вращения

        private static Vector2 CatmullRom(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;

            return 0.5f * (
                (2f * p1) +
                (-p0 + p2) * t +
                (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
                (-p0 + 3f * p1 - 3f * p2 + p3) * t3
            );
        }

        private static List<Vector2> SampleCatmullRomCurve(List<Vector2> controlPoints, int samplesPerSegment)
        {
            var result = new List<Vector2>();

            if (controlPoints.Count < 4)
                throw new ArgumentException("Need at least 4 control points for Catmull-Rom spline.");

            for (int i = 0; i <= controlPoints.Count - 4; i++)
            {
                Vector2 p0 = controlPoints[i];
                Vector2 p1 = controlPoints[i + 1];
                Vector2 p2 = controlPoints[i + 2];
                Vector2 p3 = controlPoints[i + 3];

                for (int s = 0; s < samplesPerSegment; s++)
                {
                    float t = s / (float)samplesPerSegment;
                    Vector2 point = CatmullRom(p0, p1, p2, p3, t);

                    if (point.X < 0f)
                        point.X = 0f;

                    result.Add(point);
                }
            }

            result.Add(controlPoints[controlPoints.Count - 2]);

            return result;
        }

        public static (float[] vertices, uint[] indices) CreateSurfaceOfRevolution(
    List<Vector2> controlPoints,
    int samplesPerSegment,
    int radialSegments,
    Vector3 color)
        {
            var curve = SampleCatmullRomCurve(controlPoints, samplesPerSegment);

            var vertices = new List<float>();
            var indices = new List<uint>();

            for (int i = 0; i < curve.Count; i++)
            {
                float radius = curve[i].X;
                float y = curve[i].Y;

                for (int j = 0; j <= radialSegments; j++)
                {
                    float angle = j / (float)radialSegments * MathF.PI * 2f;

                    float x = radius * MathF.Cos(angle);
                    float z = radius * MathF.Sin(angle);

                    vertices.Add(x);
                    vertices.Add(y);
                    vertices.Add(z);

                    vertices.Add(color.X);
                    vertices.Add(color.Y);
                    vertices.Add(color.Z);
                }
            }

            int ringSize = radialSegments + 1;

            for (int i = 0; i < curve.Count - 1; i++)
            {
                for (int j = 0; j < radialSegments; j++)
                {
                    uint current = (uint)(i * ringSize + j);
                    uint next = (uint)((i + 1) * ringSize + j);

                    indices.Add(current);
                    indices.Add(next);
                    indices.Add(current + 1);

                    indices.Add(current + 1);
                    indices.Add(next);
                    indices.Add(next + 1);
                }
            }

            return (vertices.ToArray(), indices.ToArray());
        }

        // Спрайтовое
        public static (float[] vertices, uint[] indices) CreateTexturedQuad()
        {
            float[] vertices =
            {
        // x, y, z,  u, v
        -0.5f, -0.5f, 0f,  0f, 0f,
         0.5f, -0.5f, 0f,  1f, 0f,
         0.5f,  0.5f, 0f,  1f, 1f,
        -0.5f,  0.5f, 0f,  0f, 1f
    };

            uint[] indices =
            {
        0, 1, 2,
        2, 3, 0
    };

            return (vertices, indices);
        }

        public static (float[] vertices, uint[] indices) CreateFullscreenQuad()
        {
            float[] vertices =
            {
        // x, y, z,   u, v
        -1f, -1f, 0f, 0f, 0f,
         1f, -1f, 0f, 1f, 0f,
         1f,  1f, 0f, 1f, 1f,
        -1f,  1f, 0f, 0f, 1f
    };

            uint[] indices =
            {
        0, 1, 2,
        2, 3, 0
    };

            return (vertices, indices);
        }
    }
}