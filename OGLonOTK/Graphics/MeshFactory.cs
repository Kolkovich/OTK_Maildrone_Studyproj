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
    }
}