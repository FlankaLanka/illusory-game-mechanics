using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class ObliqueCameraProjection : MonoBehaviour
{
    public Camera cam;
    public Transform portal;
    public float manualoffsetMagnitude = 0f;

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    private void Update()
    {
        Vector3 curForward = portal.forward;
        if(Vector3.Dot(portal.forward, portal.position - transform.position) <= 0)
        {
            curForward = -curForward;
        }

        //SetObliqueNearClipPlane(cam, portal.position, curForward);
    }

    public void SetObliqueNearClipPlane(Camera cam, Vector3 point, Vector3 normal, float minNearDistance = 0.01f)
    {
        // Convert point and normal to the camera's local space
        Vector3 camSpacePos = cam.worldToCameraMatrix.MultiplyPoint(point);
        Vector3 camSpaceNormal = cam.worldToCameraMatrix.MultiplyVector(normal).normalized;

        // Calculate the distance of the plane along the normal
        float d = -Vector3.Dot(camSpacePos, camSpaceNormal);

        // Ensure the near distance is at least the minimum threshold to avoid out-of-frustum errors
        if (d < minNearDistance)
        {
            d = minNearDistance;
        }

        // Set up the oblique projection matrix
        Vector4 clipPlaneCamSpace = new Vector4(camSpaceNormal.x, camSpaceNormal.y, camSpaceNormal.z, d);
        Matrix4x4 projectionMatrix = cam.projectionMatrix;

        // Apply the oblique projection matrix
        projectionMatrix = CalculateObliqueMatrix(projectionMatrix, clipPlaneCamSpace);
        cam.projectionMatrix = projectionMatrix;
    }

    private Matrix4x4 CalculateObliqueMatrix(Matrix4x4 projectionMatrix, Vector4 clipPlane)
    {
        // Calculate the clip-space corner point opposite the clipping plane
        Vector4 q = projectionMatrix.inverse * new Vector4(
            Mathf.Sign(clipPlane.x),
            Mathf.Sign(clipPlane.y),
            1.0f,
            1.0f
        );

        // Calculate the scaling factor
        Vector4 c = clipPlane * (2.0f / Vector4.Dot(clipPlane, q));

        // Replace the third row of the projection matrix
        projectionMatrix[2] = c.x - projectionMatrix[3];
        projectionMatrix[6] = c.y - projectionMatrix[7];
        projectionMatrix[10] = c.z - projectionMatrix[11];
        projectionMatrix[14] = c.w - projectionMatrix[15];

        return projectionMatrix;
    }
}
