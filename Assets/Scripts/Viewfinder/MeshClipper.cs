using UnityEngine;
using System.Collections.Generic;

public class MeshClipper : MonoBehaviour
{
    public Transform cutPlane;
    public Vector3 planePoint;
    public Vector3 planeNormal;

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


    //public float planeSize = 5f;
    //private void OnDrawGizmos()
    //{
    //    // Ensure the normal is normalized
    //    Vector3 normal = planeNormal.normalized;

    //    // Calculate a basis for the plane
    //    Vector3 tangent = Vector3.Cross(normal, Vector3.right);
    //    if (tangent.magnitude < 0.001f) tangent = Vector3.Cross(normal, Vector3.forward);
    //    tangent.Normalize();
    //    Vector3 bitangent = Vector3.Cross(normal, tangent);

    //    // Draw the plane as a quad
    //    Vector3 p1 = planePoint + (tangent - bitangent) * planeSize;
    //    Vector3 p2 = planePoint + (tangent + bitangent) * planeSize;
    //    Vector3 p3 = planePoint + (-tangent + bitangent) * planeSize;
    //    Vector3 p4 = planePoint + (-tangent - bitangent) * planeSize;

    //    Gizmos.color = Color.blue;
    //    Gizmos.DrawLine(p1, p2);
    //    Gizmos.DrawLine(p2, p3);
    //    Gizmos.DrawLine(p3, p4);
    //    Gizmos.DrawLine(p4, p1);

    //    // Draw the plane normal
    //    Gizmos.color = Color.red;
    //    Gizmos.DrawLine(planePoint, planePoint + normal * planeSize);

    //    // Draw the plane point as a sphere
    //    Gizmos.color = Color.green;
    //    Gizmos.DrawSphere(planePoint, 0.1f);
    //}
}
