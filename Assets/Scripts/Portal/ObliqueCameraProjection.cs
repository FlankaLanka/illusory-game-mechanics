using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class ObliqueCameraProjection : MonoBehaviour
{
    public Transform clipPlane; // The new near plane
    public bool DisableObliqueProjection;

    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
    }

    //void OnDrawGizmos()
    //{
    //    if (clipPlane != null)
    //    {
    //        Gizmos.color = Color.green;
    //        Gizmos.DrawLine(clipPlane.position, clipPlane.position + clipPlane.forward * 2f);
    //    }
    //}

    void Update()
    {
        if (clipPlane == null || DisableObliqueProjection)
        {
            cam.ResetProjectionMatrix();
            return;
        }


        Vector3 dir = cam.transform.position - clipPlane.position;
        Vector3 planeNormal = clipPlane.forward;
        if (Vector3.Dot(clipPlane.forward, dir) < 0)
            planeNormal = -planeNormal;

        Vector4 plane = CameraSpacePlane(clipPlane.position, -planeNormal);
        cam.ResetProjectionMatrix();
        Matrix4x4 projection = cam.projectionMatrix;
        MakeProjectionOblique(ref projection, plane);
        cam.projectionMatrix = projection;
    }

    private Vector4 CameraSpacePlane(Vector3 position, Vector3 normal)
    {
        Vector3 cameraPosition = cam.worldToCameraMatrix.MultiplyPoint(position);
        Vector3 cameraNormal = cam.worldToCameraMatrix.MultiplyVector(normal).normalized;

        float distance = -Vector3.Dot(cameraPosition, cameraNormal);
        return new Vector4(cameraNormal.x, cameraNormal.y, cameraNormal.z, distance);
    }

    private void MakeProjectionOblique(ref Matrix4x4 projection, Vector4 plane)
    {
        // Calculate clip-space corner point opposite the clipping plane
        Vector4 q = new Vector4(
            (Mathf.Sign(plane.x) + projection[8]) / projection[0],
            (Mathf.Sign(plane.y) + projection[9]) / projection[5],
            -1.0f,
            (1.0f + projection[10]) / projection[14]
        );

        Vector4 c = plane * (2.0f / Vector4.Dot(plane, q));
        projection[2] = c.x;
        projection[6] = c.y;
        projection[10] = c.z + 1.0f;
        projection[14] = c.w;
    }

}
