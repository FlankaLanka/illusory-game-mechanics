using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Device;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

//NOTE: Make sure execution order of this script is always last, after every script under the portal parent gameobject
[RequireComponent(typeof(Camera))]
public class ObliqueCameraProjection : MonoBehaviour
{
    private Camera cam;

    public Transform clipPlane; // The new near plane
    public bool enableObliqueProjection;
    [Range(-1.15f, 1.15f)]
    public float seamOffset = 0f;

    //probably better to refactor recursive portals into another script
    [Header("Recursive Portals")]
    [Range(1, 10)]
    public int recursionLimit = 3;

    public Transform playerCam;
    public Transform thisPortal;
    public Transform otherPortal;

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    private void Update()
    {
        //rendering through script only, set recursiveLimit to 1 for base case rendering
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

            ApplyObliqueCameraProjection();


            //FixClip(otherPortal.GetComponent<PortalTeleporter>(), playerCam, seamOffset);

            //FixClip(thisPortal.GetComponent<PortalTeleporter>(), playerCam, seamOffset);

            cam.Render();

            //reset clip plane, useful for player camera's render, being lazy here instead of writing a reset function
            //FixClip(otherPortal.GetComponent<PortalTeleporter>(), playerCam, 0.01f);

            //FixClip(thisPortal.GetComponent<PortalTeleporter>(), playerCam, 0.01f);
        }

    }

    public void FixClip(PortalTeleporter teleportingManager, Transform player, float offset)
    {
        foreach(PortalTeleporter.TravelerData traveler in teleportingManager.allTravelers)
        {
            //if object has no clone, means it is not a travelling object, just ignore
            if (traveler.clone == null)
                continue;

            if (OppositeSideOfPortal(traveler.t, player, teleportingManager.transform))
            {
                if (offset != 0)
                    Debug.Log(teleportingManager.name + "OPPSITE SIDE");
                offset = -offset;
            }
            else
            {
                if (offset != 0)
                    Debug.Log(teleportingManager.name + "SAME SIDE");
            }

            MeshRenderer mainMeshRenderer = traveler.t.GetComponent<MeshRenderer>();
            Vector3 mainMeshClipNormal = mainMeshRenderer.material.GetVector("_PlaneNormal");
            mainMeshRenderer.material.SetVector("_PlanePoint", teleportingManager.transform.position + mainMeshClipNormal * offset);

            MeshRenderer cloneMeshRenderer = traveler.clone.GetComponent<MeshRenderer>();
            Vector3 cloneMeshClipNormal = cloneMeshRenderer.material.GetVector("_PlaneNormal");
            cloneMeshRenderer.material.SetVector("_PlanePoint", teleportingManager.otherPortal.transform.position + cloneMeshClipNormal * offset);
        }
    }

    public bool OppositeSideOfPortal(Transform t, Transform p, Transform portal)
    {
        return AreOppositeSigns(Vector3.Dot(portal.forward, t.position - portal.position), Vector3.Dot(portal.forward, p.position - portal.position));
    }

    bool AreOppositeSigns(float number1, float number2)
    {
        return (number1 > 0 && number2 < 0) || (number1 < 0 && number2 > 0);
    }

    public void ApplyObliqueCameraProjection()
    {
        if (clipPlane == null || !enableObliqueProjection)
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
            planeNormal = -planeNormal;

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
