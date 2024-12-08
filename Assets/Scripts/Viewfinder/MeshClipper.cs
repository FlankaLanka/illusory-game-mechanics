using UnityEngine;
using System.Collections.Generic;

public class MeshClipper : MonoBehaviour
{
    public MeshFilter targetMeshFilter;

    public Vector3 planePoint;
    public Vector3 planeNormal;
    public Plane clippingPlane;

    void Start()
    {
        if (targetMeshFilter == null)
        {
            Debug.LogError("Target MeshFilter is not assigned.");
            return;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            clippingPlane.SetNormalAndPosition(planePoint, planeNormal.normalized);

            Mesh originalMesh = targetMeshFilter.mesh;
            Mesh clippedMesh = ClipMesh(originalMesh, clippingPlane);

            if (clippedMesh != null)
            {
                targetMeshFilter.mesh = clippedMesh;
            }
        }
    }

    Mesh ClipMesh(Mesh mesh, Plane clipPlane)
    {
        // Original mesh data
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;
        Vector3[] normals = mesh.normals;

        // New mesh data
        List<Vector3> newVertices = new List<Vector3>();
        List<int> newTriangles = new List<int>();
        List<Vector3> newNormals = new List<Vector3>();

        Dictionary<Vector3, int> vertexMapping = new Dictionary<Vector3, int>();

        for (int i = 0; i < triangles.Length; i += 3)
        {
            Vector3 v0 = vertices[triangles[i]];
            Vector3 v1 = vertices[triangles[i + 1]];
            Vector3 v2 = vertices[triangles[i + 2]];

            Vector3 n0 = normals[triangles[i]];
            Vector3 n1 = normals[triangles[i + 1]];
            Vector3 n2 = normals[triangles[i + 2]];

            bool side0 = clipPlane.GetSide(v0);
            bool side1 = clipPlane.GetSide(v1);
            bool side2 = clipPlane.GetSide(v2);

            if (side0 && side1 && side2)
            {
                // All vertices on the positive side, keep the triangle
                AddTriangle(newVertices, newTriangles, newNormals, vertexMapping, v0, v1, v2, n0, n1, n2);
            }
            else if (!(side0 || side1 || side2))
            {
                // All vertices on the negative side, discard the triangle
                continue;
            }
            else
            {
                // Triangle is intersected by the plane
                List<Vector3> clippedVerts = new List<Vector3>();
                List<Vector3> clippedNormals = new List<Vector3>();

                ClipTriangle(v0, v1, v2, n0, n1, n2, clipPlane, side0, side1, side2, clippedVerts, clippedNormals);

                // Triangulate the resulting polygon
                for (int j = 1; j < clippedVerts.Count - 1; j++)
                {
                    AddTriangle(
                        newVertices,
                        newTriangles,
                        newNormals,
                        vertexMapping,
                        clippedVerts[0], clippedVerts[j], clippedVerts[j + 1],
                        clippedNormals[0], clippedNormals[j], clippedNormals[j + 1]);
                }
            }
        }

        Mesh newMesh = new Mesh
        {
            vertices = newVertices.ToArray(),
            triangles = newTriangles.ToArray(),
            normals = newNormals.ToArray()
        };

        return newMesh;
    }

    void AddTriangle(List<Vector3> vertices, List<int> triangles, List<Vector3> normals, Dictionary<Vector3, int> vertexMapping,
                     Vector3 v0, Vector3 v1, Vector3 v2, Vector3 n0, Vector3 n1, Vector3 n2)
    {
        if (!vertexMapping.TryGetValue(v0, out int index0))
        {
            index0 = vertices.Count;
            vertices.Add(v0);
            normals.Add(n0);
            vertexMapping[v0] = index0;
        }

        if (!vertexMapping.TryGetValue(v1, out int index1))
        {
            index1 = vertices.Count;
            vertices.Add(v1);
            normals.Add(n1);
            vertexMapping[v1] = index1;
        }

        if (!vertexMapping.TryGetValue(v2, out int index2))
        {
            index2 = vertices.Count;
            vertices.Add(v2);
            normals.Add(n2);
            vertexMapping[v2] = index2;
        }

        triangles.Add(index0);
        triangles.Add(index1);
        triangles.Add(index2);
    }

    void ClipTriangle(Vector3 v0, Vector3 v1, Vector3 v2, Vector3 n0, Vector3 n1, Vector3 n2, Plane plane,
                      bool side0, bool side1, bool side2, List<Vector3> clippedVerts, List<Vector3> clippedNormals)
    {
        Vector3[] verts = { v0, v1, v2 };
        Vector3[] norms = { n0, n1, n2 };
        bool[] sides = { side0, side1, side2 };

        for (int i = 0; i < 3; i++)
        {
            int next = (i + 1) % 3;

            if (sides[i])
            {
                clippedVerts.Add(verts[i]);
                clippedNormals.Add(norms[i]);
            }

            if (sides[i] != sides[next])
            {
                float t = plane.Raycast(new Ray(verts[i], verts[next] - verts[i]), out float dist) ? dist : 0f;

                Vector3 intersection = Vector3.Lerp(verts[i], verts[next], t);
                Vector3 interpolatedNormal = Vector3.Lerp(norms[i], norms[next], t);

                clippedVerts.Add(intersection);
                clippedNormals.Add(interpolatedNormal);
            }
        }
    }


    private void OnDrawGizmos()
    {
        float planeSize = 5f;
        Gizmos.color = Color.green;

        // Get the center and orientation of the plane
        Vector3 planeCenter = planePoint;
        Quaternion planeRotation = Quaternion.LookRotation(Vector3.Cross(planeNormal, Vector3.right), planeNormal);

        // Draw the plane as a square
        Vector3 halfSize = new Vector3(planeSize / 2, 0, planeSize / 2);
        Vector3[] corners = new Vector3[]
        {
            planeCenter + planeRotation * new Vector3(-halfSize.x, 0, -halfSize.z),
            planeCenter + planeRotation * new Vector3(-halfSize.x, 0, halfSize.z),
            planeCenter + planeRotation * new Vector3(halfSize.x, 0, halfSize.z),
            planeCenter + planeRotation * new Vector3(halfSize.x, 0, -halfSize.z)
        };

        Gizmos.DrawLine(corners[0], corners[1]);
        Gizmos.DrawLine(corners[1], corners[2]);
        Gizmos.DrawLine(corners[2], corners[3]);
        Gizmos.DrawLine(corners[3], corners[0]);

        Gizmos.color = Color.red;
        Gizmos.DrawLine(planeCenter, planeCenter + planeNormal.normalized * planeSize / 2);
    }
}
