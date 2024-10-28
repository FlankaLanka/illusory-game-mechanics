using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class PlaneGenerator : MonoBehaviour
{
    public int widthSegments = 10;
    public int heightSegments = 10;
    public float width = 1f;
    public float height = 1f;

    public Material waterMat;

    void Start()
    {
        GeneratePlane();
    }

    void GeneratePlane()
    {
        Mesh mesh = new Mesh();
        Vector3[] vertices = new Vector3[(widthSegments + 1) * (heightSegments + 1)];
        Vector2[] uv = new Vector2[vertices.Length];
        int[] triangles = new int[widthSegments * heightSegments * 6];

        // Set up vertices and UVs
        int vertIndex = 0;
        for (int i = 0; i <= heightSegments; i++)
        {
            for (int j = 0; j <= widthSegments; j++)
            {
                float x = (j / (float)widthSegments) * width;
                float z = (i / (float)heightSegments) * height;
                vertices[vertIndex] = new Vector3(x, 0, z);
                uv[vertIndex] = new Vector2(j / (float)widthSegments, i / (float)heightSegments);
                vertIndex++;
            }
        }

        // Set up triangles
        int triIndex = 0;
        for (int i = 0; i < heightSegments; i++)
        {
            for (int j = 0; j < widthSegments; j++)
            {
                int bottomLeft = i * (widthSegments + 1) + j;
                int bottomRight = bottomLeft + 1;
                int topLeft = bottomLeft + widthSegments + 1;
                int topRight = topLeft + 1;

                // First triangle
                triangles[triIndex++] = bottomLeft;
                triangles[triIndex++] = topLeft;
                triangles[triIndex++] = topRight;

                // Second triangle
                triangles[triIndex++] = bottomLeft;
                triangles[triIndex++] = topRight;
                triangles[triIndex++] = bottomRight;
            }
        }

        // Assign to mesh
        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        // Apply to the MeshFilter component
        GetComponent<MeshFilter>().mesh = mesh;
        GetComponent<MeshRenderer>().material = waterMat;
    }
}
