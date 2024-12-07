using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrabAndResize : MonoBehaviour
{
    public Transform playerCam;
    public float maxDistanceAway = 10f;

    //regular grabbing
    private Transform grabbedObject;
    private Vector3 objectRelativePosition;
    private Quaternion objectRelativeRotation;
    private Vector3 objectStartingScale;

    //resizing
    private float resizeFactor = 0f;
    private Vector3 originalScaleFromCamera;
    private float originalDistFromCamera;
    private float distAwayFromWall;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            TryGrab();
        }
        else if (Input.GetMouseButtonDown(1))
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
                Rigidbody hit_rb = hit.transform.GetComponent<Rigidbody>();
                hit_rb.isKinematic = true;

                grabbedObject = hit.transform;

                objectRelativePosition = playerCam.InverseTransformPoint(grabbedObject.position);
                objectRelativeRotation = Quaternion.Inverse(playerCam.rotation) * grabbedObject.rotation;
                objectStartingScale = grabbedObject.lossyScale;

                originalScaleFromCamera = grabbedObject.lossyScale;
                originalDistFromCamera = (grabbedObject.position - playerCam.position).magnitude;

                Debug.Log("Object grabbed");
            }
        }
    }

    private void TryRelease()
    {
        if (grabbedObject == null)
            return;

        Debug.Log("Try releasing");

        Rigidbody hit_rb = grabbedObject.GetComponent<Rigidbody>();
        hit_rb.isKinematic = false;

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

        //everything below is about resizing
        Vector3 curDirectionUnnormalized = grabbedObject.position - playerCam.position;
        Ray ray = new Ray(playerCam.position, curDirectionUnnormalized.normalized);
        RaycastHit[] hits = Physics.RaycastAll(ray);
        distAwayFromWall = maxDistanceAway;
        for(int i = 0; i < hits.Length; i++)
        {
            if (hits[i].transform.GetComponent<Grabbable>())
                continue;

            distAwayFromWall = Mathf.Min(distAwayFromWall, hits[i].distance);
            //technically can break here since we looking for smallest only
            //break;
        }

        //potential TODO: instead of 0.9f, iteratively check best distance using Physics.OverlapBox
        resizeFactor = (distAwayFromWall * 0.9f) / originalDistFromCamera;
        grabbedObject.position = (curDirectionUnnormalized.normalized * originalDistFromCamera) * resizeFactor + playerCam.position;
        grabbedObject.localScale = originalScaleFromCamera * resizeFactor;
    }
}
