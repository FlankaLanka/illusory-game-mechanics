using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Device;

public class PortalCameraTransform : MonoBehaviour
{
    public Transform playerCamTransform;
    public Transform portalCamTransform;
    public Transform otherPortal;

    private void Update()
    {
        MatchTransformRelative(playerCamTransform, transform, portalCamTransform, otherPortal);
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

}
