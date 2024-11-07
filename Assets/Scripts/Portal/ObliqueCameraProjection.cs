using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class ObliqueCameraProjection : MonoBehaviour
{
    public Camera portalCam;
    public Camera playerCam;
    public Transform portal;
    public float manualoffsetMagnitude = 0f;

    public Transform tester;

    private void Awake()
    {
        portalCam = GetComponent<Camera>();
    }

    private void Update()
    {
        Vector3 curForward = portal.forward;
        if(Vector3.Dot(portal.forward, portal.position - transform.position) <= 0)
        {
            curForward = -curForward;
        }

        //SetObliqueNearClipPlane(portalCam, portal.position, curForward);

        //SetCameraNearPlane(portal.position, curForward);
    }


    public void SetCameraNearPlane(Vector3 position, Vector3 forward)
    {
        // Create a plane based on the position and forward direction
        Plane portalPlane = new Plane(forward, position);

        // Set the camera's near clip plane to this plane using oblique projection
        Matrix4x4 projectionMatrix = CalculateObliqueMatrix(portalCam, portalPlane);
        portalCam.projectionMatrix = projectionMatrix;
    }

    // Function to calculate an oblique projection matrix from a given plane
    private Matrix4x4 CalculateObliqueMatrix(Camera cam, Plane plane)
    {
        // Get the current projection matrix
        Matrix4x4 projection = cam.projectionMatrix;

        // Convert the plane into camera space
        Vector4 planeCameraSpace = CameraSpacePlane(cam, plane);

        // Modify the projection matrix with the oblique projection
        MakeOblique(ref projection, planeCameraSpace);
        return projection;
    }

    // Converts a world-space plane to a camera-space plane
    private Vector4 CameraSpacePlane(Camera cam, Plane plane)
    {
        Vector3 camPosition = cam.transform.position;
        Vector3 normal = plane.normal;
        float distance = plane.distance + Vector3.Dot(normal, camPosition);

        Vector3 normalCameraSpace = cam.worldToCameraMatrix.MultiplyVector(normal).normalized;
        float offsetCameraSpace = distance - Vector3.Dot(normalCameraSpace, camPosition);

        return new Vector4(normalCameraSpace.x, normalCameraSpace.y, normalCameraSpace.z, offsetCameraSpace);
    }

    // Updates the projection matrix for oblique clipping
    private void MakeOblique(ref Matrix4x4 projectionMatrix, Vector4 plane)
    {
        Vector4 q = projectionMatrix.inverse * new Vector4(
            Sign(plane.x),
            Sign(plane.y),
            1.0f,
            1.0f
        );

        Vector4 c = plane * (2.0F / Vector4.Dot(plane, q));
        projectionMatrix[2] = c.x - projectionMatrix[3];
        projectionMatrix[6] = c.y - projectionMatrix[7];
        projectionMatrix[10] = c.z - projectionMatrix[11];
        projectionMatrix[14] = c.w - projectionMatrix[15];
    }

    private float Sign(float value)
    {
        return (value < 0.0F) ? -1.0F : 1.0F;
    }
    /// <summary>
    /// ////////////////////////////////
    /// </summary>
    /// <param name="cam"></param>
    /// <param name="point"></param>
    /// <param name="normal"></param>
    /// <param name="minNearDistance"></param>

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

        tester.position = projectionMatrix.GetPosition();
        tester.rotation = projectionMatrix.rotation;
        tester.localScale = projectionMatrix.lossyScale;
    }

    private static Matrix4x4 CalculateObliqueMatrix(Matrix4x4 projectionMatrix, Vector4 clipPlane)
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
