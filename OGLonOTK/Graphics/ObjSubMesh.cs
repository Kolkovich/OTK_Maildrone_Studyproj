namespace OGLonOTK.Graphics
{
    public class ObjSubMesh
    {
        public string MaterialName { get; }
        public TexturedMesh Mesh { get; }
        public Texture Texture { get; }

        public ObjSubMesh(string materialName, TexturedMesh mesh, Texture texture)
        {
            MaterialName = materialName;
            Mesh = mesh;
            Texture = texture;
        }
    }
}