using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using OpenTK.Mathematics;

namespace OGLonOTK.Graphics
{
    public static class ObjLoader
    {
        public static (float[] vertices, uint[] indices) LoadAsColoredMesh(string path, Vector3 color)
        {
            var lines = File.ReadAllLines(path);

            var positions = new List<Vector3>();

            // 1. Считываем все позиции
            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();

                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                    continue;

                if (line.StartsWith("v "))
                {
                    var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                    float x = float.Parse(parts[1], CultureInfo.InvariantCulture);
                    float y = float.Parse(parts[2], CultureInfo.InvariantCulture);
                    float z = float.Parse(parts[3], CultureInfo.InvariantCulture);

                    positions.Add(new Vector3(x, y, z));
                }
            }

            if (positions.Count == 0)
                throw new Exception("OBJ file does not contain any vertex positions.");

            // 2. Центрируем и нормализуем модель
            NormalizePositions(positions);

            var vertices = new List<float>();
            var indices = new List<uint>();
            uint currentIndex = 0;

            // 3. Считываем face-ы и триангулируем их
            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();

                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                    continue;

                if (line.StartsWith("f "))
                {
                    var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    var faceVertexIndices = new List<int>();

                    for (int i = 1; i < parts.Length; i++)
                    {
                        string token = parts[i];
                        string[] slashSplit = token.Split('/');

                        int positionIndex = int.Parse(slashSplit[0], CultureInfo.InvariantCulture);

                        // OBJ индексация начинается с 1
                        faceVertexIndices.Add(positionIndex - 1);
                    }

                    // Триангуляция веером
                    for (int i = 1; i < faceVertexIndices.Count - 1; i++)
                    {
                        int a = faceVertexIndices[0];
                        int b = faceVertexIndices[i];
                        int c = faceVertexIndices[i + 1];

                        AddVertex(positions[a], color, vertices);
                        indices.Add(currentIndex++);

                        AddVertex(positions[b], color, vertices);
                        indices.Add(currentIndex++);

                        AddVertex(positions[c], color, vertices);
                        indices.Add(currentIndex++);
                    }
                }
            }

            return (vertices.ToArray(), indices.ToArray());
        }

        private static void NormalizePositions(List<Vector3> positions)
        {
            Vector3 min = positions[0];
            Vector3 max = positions[0];

            foreach (var p in positions)
            {
                min = Vector3.ComponentMin(min, p);
                max = Vector3.ComponentMax(max, p);
            }

            Vector3 center = (min + max) / 2f;
            Vector3 size = max - min;

            float maxDimension = MathF.Max(size.X, MathF.Max(size.Y, size.Z));
            if (maxDimension <= 0f)
                maxDimension = 1f;

            // Нормализуем так, чтобы наибольший размер стал равен 1
            float scale = 1f / maxDimension;

            for (int i = 0; i < positions.Count; i++)
            {
                positions[i] = (positions[i] - center) * scale;
            }
        }

        private static void AddVertex(Vector3 position, Vector3 color, List<float> vertices)
        {
            vertices.Add(position.X);
            vertices.Add(position.Y);
            vertices.Add(position.Z);

            vertices.Add(color.X);
            vertices.Add(color.Y);
            vertices.Add(color.Z);
        }
    }
}