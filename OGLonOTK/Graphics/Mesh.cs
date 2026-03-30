using OpenTK.Graphics.OpenGL4;

namespace OGLonOTK.Graphics
{
    public class Mesh
    {
        private readonly float[] _vertices;
        private readonly uint[] _indices;

        private int _vertexArrayObject;
        private int _vertexBufferObject;
        private int _elementBufferObject;

        public Mesh(float[] vertices, uint[] indices)
        {
            _vertices = vertices;
            _indices = indices;
        }

        public void Load()
        {
            _vertexArrayObject = GL.GenVertexArray();
            GL.BindVertexArray(_vertexArrayObject);

            _vertexBufferObject = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBufferObject);
            GL.BufferData(
                BufferTarget.ArrayBuffer,
                _vertices.Length * sizeof(float),
                _vertices,
                BufferUsageHint.StaticDraw);

            _elementBufferObject = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _elementBufferObject);
            GL.BufferData(
                BufferTarget.ElementArrayBuffer,
                _indices.Length * sizeof(uint),
                _indices,
                BufferUsageHint.StaticDraw);

            // Атрибут 0: позиция (x, y, z)
            GL.VertexAttribPointer(
                index: 0,
                size: 3,
                type: VertexAttribPointerType.Float,
                normalized: false,
                stride: 6 * sizeof(float),
                offset: 0);
            GL.EnableVertexAttribArray(0);

            // Атрибут 1: цвет (r, g, b)
            GL.VertexAttribPointer(
                index: 1,
                size: 3,
                type: VertexAttribPointerType.Float,
                normalized: false,
                stride: 6 * sizeof(float),
                offset: 3 * sizeof(float));
            GL.EnableVertexAttribArray(1);

            GL.BindVertexArray(0);
        }

        public void Render()
        {
            GL.BindVertexArray(_vertexArrayObject);
            GL.DrawElements(PrimitiveType.Triangles, _indices.Length, DrawElementsType.UnsignedInt, 0);
        }

        public void Unload()
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindVertexArray(0);

            GL.DeleteBuffer(_vertexBufferObject);
            GL.DeleteBuffer(_elementBufferObject);
            GL.DeleteVertexArray(_vertexArrayObject);
        }
    }
}