using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshSlicer
{
    public static (GameObject, GameObject) Cut(GameObject originalObject, Vector3 planeNormal, Vector3 planePoint)
    {
        //check mesh requirements
        MeshFilter mainMeshFilter = originalObject.GetComponent<MeshFilter>();
        MeshRenderer mainMeshRenderer = originalObject.GetComponent<MeshRenderer>();
        if (mainMeshFilter == null || mainMeshFilter.mesh == null || mainMeshRenderer == null)
        {
            Debug.Log(originalObject.name + " (GameObject) needs MeshFilter and MeshRenderer to cut.");
            return (null, null);
        }

        //check plane requirements
        if (planeNormal == Vector3.zero)
        {
            Debug.Log("Plane Direction Invalid");
            return (null, null);
        }
        planeNormal = planeNormal.normalized;

        //start cutting
        Mesh leftMesh = new();
        Mesh rightMesh = new();
        SeparateMesh(mainMeshFilter.mesh, leftMesh, rightMesh, originalObject.transform, planeNormal, planePoint);

        return (ConstructGameObjectFromMesh(leftMesh, mainMeshRenderer, originalObject, "left"), ConstructGameObjectFromMesh(rightMesh, mainMeshRenderer, originalObject, "right"));
    }

    public class CustomMeshDataStruct
    {
        public List<Vector3> verticesList = new();
        public List<Vector3> normalsList = new();
        public List<Vector2> uvsList = new();
        public List<int> trianglesList = new();
    }

    private static void SeparateMesh(Mesh mainMesh, Mesh leftMesh, Mesh rightMesh, Transform meshTransform, Vector3 planeNormal, Vector3 planePoint)
    {
        List<Vector3> verticesLeft = new();
        List<Vector3> verticesRight = new();

        List<Vector3> normalsLeft = new();
        List<Vector3> normalsRight = new();

        List<Vector2> uvsLeft = new();
        List<Vector2> uvsRight = new();

        List<int> trianglesLeft = new();
        List<int> trianglesRight = new();

        CustomMeshDataStruct customLeftMesh = new();
        CustomMeshDataStruct customRightMesh = new();


        for (int i = 0; i < mainMesh.triangles.Length; i += 3)
        {
            int v1 = mainMesh.triangles[i];
            int v2 = mainMesh.triangles[i + 1];
            int v3 = mainMesh.triangles[i + 2];

            //use world space to check plane side
            Vector3 worldV1 = meshTransform.TransformPoint(mainMesh.vertices[v1]);
            Vector3 worldV2 = meshTransform.TransformPoint(mainMesh.vertices[v2]);
            Vector3 worldV3 = meshTransform.TransformPoint(mainMesh.vertices[v3]);

            bool v1NormalSide = OnNormalSideOfPlane(planeNormal, planePoint, worldV1);
            bool v2NormalSide = OnNormalSideOfPlane(planeNormal, planePoint, worldV2);
            bool v3NormalSide = OnNormalSideOfPlane(planeNormal, planePoint, worldV3);


            //potential TODO: improve vertex storage. Instead of creating duplicate, use dictionary to map
            if (v1NormalSide && v2NormalSide && v3NormalSide)
            {
                // Add vertices, normals, uvs, and triangles to the left mesh
                AddTriangleToMesh(v1, v2, v3, mainMesh, verticesLeft, normalsLeft, uvsLeft, trianglesLeft);
                //AddTriangleToMesh(v1, v2, v3, mainMesh, customLeftMesh);
            }
            else if (!v1NormalSide && !v2NormalSide && !v3NormalSide)
            {
                // Add vertices, normals, uvs, and triangles to the right mesh
                AddTriangleToMesh(v1, v2, v3, mainMesh, verticesRight, normalsRight, uvsRight, trianglesRight);
                //AddTriangleToMesh(v1, v2, v3, mainMesh, customRightMesh);
            }
            else
            {
                // Handle triangles that intersect the plane (optional)
                // This involves splitting the triangle and generating new vertices/normals/UVs

                if (v1NormalSide && !v2NormalSide && !v3NormalSide)
                {
                    Patch((v2, v3), v1, verticesRight, verticesLeft, planeNormal, planePoint, mainMesh);
                }
                else if (v1NormalSide && v2NormalSide && !v3NormalSide)
                {
                    Patch((v1, v2), v3, verticesLeft, verticesRight, planeNormal, planePoint, mainMesh);
                }
                else if (v1NormalSide && !v2NormalSide && v3NormalSide)
                {
                    Patch((v3, v1), v2, verticesLeft, verticesRight, planeNormal, planePoint, mainMesh);
                }
                else if (!v1NormalSide && v2NormalSide && v3NormalSide)
                {
                    Patch((v2, v3), v1, verticesLeft, verticesRight, planeNormal, planePoint, mainMesh);
                }
                else if (!v1NormalSide && v2NormalSide && !v3NormalSide)
                {
                    Patch((v3, v1), v2, verticesRight, verticesLeft, planeNormal, planePoint, mainMesh);
                }
                else if (!v1NormalSide && !v2NormalSide && v3NormalSide)
                {
                    Patch((v1, v2), v3, verticesRight, verticesLeft, planeNormal, planePoint, mainMesh);
                }
            }
        }

        leftMesh.vertices = verticesLeft.ToArray();
        rightMesh.vertices = verticesRight.ToArray();

        leftMesh.normals = normalsLeft.ToArray();
        rightMesh.normals = normalsRight.ToArray();

        leftMesh.uv = uvsLeft.ToArray();
        rightMesh.uv = uvsRight.ToArray();

        leftMesh.triangles = trianglesLeft.ToArray();
        rightMesh.triangles = trianglesRight.ToArray();

        //Debug.Log("ORIGINAL INFO: vertices: " + mainMesh.vertices.Length + " triangles: " + mainMesh.triangles.Length);
        //Debug.Log("LEFT INFO: vertices: " + leftMesh.vertices.Length + " triangles: " + leftMesh.triangles.Length);
        //Debug.Log("RIGHT INFO: vertices: " + rightMesh.vertices.Length + " triangles: " + rightMesh.triangles.Length);

    }


    private static void AddTriangleToMesh(int v1Index, int v2Index, int v3Index, Mesh mainMesh,
                                          List<Vector3> verticesList, List<Vector3> normalsList, List<Vector2> uvsList, List<int> trianglesList)
    {
        verticesList.Add(mainMesh.vertices[v1Index]);
        verticesList.Add(mainMesh.vertices[v2Index]);
        verticesList.Add(mainMesh.vertices[v3Index]);

        normalsList.Add(mainMesh.normals[v1Index]);
        normalsList.Add(mainMesh.normals[v2Index]);
        normalsList.Add(mainMesh.normals[v3Index]);

        uvsList.Add(mainMesh.uv[v1Index]);
        uvsList.Add(mainMesh.uv[v2Index]);
        uvsList.Add(mainMesh.uv[v3Index]);

        trianglesList.Add(verticesList.Count - 3);
        trianglesList.Add(verticesList.Count - 2);
        trianglesList.Add(verticesList.Count - 1);
    }

    private static void AddTriangleToMesh(int v1Index, int v2Index, int v3Index, Mesh mainMesh, CustomMeshDataStruct customMesh)
    {
        customMesh.verticesList.Add(mainMesh.vertices[v1Index]);
        customMesh.verticesList.Add(mainMesh.vertices[v2Index]);
        customMesh.verticesList.Add(mainMesh.vertices[v3Index]);

        customMesh.normalsList.Add(mainMesh.normals[v1Index]);
        customMesh.normalsList.Add(mainMesh.normals[v2Index]);
        customMesh.normalsList.Add(mainMesh.normals[v3Index]);

        customMesh.uvsList.Add(mainMesh.uv[v1Index]);
        customMesh.uvsList.Add(mainMesh.uv[v2Index]);
        customMesh.uvsList.Add(mainMesh.uv[v3Index]);

        customMesh.trianglesList.Add(customMesh.verticesList.Count - 3);
        customMesh.trianglesList.Add(customMesh.verticesList.Count - 2);
        customMesh.trianglesList.Add(customMesh.verticesList.Count - 1);
    }

    private static void Patch((int,int) vIndexSameSide, int vIndexOppositeSide,
                              List<Vector3> verticesListDuo, List<Vector3> verticesListSolo, Vector3 planeNormal, Vector3 planePoint, Mesh mainMesh)
    {
        Vector3 lerpedVertex1 = FindLerpOnPlane(planeNormal, planePoint, mainMesh.vertices[vIndexSameSide.Item1], mainMesh.vertices[vIndexOppositeSide]);
        Vector3 lerpedVertex2 = FindLerpOnPlane(planeNormal, planePoint, mainMesh.vertices[vIndexSameSide.Item2], mainMesh.vertices[vIndexOppositeSide]);

        //patch single, order of adding is important
        verticesListSolo.Add(mainMesh.vertices[vIndexOppositeSide]);


        //patch double
        
    }

    private static void AddNewTriangleToMesh()
    {

    }

    private static void PatchDouble()
    {

    }


    private static GameObject ConstructGameObjectFromMesh(Mesh createdMesh, MeshRenderer originalMeshRenderer, GameObject originalObject, string side)
    {
        GameObject g = new GameObject("Dupe of " + originalObject.name + side);
        g.AddComponent<MeshFilter>().mesh = createdMesh;
        g.AddComponent<MeshRenderer>().material = originalMeshRenderer.material;

        g.transform.SetPositionAndRotation(originalObject.transform.position, originalObject.transform.rotation);
        g.transform.localScale = originalObject.transform.lossyScale;

        return g;
    }


    #region MathHelpers

    private static bool OnNormalSideOfPlane(Vector3 planeNormal, Vector3 planePoint, Vector3 vertexPoint)
    {
        return Vector3.Dot(planeNormal, vertexPoint - planePoint) >= 0;
    }

    private static Vector3 FindLerpOnPlane(Vector3 planeNormal, Vector3 planePoint, Vector3 vertexA, Vector3 vertexB)
    {
        // Calculate the direction vector between the two vertices
        Vector3 edgeDirection = vertexB - vertexA;

        // Calculate the distance from each vertex to the plane
        float distanceA = Vector3.Dot(planeNormal, vertexA - planePoint);
        float distanceB = Vector3.Dot(planeNormal, vertexB - planePoint);

        // Calculate the interpolation factor (lerp value)
        float t = distanceA / (distanceA - distanceB);

        // Lerp between the two vertices based on the factor
        return Vector3.Lerp(vertexA, vertexB, t);
    }

    #endregion
}
