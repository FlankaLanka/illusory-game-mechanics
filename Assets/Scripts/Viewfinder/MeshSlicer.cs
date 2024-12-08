using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshSlicer
{
    public static void Cut(GameObject originalObject, Vector3 planeNormal, Vector3 planeDirection)
    {
        if (originalObject.GetComponent<MeshFilter>() == null || originalObject.GetComponent<MeshFilter>().mesh == null)
        {
            Debug.Log(originalObject.name + " (GameObject) has no mesh to cut.");
            return;
        }

        if(planeDirection.magnitude == 0)
        {
            Debug.Log("Plane Direction Invalid");
            return;
        }

        planeDirection = planeDirection.normalized;

        GeneratedMesh left = new();
        GeneratedMesh right = new();

    }
}
