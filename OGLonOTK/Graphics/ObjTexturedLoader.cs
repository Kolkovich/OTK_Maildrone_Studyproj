using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using OpenTK.Mathematics;

namespace OGLonOTK.Graphics
{
    public static class ObjTexturedLoader
    {
        private class VertexKey : IEquatable<VertexKey>
        {
            public int PositionIndex { get; }
            public int TexCoordIndex { get; }

            public VertexKey(int positionIndex, int texCoordIndex)
            {
                PositionIndex = positionIndex;
                TexCoordIndex = texCoordIndex;
            }

            public bool Equals(VertexKey other)
            {
                if (other == null) return false;
                return PositionIndex == other.PositionIndex && TexCoordIndex == other.TexCoordIndex;
            }

            public override bool Equals(object obj) => Equals(obj as VertexKey);
            public override int GetHashCode() => HashCode.Combine(PositionIndex, TexCoordIndex);
        }

        private class SubMeshBuilder
        {
            public List<float> Vertices { get; } = new();
            public List<uint> Indices { get; } = new();
            public Dictionary<VertexKey, uint> VertexMap { get; } = new();
        }

        public static List<ObjSubMesh> Load(
            string objPath,
            Dictionary<string, Texture> materialTextures)
        {
            var lines = File.ReadAllLines(objPath);

            var positions = new List<Vector3>();
            var texCoords = new List<Vector2>();

            foreach (var raw in lines)
            {
                var line = raw.Trim();

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
                else if (line.StartsWith("vt "))
                {
                    var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    float u = float.Parse(parts[1], CultureInfo.InvariantCulture);
                    float v = float.Parse(parts[2], CultureInfo.InvariantCulture);
                    texCoords.Add(new Vector2(u, v));
                }
            }

            CenterAndNormalize(positions);

            var builders = new Dictionary<string, SubMeshBuilder>();
            string currentMaterial = "default";

            foreach (var raw in lines)
            {
                var line = raw.Trim();

                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                    continue;

                if (line.StartsWith("usemtl "))
                {
                    currentMaterial = line.Substring("usemtl ".Length).Trim();
                    if (!builders.ContainsKey(currentMaterial))
                        builders[currentMaterial] = new SubMeshBuilder();
                }
                else if (line.StartsWith("f "))
                {
                    if (!builders.ContainsKey(currentMaterial))
                        builders[currentMaterial] = new SubMeshBuilder();

                    var builder = builders[currentMaterial];
                    var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                    var faceKeys = new List<VertexKey>();

                    for (int i = 1; i < parts.Length; i++)
                    {
                        var token = parts[i];
                        var split = token.Split('/');

                        int posIndex = int.Parse(split[0], CultureInfo.InvariantCulture) - 1;
                        int texIndex = split.Length > 1 && !string.IsNullOrWhiteSpace(split[1])
                            ? int.Parse(split[1], CultureInfo.InvariantCulture) - 1
                            : -1;

                        faceKeys.Add(new VertexKey(posIndex, texIndex));
                    }

                    for (int i = 1; i < faceKeys.Count - 1; i++)
                    {
                        AddVertex(faceKeys[0], positions, texCoords, builder);
                        AddVertex(faceKeys[i], positions, texCoords, builder);
                        AddVertex(faceKeys[i + 1], positions, texCoords, builder);
                    }
                }
            }

            var result = new List<ObjSubMesh>();

            foreach (var pair in builders)
            {
                string materialName = pair.Key;
                var builder = pair.Value;

                if (!materialTextures.TryGetValue(materialName, out var texture))
                    continue;

                var mesh = new TexturedMesh(builder.Vertices.ToArray(), builder.Indices.ToArray());
                mesh.Load();

                result.Add(new ObjSubMesh(materialName, mesh, texture));
            }

            return result;
        }

        private static void AddVertex(
            VertexKey key,
            List<Vector3> positions,
            List<Vector2> texCoords,
            SubMeshBuilder builder)
        {
            if (!builder.VertexMap.TryGetValue(key, out uint index))
            {
                var position = positions[key.PositionIndex];
                var uv = key.TexCoordIndex >= 0 ? texCoords[key.TexCoordIndex] : Vector2.Zero;

                index = (uint)(builder.Vertices.Count / 5);

                builder.Vertices.Add(position.X);
                builder.Vertices.Add(position.Y);
                builder.Vertices.Add(position.Z);
                builder.Vertices.Add(uv.X);
                builder.Vertices.Add(uv.Y);

                builder.VertexMap[key] = index;
            }

            builder.Indices.Add(index);
        }

        private static void CenterAndNormalize(List<Vector3> positions)
        {
            if (positions.Count == 0)
                return;

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

            float scale = 1f / maxDimension;

            for (int i = 0; i < positions.Count; i++)
            {
                positions[i] = (positions[i] - center) * scale;
            }
        }
    }
}