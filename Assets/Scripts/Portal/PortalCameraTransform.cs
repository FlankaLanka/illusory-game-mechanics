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
        MatchTransformRelative();
    }

    public void MatchTransformRelative()
    {
        // Calculate the relative position and rotation of A with respect to B
        Vector3 relativePosition = transform.InverseTransformPoint(playerCamTransform.position);
        Quaternion relativeRotation = Quaternion.Inverse(transform.rotation) * playerCamTransform.rotation;

        // Apply the relative position and rotation to C with respect to D
        portalCamTransform.position = otherPortal.TransformPoint(relativePosition);
        portalCamTransform.rotation = otherPortal.rotation * relativeRotation;
    }

}
