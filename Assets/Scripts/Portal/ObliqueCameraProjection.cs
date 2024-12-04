using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Device;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

[RequireComponent(typeof(Camera))]
public class ObliqueCameraProjection : MonoBehaviour
{
    public Transform clipPlane; // The new near plane
    public bool DisableObliqueProjection;

    [Range(-0.1f, 0.2f)]
    public float obliqueOffset; // Offset to avoid precision issues

    private Camera cam;

    [Header("Recursive Portals")]

    [Range(0, 10)]
    public int recursionLimit = 3;

    public Transform playerCam;
    public Transform thisPortal;
    public Transform otherPortal;
    //public GameObject[] recursiveTracker; //for debug to visualize the transform of recursive viewpoints, attach cubes, for example, to this

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    private void Update()
    {
        ApplyObliqueCameraProjection();
    }

    private void LateUpdate()
    {
        CalculateRecursivePortals();
    }

    public void CalculateRecursivePortals()
    {
        Matrix4x4 originalMatrix = transform.localToWorldMatrix;
        RenderFromPerspective(recursionLimit);
        transform.SetPositionAndRotation(originalMatrix.GetColumn(3), originalMatrix.rotation);
    }

    //iteration goes from last to first
    private void RenderFromPerspective(int iterations)
    {
        Matrix4x4 localToWorldMatrix = playerCam.transform.localToWorldMatrix;
        Matrix4x4[] matrices = new Matrix4x4[iterations];

        for (int i = 0; i < recursionLimit; i++)
        {
            localToWorldMatrix = otherPortal.localToWorldMatrix * thisPortal.worldToLocalMatrix * localToWorldMatrix;
            matrices[i] = localToWorldMatrix;
        }

        for (int i = recursionLimit - 1; i >= 0; i--)
        {
            transform.SetPositionAndRotation(matrices[i].GetColumn(3), matrices[i].rotation);

            //for debug
            //if (i < recursiveTracker.Length && recursiveTracker[i] != null)
            //    recursiveTracker[i].transform.SetPositionAndRotation(matrices[i].GetColumn(3), matrices[i].rotation);

            ApplyObliqueCameraProjection();
            cam.Render();
        }

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
