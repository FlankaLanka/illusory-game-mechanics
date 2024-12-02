using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PortalScreenTransform : MonoBehaviour
{
    public Mesh cube;
    public Mesh quad;

    public Transform playerCamera;
    public Transform portalScreen;
    [Range(0.02f,1f)]
    public float thickness = 0.02f;

    [Range(-1f, 1f)]
    public float offset = 0.02f; //instead of mapping screen face to portal center, make some room


    private void OnEnable()
    {
        portalScreen.GetComponent<MeshFilter>().mesh = cube;
    }

    void Update()
    {
        FixScreenCubeSideRelativeToPlayer();
    }

    public void FixScreenCubeSideRelativeToPlayer()
    {
        if (Vector3.Dot(transform.forward, playerCamera.position - transform.position) > 0)
        {
            portalScreen.localPosition = new Vector3(portalScreen.localPosition.x, portalScreen.localPosition.y, -thickness / 2 + offset);
        }
        else
        {
            portalScreen.localPosition = new Vector3(portalScreen.localPosition.x, portalScreen.localPosition.y, thickness / 2 - offset);
        }
        portalScreen.localScale = new Vector3(portalScreen.localScale.x, portalScreen.localScale.y, thickness);
    }

    private void OnDisable()
    {
        //restore default
        portalScreen.GetComponent<MeshFilter>().mesh = quad;
        portalScreen.localPosition = Vector3.zero;
        portalScreen.localScale = new Vector3(portalScreen.localScale.x, portalScreen.localScale.y, 1);
    }
}
