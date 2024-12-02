using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class ObliqueCameraProjection : MonoBehaviour
{
    public Transform clipPlane; // The new near plane
    public bool DisableObliqueProjection;

    [Range(-0.1f, 0.2f)]
    public float obliqueOffset; // Offset to avoid precision issues

    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
    }

    void LateUpdate()
    {
        ApplyObliqueCameraProjection();
    }

    public void ApplyObliqueCameraProjection()
    {
        if (clipPlane == null || DisableObliqueProjection)
        {
            cam.ResetProjectionMatrix();
            return;
        }

        // Calculate plane normal and position in world space
        Vector3 dirToPlane = cam.transform.position - clipPlane.position;
        Vector3 planeNormal = clipPlane.forward;
        Vector3 planePosition = clipPlane.position;

        // Adjust plane position based on camera's relative position
        if (Vector3.Dot(planeNormal, dirToPlane) > 0)
        {
            planeNormal = -planeNormal;
        }
        planePosition -= planeNormal * obliqueOffset;

        // Apply the adjusted projection matrix
        cam.ResetProjectionMatrix();
        Vector4 plane = CameraSpacePlane(planePosition, planeNormal);
        Matrix4x4 projection = cam.projectionMatrix;
        MakeProjectionOblique(ref projection, plane);
        cam.projectionMatrix = projection;
    }

    private Vector4 CameraSpacePlane(Vector3 position, Vector3 normal)
    {
        // Transform plane position and normal to camera space
        Vector3 cameraSpacePos = cam.worldToCameraMatrix.MultiplyPoint(position);
        Vector3 cameraSpaceNormal = cam.worldToCameraMatrix.MultiplyVector(normal).normalized;

        // Calculate the plane equation in camera space
        float distance = -Vector3.Dot(cameraSpacePos, cameraSpaceNormal);
        return new Vector4(cameraSpaceNormal.x, cameraSpaceNormal.y, cameraSpaceNormal.z, distance);
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

        // Scale the plane vector to project into clip space
        Vector4 c = plane * (2.0f / Vector4.Dot(plane, q));
        projection[2] = c.x;
        projection[6] = c.y;
        projection[10] = c.z + 1.0f;
        projection[14] = c.w;
    }
}
