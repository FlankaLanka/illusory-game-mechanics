using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrabAndResize : MonoBehaviour
{
    public Transform playerCam;
    public Transform grabbedObject;

    public float maxDistanceAway = 10f;

    private Vector3 objectRelativePosition;
    private Quaternion objectRelativeRotation;
    private Vector3 objectStartingScale;


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


    public float resizeFactor = 0f;
    private Vector3 originalScaleFromCamera;
    private float originalDistFromCamera;

    private float distAwayFromWall;

    private void TryHold()
    {
        if (grabbedObject == null)
            return;

        Debug.Log("Try Holding");

        //normal grab
        grabbedObject.position = playerCam.TransformPoint(objectRelativePosition);
        grabbedObject.rotation = playerCam.rotation * objectRelativeRotation;

        //calculate resizeFactor, make object reach closest wall
        Vector3 curDirection = (grabbedObject.position - playerCam.position).normalized;

        Ray ray = new Ray(playerCam.position, curDirection);
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

        //distAwayFromWall = Vector3.Distance(GetReasonableFinalDistance(grabbedObject, playerCam.position, playerCam.position + distAwayFromWall * curDirection, 0.05f), playerCam.position);
        resizeFactor = distAwayFromWall / originalDistFromCamera;

        grabbedObject.position = (curDirection * originalDistFromCamera) * resizeFactor + playerCam.position;
        grabbedObject.localScale = originalScaleFromCamera * resizeFactor;
    }

    //TODO: make sure the box isn't colliding into wall
    private Vector3 GetReasonableFinalDistance(Transform t, Vector3 start, Vector3 end, float intervals)
    {
        float lerp = 1f;
        Vector3 center = Vector3.Lerp(start, end, lerp);
        while(Physics.OverlapBox(center,t.GetComponent<BoxCollider>().size, t.rotation).Length > 0 && lerp >= intervals)
        {
            lerp -= intervals;
        }

        return Vector3.Lerp(start, end, lerp);
    }
}
