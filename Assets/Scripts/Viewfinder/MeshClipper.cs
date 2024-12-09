using UnityEngine;
using System.Collections.Generic;

public class MeshClipper : MonoBehaviour
{
    public Transform cutPlane;
    public Vector3 planePoint;
    public Vector3 planeNormal;

    public bool gg = false;

    public GameObject objectToCut;

    private void Update()
    {
        planeNormal = cutPlane.forward;
        planePoint = cutPlane.position;

        if(Input.GetKeyDown(KeyCode.E))
        {
            GameObject left, right;
            (left, right) = MeshSlicer.Cut(objectToCut, planeNormal, planePoint);
        }
    }
}
