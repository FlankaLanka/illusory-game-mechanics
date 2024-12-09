using System;
using System.Collections;
using System.Collections.Generic;
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

        return (ConstructGameObjectFromMesh(leftMesh, mainMeshRenderer, originalObject, " left"), ConstructGameObjectFromMesh(rightMesh, mainMeshRenderer, originalObject, " right"));
    }


    private static void SeparateMesh(Mesh mainMesh, Mesh leftMesh, Mesh rightMesh, Transform meshTransform, Vector3 planeNormal, Vector3 planePoint)
    {
        CustomMeshDataStruct customLeftMesh = new();
        CustomMeshDataStruct customRightMesh = new();

        //used to check if vertex already in array
        Dictionary<(Vector3, Vector3, Vector2), int> vertexDataMap = new();

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
                AddTriangleToMesh(datav1, datav2, datav3, customLeftMesh, vertexDataMap);
                //Debug.Log("HERE IN LEFT");
            }
            else if (!v1NormalSide && !v2NormalSide && !v3NormalSide)
            {
                CustomVertexDataStruct datav1 = new(mainMesh.vertices[v1], mainMesh.normals[v1], mainMesh.uv[v1]);
                CustomVertexDataStruct datav2 = new(mainMesh.vertices[v2], mainMesh.normals[v2], mainMesh.uv[v2]);
                CustomVertexDataStruct datav3 = new(mainMesh.vertices[v3], mainMesh.normals[v3], mainMesh.uv[v3]);
                AddTriangleToMesh(datav1, datav2, datav3, customRightMesh, vertexDataMap);
                //Debug.Log("HERE IN RIGHT");
            }
            else
            {
                if (v1NormalSide && !v2NormalSide && !v3NormalSide)
                {
                    Patch((v2, v3), v1, customRightMesh, customLeftMesh, planeNormal, planePoint, mainMesh, meshTransform, vertexDataMap, generatedVerticesRight, generatedVerticesLeft);
                }
                else if (v1NormalSide && v2NormalSide && !v3NormalSide)
                {
                    Patch((v1, v2), v3, customLeftMesh, customRightMesh, planeNormal, planePoint, mainMesh, meshTransform, vertexDataMap, generatedVerticesLeft, generatedVerticesRight);
                }
                else if (v1NormalSide && !v2NormalSide && v3NormalSide)
                {
                    Patch((v3, v1), v2, customLeftMesh, customRightMesh, planeNormal, planePoint, mainMesh, meshTransform, vertexDataMap, generatedVerticesLeft, generatedVerticesRight);
                }
                else if (!v1NormalSide && v2NormalSide && v3NormalSide)
                {
                    Patch((v2, v3), v1, customLeftMesh, customRightMesh, planeNormal, planePoint, mainMesh, meshTransform, vertexDataMap, generatedVerticesLeft, generatedVerticesRight);
                }
                else if (!v1NormalSide && v2NormalSide && !v3NormalSide)
                {
                    Patch((v3, v1), v2, customRightMesh, customLeftMesh, planeNormal, planePoint, mainMesh, meshTransform, vertexDataMap, generatedVerticesRight, generatedVerticesLeft);
                }
                else if (!v1NormalSide && !v2NormalSide && v3NormalSide)
                {
                    Patch((v1, v2), v3, customRightMesh, customLeftMesh, planeNormal, planePoint, mainMesh, meshTransform, vertexDataMap, generatedVerticesRight, generatedVerticesLeft);
                }
                //Debug.Log("HERE IN SLICE");
            }
        }

        //patched plane here
        AddCenterPoint(customLeftMesh, generatedVerticesLeft);
        PatchPlane(customLeftMesh, generatedVerticesLeft);

        AddCenterPoint(customRightMesh, generatedVerticesRight);
        PatchPlane(customRightMesh, generatedVerticesRight);


        leftMesh.vertices = customLeftMesh.verticesList.ToArray();
        rightMesh.vertices = customRightMesh.verticesList.ToArray();

        leftMesh.normals = customLeftMesh.normalsList.ToArray();
        rightMesh.normals = customRightMesh.normalsList.ToArray();

        leftMesh.uv = customLeftMesh.uvsList.ToArray();
        rightMesh.uv = customRightMesh.uvsList.ToArray();

        leftMesh.triangles = customLeftMesh.trianglesList.ToArray();
        rightMesh.triangles = customRightMesh.trianglesList.ToArray();

        //DebuggingLog(mainMesh, leftMesh, rightMesh);
    }

    private static void AddCenterPoint(CustomMeshDataStruct customMesh, List<int> generatedVertices)
    {
        CustomVertexDataStruct output = new();

        for(int i = 0; i < generatedVertices.Count; i++)
        {
            output.vertex += customMesh.verticesList[generatedVertices[i]];
            output.normal += customMesh.normalsList[generatedVertices[i]];
            output.uv += customMesh.uvsList[generatedVertices[i]];
        }

        output.vertex /= generatedVertices.Count;
        output.normal /= generatedVertices.Count;
        output.uv /= generatedVertices.Count;

        Debug.Log("CENTER POINT: " + output.vertex);

        customMesh.verticesList.Add(output.vertex);
        customMesh.normalsList.Add(output.normal);
        customMesh.uvsList.Add(output.uv);
    }

    private static void PatchPlane(CustomMeshDataStruct customMesh, List<int> generatedVertices)
    {
        for(int i = 0; i < generatedVertices.Count - 1; i++)
        {
            customMesh.trianglesList.Add(generatedVertices[i]);
            customMesh.trianglesList.Add(customMesh.verticesList.Count - 1);

            customMesh.trianglesList.Add(generatedVertices[i + 1]);
        }
    }

    //private static void DebuggingLog(Mesh original, Mesh left, Mesh right)
    //{
    //    Debug.Log("original verts count: " + original.vertices.Length);
    //    for(int i = 0; i < original.vertices.Length; i++)
    //    {
    //        Debug.Log("original verts: " + original.vertices[i]);
    //    }

    //    Debug.Log("original tris count: " + original.vertices.Length);
    //    for (int i = 0; i < original.triangles.Length; i+=3)
    //    {
    //        Debug.Log("original tris: " + original.triangles[i] + " " + original.triangles[i+1] + " " + original.triangles[i+2]);
    //    }

    //    Debug.Log("left verts count: " + left.vertices.Length);
    //    for (int i = 0; i < left.vertices.Length; i++)
    //    {
    //        Debug.Log("left verts: " + left.vertices[i]);
    //    }

    //    Debug.Log("left tris count: " + left.vertices.Length);
    //    for (int i = 0; i < left.triangles.Length; i += 3)
    //    {
    //        Debug.Log("left tris: " + left.triangles[i] + " " + left.triangles[i + 1] + " " + left.triangles[i + 2]);
    //    }

    //    Debug.Log("right verts count: " + right.vertices.Length);
    //    for (int i = 0; i < right.vertices.Length; i++)
    //    {
    //        Debug.Log("right verts: " + right.vertices[i]);
    //    }

    //    Debug.Log("right tris count: " + right.vertices.Length);
    //    for (int i = 0; i < right.triangles.Length; i += 3)
    //    {
    //        Debug.Log("right tris: " + right.triangles[i] + " " + right.triangles[i + 1] + " " + right.triangles[i + 2]);
    //    }
    //}

    private static void AddTriangleToMesh(CustomVertexDataStruct v1, CustomVertexDataStruct v2, CustomVertexDataStruct v3, CustomMeshDataStruct customMesh,
                                          Dictionary<(Vector3, Vector3, Vector2), int> vertexDataMap)
    {
        //if(vertexDataMap.ContainsKey((v1.vertex,v1.normal,v1.uv)))
        //{
        //    customMesh.trianglesList.Add(vertexDataMap[(v1.vertex, v1.normal, v1.uv)]);
        //}
        //else
        //{
        //    AddVertexDataToMesh(v1, customMesh);
        //    customMesh.trianglesList.Add(customMesh.verticesList.Count - 1);

        //    vertexDataMap.Add((v1.vertex, v1.normal, v1.uv), customMesh.verticesList.Count - 1);
        //}

        //if (vertexDataMap.ContainsKey((v2.vertex, v2.normal, v2.uv)))
        //{
        //    customMesh.trianglesList.Add(vertexDataMap[(v2.vertex, v2.normal, v2.uv)]);
        //}
        //else
        //{
        //    AddVertexDataToMesh(v2, customMesh);
        //    customMesh.trianglesList.Add(customMesh.verticesList.Count - 1);

        //    vertexDataMap.Add((v2.vertex, v2.normal, v2.uv), customMesh.verticesList.Count - 1);
        //}

        //if (vertexDataMap.ContainsKey((v3.vertex, v3.normal, v3.uv)))
        //{
        //    customMesh.trianglesList.Add(vertexDataMap[(v3.vertex, v3.normal, v3.uv)]);
        //}
        //else
        //{
        //    AddVertexDataToMesh(v3, customMesh);
        //    customMesh.trianglesList.Add(customMesh.verticesList.Count - 1);

        //    vertexDataMap.Add((v3.vertex, v3.normal, v3.uv), customMesh.verticesList.Count - 1);
        //}

        AddVertexDataToMesh(v1, customMesh);
        AddVertexDataToMesh(v2, customMesh);
        AddVertexDataToMesh(v3, customMesh);

        customMesh.trianglesList.Add(customMesh.verticesList.Count - 3);
        customMesh.trianglesList.Add(customMesh.verticesList.Count - 2);
        customMesh.trianglesList.Add(customMesh.verticesList.Count - 1);
    }

    private static void AddVertexDataToMesh(CustomVertexDataStruct vertexData, CustomMeshDataStruct customMesh)
    {
        customMesh.verticesList.Add(vertexData.vertex);
        customMesh.normalsList.Add(vertexData.normal);
        customMesh.uvsList.Add(vertexData.uv);
    }

    private static void Patch((int,int) vIndexSameSide, int vIndexOppositeSide, CustomMeshDataStruct customMeshDuo, CustomMeshDataStruct customMeshSolo,
                              Vector3 planeNormal, Vector3 planePoint, Mesh mainMesh, Transform meshTransform, Dictionary<(Vector3, Vector3, Vector2), int> vertexDataMap,
                              List<int> generatedVerticesDuo, List<int> generatedVerticesSolo)
    {

        float lerpTime1 = FindLerpOnPlane(planeNormal, planePoint, meshTransform.TransformPoint(mainMesh.vertices[vIndexSameSide.Item1]), meshTransform.TransformPoint(mainMesh.vertices[vIndexOppositeSide]));
        float lerpTime2 = FindLerpOnPlane(planeNormal, planePoint, meshTransform.TransformPoint(mainMesh.vertices[vIndexSameSide.Item2]), meshTransform.TransformPoint(mainMesh.vertices[vIndexOppositeSide]));

        Vector3 lerpedVertex1 = Vector3.Lerp(mainMesh.vertices[vIndexSameSide.Item1], mainMesh.vertices[vIndexOppositeSide], lerpTime1);
        Vector3 lerpedNormal1 = Vector3.Lerp(mainMesh.normals[vIndexSameSide.Item1], mainMesh.normals[vIndexOppositeSide], lerpTime1);
        Vector3 lerpedUVs1 = Vector3.Lerp(mainMesh.uv[vIndexSameSide.Item1], mainMesh.uv[vIndexOppositeSide], lerpTime1);

        Vector3 lerpedVertex2 = Vector3.Lerp(mainMesh.vertices[vIndexSameSide.Item2], mainMesh.vertices[vIndexOppositeSide], lerpTime2);
        Vector3 lerpedNormal2 = Vector3.Lerp(mainMesh.normals[vIndexSameSide.Item2], mainMesh.normals[vIndexOppositeSide], lerpTime2);
        Vector3 lerpedUVs2 = Vector3.Lerp(mainMesh.uv[vIndexSameSide.Item2], mainMesh.uv[vIndexOppositeSide], lerpTime2);

        //patch single, order of adding is important
        CustomVertexDataStruct soloVertexData1 = new(mainMesh.vertices[vIndexOppositeSide], mainMesh.normals[vIndexOppositeSide], mainMesh.uv[vIndexOppositeSide]);
        CustomVertexDataStruct lerpedVertexData1 = new(lerpedVertex1, lerpedNormal1, lerpedUVs1);
        CustomVertexDataStruct lerpedVertexData2 = new(lerpedVertex2, lerpedNormal2, lerpedUVs2);
        AddTriangleToMesh(soloVertexData1, lerpedVertexData1, lerpedVertexData2, customMeshSolo, vertexDataMap);

        //add current index of vert as it will be a new triangle later
        generatedVerticesSolo.Add(customMeshSolo.verticesList.Count - 1);

        //patch double
        CustomVertexDataStruct duoVertexData1 = new(mainMesh.vertices[vIndexSameSide.Item1], mainMesh.normals[vIndexSameSide.Item1], mainMesh.uv[vIndexSameSide.Item1]);
        CustomVertexDataStruct duoVertexData2 = new(mainMesh.vertices[vIndexSameSide.Item2], mainMesh.normals[vIndexSameSide.Item2], mainMesh.uv[vIndexSameSide.Item2]);
        AddTriangleToMesh(duoVertexData1, duoVertexData2, lerpedVertexData1, customMeshDuo, vertexDataMap);
        AddTriangleToMesh(lerpedVertexData1, duoVertexData2, lerpedVertexData2, customMeshDuo, vertexDataMap);

        //add current index of vert as it will be a new triangle later
        generatedVerticesDuo.Add(customMeshDuo.verticesList.Count - 2);
        generatedVerticesDuo.Add(customMeshDuo.verticesList.Count - 1);
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

    private static float FindLerpOnPlane(Vector3 planeNormal, Vector3 planePoint, Vector3 vertexA, Vector3 vertexB)
    {
        Vector3 edgeDirection = vertexB - vertexA;
        float distanceA = Vector3.Dot(planeNormal, vertexA - planePoint);
        float distanceB = Vector3.Dot(planeNormal, vertexB - planePoint);
        return distanceA / (distanceA - distanceB);
    }

    #endregion
}
