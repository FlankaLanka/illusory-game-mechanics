using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrabAndResize : MonoBehaviour
{
    public Transform playerCam;
    public Transform grabbedObject;

    public float maxDistanceAway = 100f;

    private Vector3 objectRelativePosition;
    private Quaternion objectRelativeRotation;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            TryGrab();
        }
        else if (Input.GetMouseButtonUp(0))
        {
            TryRelease();
        }

        TryHold();
    }

    private void TryGrab()
    {
        if (grabbedObject != null)
            return;

        Debug.Log("Try grabbing");

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            if (hit.transform.GetComponent<Grabbable>())
            {
                hit.transform.GetComponent<Rigidbody>().isKinematic = true;

                grabbedObject = hit.transform;

                objectRelativePosition = playerCam.InverseTransformPoint(grabbedObject.position);
                objectRelativeRotation = Quaternion.Inverse(playerCam.rotation) * grabbedObject.rotation;

                Debug.Log("Object grabbed");
            }
        }
    }

    private void TryRelease()
    {
        if (grabbedObject == null)
            return;

        Debug.Log("Try releasing");

        grabbedObject.GetComponent<Rigidbody>().isKinematic = false;
        grabbedObject = null;

        Debug.Log("Object released");
    }

    private void TryHold()
    {
        if (grabbedObject == null)
            return;

        Debug.Log("Try Holding");

        //normal grab
        grabbedObject.position = playerCam.TransformPoint(objectRelativePosition);
        grabbedObject.rotation = playerCam.rotation * objectRelativeRotation;

        //perspective grab next
    }
}
