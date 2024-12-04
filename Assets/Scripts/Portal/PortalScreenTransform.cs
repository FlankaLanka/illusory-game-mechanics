using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PortalScreenTransform : MonoBehaviour
{
    public Transform playerCamera;
    public Transform portalScreen;
    [Range(0.02f,1f)]
    public float thickness = 0.02f; //in case you see flickers due to a larger camera near plane, try increasing the thickness of the portal

    void Update()
    {
        FixScreenCubeSideRelativeToPlayer();
    }

    public void FixScreenCubeSideRelativeToPlayer()
    {
        if (Vector3.Dot(transform.forward, playerCamera.position - transform.position) > 0)
        {
            portalScreen.localPosition = new Vector3(portalScreen.localPosition.x, portalScreen.localPosition.y, -thickness / 2);
        }
        else
        {
            portalScreen.localPosition = new Vector3(portalScreen.localPosition.x, portalScreen.localPosition.y, thickness / 2);
        }
        portalScreen.localScale = new Vector3(portalScreen.localScale.x, portalScreen.localScale.y, thickness);
    }
}
