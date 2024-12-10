using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class MeshSlicer
{
    #region Helper Classes

    public class CustomMeshDataStruct
    {
        public List<Vector3> verticesList = new();
        public List<Vector3> normalsList = new();
        public List<Vector2> uvsList = new();
        public List<int> trianglesList = new();
    }

    public class CustomVertexDataStruct
    {
        public Vector3 vertex;
        public Vector3 normal;
        public Vector2 uv;
        public bool generated = false;

        public CustomVertexDataStruct() { }

        public CustomVertexDataStruct(Vector3 v, Vector3 n, Vector2 u)
        {
            vertex = v;
            normal = n;
            uv = u;
        }
    }

    #endregion


    public static (GameObject, GameObject) Cut(GameObject originalObject, Vector3 planeNormal, Vector3 planePoint)
    {
        if (originalObject.GetComponent<Sliceable>() == null)
        {
            return (null,null);
        }

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

        SaveMeshToFile(mainMeshFilter.mesh, "mainM");
        SaveMeshToFile(leftMesh, "leftM");
        SaveMeshToFile(rightMesh, "rightM");

        return (ConstructGameObjectFromMesh(leftMesh, mainMeshRenderer, originalObject, " left"), ConstructGameObjectFromMesh(rightMesh, mainMeshRenderer, originalObject, " right"));
    }


    private static void SeparateMesh(Mesh mainMesh, Mesh leftMesh, Mesh rightMesh, Transform meshTransform, Vector3 planeNormal, Vector3 planePoint)
    {
        CustomMeshDataStruct customLeftMesh = new();
        CustomMeshDataStruct customRightMesh = new();

        //used to check if vertex already in array
        Dictionary<(Vector3, Vector3, Vector2), int> vertexDataMapLeft = new();
        Dictionary<(Vector3, Vector3, Vector2), int> vertexDataMapRight = new();

        //used for fixing the hollowness after cutting
        List<int> generatedVerticesLeft = new();
        List<int> generatedVerticesRight = new();

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
                CustomVertexDataStruct datav1 = new(mainMesh.vertices[v1], mainMesh.normals[v1], mainMesh.uv[v1]);
                CustomVertexDataStruct datav2 = new(mainMesh.vertices[v2], mainMesh.normals[v2], mainMesh.uv[v2]);
                CustomVertexDataStruct datav3 = new(mainMesh.vertices[v3], mainMesh.normals[v3], mainMesh.uv[v3]);
                AddTriangleToMesh(datav1, datav2, datav3, customLeftMesh, vertexDataMapLeft, generatedVerticesLeft);
                //Debug.Log("HERE IN LEFT");
            }
            else if (!v1NormalSide && !v2NormalSide && !v3NormalSide)
            {
                CustomVertexDataStruct datav1 = new(mainMesh.vertices[v1], mainMesh.normals[v1], mainMesh.uv[v1]);
                CustomVertexDataStruct datav2 = new(mainMesh.vertices[v2], mainMesh.normals[v2], mainMesh.uv[v2]);
                CustomVertexDataStruct datav3 = new(mainMesh.vertices[v3], mainMesh.normals[v3], mainMesh.uv[v3]);
                AddTriangleToMesh(datav1, datav2, datav3, customRightMesh, vertexDataMapRight, generatedVerticesRight);
                //Debug.Log("HERE IN RIGHT");
            }
            else
            {
                if (v1NormalSide && !v2NormalSide && !v3NormalSide)
                {
                    Patch((v2, v3), v1, customRightMesh, customLeftMesh, planeNormal, planePoint, mainMesh, meshTransform, vertexDataMapRight, vertexDataMapLeft, generatedVerticesRight, generatedVerticesLeft);
                }
                else if (v1NormalSide && v2NormalSide && !v3NormalSide)
                {
                    Patch((v1, v2), v3, customLeftMesh, customRightMesh, planeNormal, planePoint, mainMesh, meshTransform, vertexDataMapLeft, vertexDataMapRight, generatedVerticesLeft, generatedVerticesRight);
                }
                else if (v1NormalSide && !v2NormalSide && v3NormalSide)
                {
                    Patch((v3, v1), v2, customLeftMesh, customRightMesh, planeNormal, planePoint, mainMesh, meshTransform, vertexDataMapLeft, vertexDataMapRight, generatedVerticesLeft, generatedVerticesRight);
                }
                else if (!v1NormalSide && v2NormalSide && v3NormalSide)
                {
                    Patch((v2, v3), v1, customLeftMesh, customRightMesh, planeNormal, planePoint, mainMesh, meshTransform, vertexDataMapLeft, vertexDataMapRight, generatedVerticesLeft, generatedVerticesRight);
                }
                else if (!v1NormalSide && v2NormalSide && !v3NormalSide)
                {
                    Patch((v3, v1), v2, customRightMesh, customLeftMesh, planeNormal, planePoint, mainMesh, meshTransform, vertexDataMapRight, vertexDataMapLeft, generatedVerticesRight, generatedVerticesLeft);
                }
                else if (!v1NormalSide && !v2NormalSide && v3NormalSide)
                {
                    Patch((v1, v2), v3, customRightMesh, customLeftMesh, planeNormal, planePoint, mainMesh, meshTransform, vertexDataMapRight, vertexDataMapLeft, generatedVerticesRight, generatedVerticesLeft);
                }
                //Debug.Log("HERE IN SLICE");
            }
        }

        //patched plane here
        Vector3 leftCenter = AddCenterPoint(customLeftMesh, generatedVerticesLeft, -planeNormal);
        SelectionSortAngles(generatedVerticesLeft, customLeftMesh, leftCenter, -planeNormal);
        PatchPlane(customLeftMesh, generatedVerticesLeft, -planeNormal);

        Debug.Log("CHECKPOINTTTTTTTTTT");
        for (int i = 0; i < generatedVerticesLeft.Count; i++)
        {
            Debug.Log(customLeftMesh.verticesList[generatedVerticesLeft[i]] + " Index is " + generatedVerticesLeft[i]);
        }

        foreach (var entry in vertexDataMapLeft)
        {
            var key = entry.Key;
            int value = entry.Value;

            //Debug.Log($"Key: Vertex = {key.Item1}, Normal = {key.Item2}, UV = {key.Item3}, Value: {value}");
        }


        Vector3 rightCenter = AddCenterPoint(customRightMesh, generatedVerticesRight, planeNormal);
        SelectionSortAngles(generatedVerticesRight, customRightMesh, rightCenter, planeNormal);
        PatchPlane(customRightMesh, generatedVerticesRight, planeNormal);


        leftMesh.vertices = customLeftMesh.verticesList.ToArray();
        rightMesh.vertices = customRightMesh.verticesList.ToArray();

        leftMesh.normals = customLeftMesh.normalsList.ToArray();
        rightMesh.normals = customRightMesh.normalsList.ToArray();

        leftMesh.uv = customLeftMesh.uvsList.ToArray();
        rightMesh.uv = customRightMesh.uvsList.ToArray();

        leftMesh.triangles = customLeftMesh.trianglesList.ToArray();
        rightMesh.triangles = customRightMesh.trianglesList.ToArray();
    }



    private static Vector3 AddCenterPoint(CustomMeshDataStruct customMesh, List<int> generatedVertices, Vector3 planeNormal)
    {
        CustomVertexDataStruct output = new();

        for(int i = 0; i < generatedVertices.Count; i++)
        {
            output.vertex += customMesh.verticesList[generatedVertices[i]];
            output.uv += customMesh.uvsList[generatedVertices[i]];
        }

        if(generatedVertices.Count != 0)
        {
            output.vertex /= generatedVertices.Count;
            output.uv /= generatedVertices.Count;
        }

        Debug.Log("CENTER POINT: " + output.vertex);

        customMesh.verticesList.Add(output.vertex);
        customMesh.normalsList.Add(planeNormal);
        customMesh.uvsList.Add(output.uv);

        return output.vertex;
    }

    private static void PatchPlane(CustomMeshDataStruct customMesh, List<int> generatedVertices, Vector3 planeNormal)
    {
        Vector3 a, b, c;

        Debug.Log("TRAIGGE");
        for(int i = 0; i < generatedVertices.Count - 1; i++)
        {
            c = customMesh.verticesList[customMesh.verticesList.Count - 1];
            a = customMesh.verticesList[generatedVertices[i]];
            b = customMesh.verticesList[generatedVertices[i+1]];

            if(Vector3.Dot(Vector3.Cross(a - c, b - c), planeNormal) > 0)
            {
                customMesh.trianglesList.Add(customMesh.verticesList.Count - 1);
                customMesh.trianglesList.Add(generatedVertices[i]);
                customMesh.trianglesList.Add(generatedVertices[i + 1]);

                Debug.Log("created triangle " + (customMesh.verticesList.Count - 1) + " " + generatedVertices[i] + " " + generatedVertices[i + 1]);
            }
            else
            {
                customMesh.trianglesList.Add(customMesh.verticesList.Count - 1);
                customMesh.trianglesList.Add(generatedVertices[i + 1]);
                customMesh.trianglesList.Add(generatedVertices[i]);

                Debug.Log("created triangle " + (customMesh.verticesList.Count - 1) + " " + generatedVertices[i + 1] + " " + generatedVertices[i]);
            }


        }

        if (generatedVertices.Count < 3 || customMesh.verticesList.Count < 3)
            return;

        //add last triangle (loop around)
        c = customMesh.verticesList[customMesh.verticesList.Count - 1];
        a = customMesh.verticesList[customMesh.verticesList.Count - 2];
        b = customMesh.verticesList[0];

        if (Vector3.Dot(Vector3.Cross(a - c, b - c), planeNormal) > 0)
        {
            customMesh.trianglesList.Add(customMesh.verticesList.Count - 1);
            customMesh.trianglesList.Add(customMesh.verticesList.Count - 2);
            customMesh.trianglesList.Add(0);

            Debug.Log("created triangle " + (customMesh.verticesList.Count - 1) + " " + (customMesh.verticesList.Count - 2) + " " + "0");
        }
        else
        {
            customMesh.trianglesList.Add(customMesh.verticesList.Count - 1);
            customMesh.trianglesList.Add(0);
            customMesh.trianglesList.Add(customMesh.verticesList.Count - 2);

            Debug.Log("created triangle " + (customMesh.verticesList.Count - 1) + " " + "0" + " " + (customMesh.verticesList.Count - 2));
        }
    }

    private static void AddTriangleToMesh(CustomVertexDataStruct v1, CustomVertexDataStruct v2, CustomVertexDataStruct v3, CustomMeshDataStruct customMesh,
                                          Dictionary<(Vector3, Vector3, Vector2), int> vertexDataMap, List<int> generatedVertices)
    {
        //Debug.Log(v1.vertex + " L " + v2.vertex + v3.vertex);
        
        if (vertexDataMap.ContainsKey((v1.vertex, v1.normal, v1.uv)))
        {
            customMesh.trianglesList.Add(vertexDataMap[(v1.vertex, v1.normal, v1.uv)]);
        }
        else
        {
            AddVertexDataToMesh(v1, customMesh);
            customMesh.trianglesList.Add(customMesh.verticesList.Count - 1);
            if (v1.generated)
                generatedVertices.Add(customMesh.verticesList.Count - 1);

            vertexDataMap[(v1.vertex, v1.normal, v1.uv)] = customMesh.verticesList.Count - 1;
        }

        if (vertexDataMap.ContainsKey((v2.vertex, v2.normal, v2.uv)))
        {
            customMesh.trianglesList.Add(vertexDataMap[(v2.vertex, v2.normal, v2.uv)]);
        }
        else
        {
            AddVertexDataToMesh(v2, customMesh);
            customMesh.trianglesList.Add(customMesh.verticesList.Count - 1);

            if (v2.generated)
                generatedVertices.Add(customMesh.verticesList.Count - 1);

            vertexDataMap[(v2.vertex, v2.normal, v2.uv)] = customMesh.verticesList.Count - 1;
        }

        if (vertexDataMap.ContainsKey((v3.vertex, v3.normal, v3.uv)))
        {
            customMesh.trianglesList.Add(vertexDataMap[(v3.vertex, v3.normal, v3.uv)]);
        }
        else
        {
            AddVertexDataToMesh(v3, customMesh);
            customMesh.trianglesList.Add(customMesh.verticesList.Count - 1);

            if (v3.generated)
                generatedVertices.Add(customMesh.verticesList.Count - 1);

            vertexDataMap[(v3.vertex, v3.normal, v3.uv)] = customMesh.verticesList.Count - 1;
        }

        //return;

        //AddVertexDataToMesh(v1, customMesh);
        //AddVertexDataToMesh(v2, customMesh);
        //AddVertexDataToMesh(v3, customMesh);

        //customMesh.trianglesList.Add(customMesh.verticesList.Count - 3);
        //customMesh.trianglesList.Add(customMesh.verticesList.Count - 2);
        //customMesh.trianglesList.Add(customMesh.verticesList.Count - 1);

        //if (v1.generated)
        //    generatedVertices.Add(customMesh.verticesList.Count - 3);
        //if (v2.generated)
        //    generatedVertices.Add(customMesh.verticesList.Count - 2);
        //if (v3.generated)
        //    generatedVertices.Add(customMesh.verticesList.Count - 1);
    }

    private static void AddVertexDataToMesh(CustomVertexDataStruct vertexData, CustomMeshDataStruct customMesh)
    {
        customMesh.verticesList.Add(vertexData.vertex);
        customMesh.normalsList.Add(vertexData.normal);
        customMesh.uvsList.Add(vertexData.uv);
    }

    private static void Patch((int,int) vIndexSameSide, int vIndexOppositeSide, CustomMeshDataStruct customMeshDuo, CustomMeshDataStruct customMeshSolo,
                              Vector3 planeNormal, Vector3 planePoint, Mesh mainMesh, Transform meshTransform, Dictionary<(Vector3, Vector3, Vector2), int> vertexDataMapSolo,
                              Dictionary<(Vector3, Vector3, Vector2), int> vertexDataMapDuo, List<int> generatedVerticesDuo, List<int> generatedVerticesSolo)
    {

        float lerpTime1 = FindLerpOnPlane(planeNormal, planePoint, meshTransform.TransformPoint(mainMesh.vertices[vIndexSameSide.Item1]), meshTransform.TransformPoint(mainMesh.vertices[vIndexOppositeSide]));
        float lerpTime2 = FindLerpOnPlane(planeNormal, planePoint, meshTransform.TransformPoint(mainMesh.vertices[vIndexSameSide.Item2]), meshTransform.TransformPoint(mainMesh.vertices[vIndexOppositeSide]));

        Vector3 lerpedVertex1 = Vector3.Lerp(mainMesh.vertices[vIndexSameSide.Item1], mainMesh.vertices[vIndexOppositeSide], lerpTime1);
        Vector3 lerpedNormal1 = Vector3.Lerp(mainMesh.normals[vIndexSameSide.Item1], mainMesh.normals[vIndexOppositeSide], lerpTime1);
        Vector3 lerpedUVs1 = Vector3.Lerp(mainMesh.uv[vIndexSameSide.Item1], mainMesh.uv[vIndexOppositeSide], lerpTime1);

        Vector3 lerpedVertex2 = Vector3.Lerp(mainMesh.vertices[vIndexSameSide.Item2], mainMesh.vertices[vIndexOppositeSide], lerpTime2);
        Vector3 lerpedNormal2 = Vector3.Lerp(mainMesh.normals[vIndexSameSide.Item2], mainMesh.normals[vIndexOppositeSide], lerpTime2);
        Vector3 lerpedUVs2 = Vector3.Lerp(mainMesh.uv[vIndexSameSide.Item2], mainMesh.uv[vIndexOppositeSide], lerpTime2);

        CustomVertexDataStruct lerpedVertexData1 = new(lerpedVertex1, lerpedNormal1, lerpedUVs1);
        lerpedVertexData1.generated = true;
        CustomVertexDataStruct lerpedVertexData2 = new(lerpedVertex2, lerpedNormal2, lerpedUVs2);
        lerpedVertexData2.generated = true;

        //patch single, order of adding is important
        CustomVertexDataStruct soloVertexData1 = new(mainMesh.vertices[vIndexOppositeSide], mainMesh.normals[vIndexOppositeSide], mainMesh.uv[vIndexOppositeSide]);
        AddTriangleToMesh(soloVertexData1, lerpedVertexData1, lerpedVertexData2, customMeshSolo, vertexDataMapSolo, generatedVerticesSolo);

        //patch double
        CustomVertexDataStruct duoVertexData1 = new(mainMesh.vertices[vIndexSameSide.Item1], mainMesh.normals[vIndexSameSide.Item1], mainMesh.uv[vIndexSameSide.Item1]);
        CustomVertexDataStruct duoVertexData2 = new(mainMesh.vertices[vIndexSameSide.Item2], mainMesh.normals[vIndexSameSide.Item2], mainMesh.uv[vIndexSameSide.Item2]);
        AddTriangleToMesh(duoVertexData1, duoVertexData2, lerpedVertexData1, customMeshDuo, vertexDataMapDuo, generatedVerticesDuo);
        AddTriangleToMesh(lerpedVertexData1, duoVertexData2, lerpedVertexData2, customMeshDuo, vertexDataMapDuo, generatedVerticesDuo);
    }


    private static GameObject ConstructGameObjectFromMesh(Mesh createdMesh, MeshRenderer originalMeshRenderer, GameObject originalObject, string side)
    {
        GameObject g = new GameObject(originalObject.name + " " + side);
        g.AddComponent<MeshFilter>().mesh = createdMesh;
        g.AddComponent<MeshRenderer>().material = originalMeshRenderer.material;
        g.AddComponent<Sliceable>();
        g.transform.SetPositionAndRotation(originalObject.transform.position, originalObject.transform.rotation);
        g.transform.localScale = originalObject.transform.lossyScale;
        return g;
    }


    #region MathHelpers

    public static void SelectionSortAngles(List<int> generatedVertices, CustomMeshDataStruct customMesh, Vector3 center, Vector3 planeNormal)
    {
        for (int i = 0; i < generatedVertices.Count; i++)
        {
            for (int j = i + 1; j < generatedVertices.Count; j++)
            {
                if (V1HasSmallerAngle(customMesh.verticesList[generatedVertices[j]], customMesh.verticesList[generatedVertices[i]], center, planeNormal))
                {
                    int temp = generatedVertices[j];
                    generatedVertices[j] = generatedVertices[i];
                    generatedVertices[i] = temp;
                }
            }
        }
    }


    public static bool V1HasSmallerAngle(Vector3 v1, Vector3 v2, Vector3 center, Vector3 planeNormal)
    {
        // Calculate two orthogonal basis vectors in the plane
        Vector3 u = Vector3.Cross(planeNormal, Vector3.up);
        if (u.magnitude < 0.0001f) // Handle edge case where planeNormal is collinear with Vector3.up
            u = Vector3.Cross(planeNormal, Vector3.right);
        u.Normalize();
        Vector3 v = Vector3.Cross(planeNormal, u);

        // Project v1 and v2 onto the plane
        Vector3 proj1 = ProjectOntoPlane(v1, center, planeNormal);
        Vector3 proj2 = ProjectOntoPlane(v2, center, planeNormal);

        // Compute vectors from the center point
        Vector3 vec1 = proj1 - center;
        Vector3 vec2 = proj2 - center;

        // Calculate angles using atan2 in the plane's local coordinate system
        float angle1 = Mathf.Atan2(Vector3.Dot(vec1, v), Vector3.Dot(vec1, u));
        float angle2 = Mathf.Atan2(Vector3.Dot(vec2, v), Vector3.Dot(vec2, u));

        return angle1 < angle2;
    }

    // Helper function: Project a point onto a plane
    private static Vector3 ProjectOntoPlane(Vector3 point, Vector3 planePoint, Vector3 planeNormal)
    {
        Vector3 toPoint = point - planePoint;
        float distance = Vector3.Dot(toPoint, planeNormal);
        return point - planeNormal * distance;
    }

    private static bool OnNormalSideOfPlane(Vector3 planeNormal, Vector3 planePoint, Vector3 vertexPoint)
    {
        return Vector3.Dot(planeNormal, vertexPoint - planePoint) >= 0;
    }

    private static float FindLerpOnPlane(Vector3 planeNormal, Vector3 planePoint, Vector3 vertexA, Vector3 vertexB)
    {
        Vector3 edgeDirection = vertexB - vertexA;
        float distanceA = Vector3.Dot(planeNormal, vertexA - planePoint);
        float distanceB = Vector3.Dot(planeNormal, vertexB - planePoint);
        return distanceA / (distanceA - distanceB);
    }

    //mostly for debug
    public static bool ArePointsOnPlane(List<Vector3> points, Vector3 planeNormal, Vector3 planePoint, float tolerance = 1e-6f)
    {
        foreach (var point in points)
        {
            Vector3 vectorToPoint = point - planePoint;
            float distance = Vector3.Dot(planeNormal.normalized, vectorToPoint);
            if (Mathf.Abs(distance) > tolerance)
            {
                return false;
            }
        }
        return true;
    }

    #endregion


    #region DebugTools

    public static void SaveMeshToFile(Mesh mesh, string fileName)
    {
        string path = Path.Combine(Application.dataPath, fileName);
        using (StreamWriter writer = new StreamWriter(path))
        {
            writer.WriteLine("Vertices:");
            foreach (var vertex in mesh.vertices)
                writer.WriteLine(vertex);

            writer.WriteLine("\nNormals:");
            foreach (var normal in mesh.normals)
                writer.WriteLine(normal);

            writer.WriteLine("\nUVs:");
            foreach (var uv in mesh.uv)
                writer.WriteLine(uv);

            writer.WriteLine("\nTriangles:");
            for (int i = 0; i < mesh.triangles.Length; i += 3)
            {
                writer.WriteLine($"{mesh.triangles[i]}, {mesh.triangles[i + 1]}, {mesh.triangles[i + 2]}");
            }
        }
        Debug.Log($"Mesh data saved to {path}");
    }


    private static void DebuggingLog(Mesh original, Mesh left, Mesh right)
    {
        Debug.Log("original verts count: " + original.vertices.Length);
        for (int i = 0; i < original.vertices.Length; i++)
        {
            Debug.Log("original verts: " + original.vertices[i]);
        }

        Debug.Log("original tris count: " + original.vertices.Length);
        for (int i = 0; i < original.triangles.Length; i += 3)
        {
            Debug.Log("original tris: " + original.triangles[i] + " " + original.triangles[i + 1] + " " + original.triangles[i + 2]);
        }

        Debug.Log("left verts count: " + left.vertices.Length);
        for (int i = 0; i < left.vertices.Length; i++)
        {
            Debug.Log("left verts: " + left.vertices[i]);
        }

        Debug.Log("left tris count: " + left.vertices.Length);
        for (int i = 0; i < left.triangles.Length; i += 3)
        {
            Debug.Log("left tris: " + left.triangles[i] + " " + left.triangles[i + 1] + " " + left.triangles[i + 2]);
        }

        Debug.Log("right verts count: " + right.vertices.Length);
        for (int i = 0; i < right.vertices.Length; i++)
        {
            Debug.Log("right verts: " + right.vertices[i]);
        }

        Debug.Log("right tris count: " + right.vertices.Length);
        for (int i = 0; i < right.triangles.Length; i += 3)
        {
            Debug.Log("right tris: " + right.triangles[i] + " " + right.triangles[i + 1] + " " + right.triangles[i + 2]);
        }
    }

    #endregion
}
