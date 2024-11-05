using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PortalCameraTransform : MonoBehaviour
{
    public Transform playerCamTransform;
    public Transform portalCamTransform;
    public Transform otherPortalScreen;

    private Vector3 initalPortalForward;

    private void Awake()
    {
        initalPortalForward = transform.forward;
    }


    private void Update()
    {
        FacePlayer();
        MatchTransformRelative(playerCamTransform, transform, portalCamTransform, otherPortalScreen);
    }

    void MatchTransformRelative(Transform sourceA, Transform sourceB, Transform targetC, Transform targetD)
    {
        // Calculate the relative position and rotation of A with respect to B
        Vector3 relativePosition = sourceB.InverseTransformPoint(sourceA.position);
        Quaternion relativeRotation = Quaternion.Inverse(sourceB.rotation) * sourceA.rotation;

        // Apply the relative position and rotation to C with respect to D
        targetC.position = targetD.TransformPoint(relativePosition);
        targetC.rotation = targetD.rotation * relativeRotation;
    }


    private void FacePlayer()
    {
        Vector3 dirPortalToPlayer = playerCamTransform.position - transform.position;
        if(Vector3.Dot(initalPortalForward, dirPortalToPlayer) < 0)
        {
            transform.localRotation = Quaternion.Euler(0, 0, 0);
        }
        else
        {
            transform.localRotation = Quaternion.Euler(0, 180, 0);
        }
    }
}
