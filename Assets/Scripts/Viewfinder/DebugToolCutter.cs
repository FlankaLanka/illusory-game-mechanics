using UnityEngine;
using System.Collections.Generic;

//This is a debug script for testing slicing
public class MeshClipper : MonoBehaviour
{
    public Vector3 planePoint;
    public Vector3 planeNormal;

    public Transform cutPlane;
    public GameObject objectToCut;
    public GameObject left, right;

    private void Update()
    {
        planeNormal = cutPlane.forward;
        planePoint = cutPlane.position;

        if(Input.GetKeyDown(KeyCode.E))
        {
            (left, right) = MeshSlicer.Cut(objectToCut, planeNormal, planePoint);
            objectToCut.SetActive(false);
        }
    }
}
