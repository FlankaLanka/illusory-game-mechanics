using UnityEngine;
using System.Collections.Generic;

public class MeshClipper : MonoBehaviour
{
    public Transform cutPlane;
    public Vector3 planePoint;
    public Vector3 planeNormal;

    public GameObject objectToCut;


    public GameObject left, right;
    public bool lol = false;

    private void Update()
    {
        planeNormal = cutPlane.forward;
        planePoint = cutPlane.position;

        if(Input.GetKeyDown(KeyCode.E))
        {
            //Destroy(left);
            //Destroy(right);
            (left, right) = MeshSlicer.Cut(objectToCut, planeNormal, planePoint);
            objectToCut.SetActive(false);
        }
    }
}
