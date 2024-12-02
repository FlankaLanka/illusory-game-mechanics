using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(BoxCollider))]
public class ObjectMover : MonoBehaviour
{
    public Vector3 vel;

    private Vector3 originalVel;
    private Matrix4x4 originalTransform;
    private Rigidbody rb;
    private bool isPaused = false;

    void Start()
    {
        originalVel = vel;
        originalTransform = transform.localToWorldMatrix;
        rb = GetComponent<Rigidbody>();

        Debug.Log("Press R to reset cube, T to pause cube.");
    }

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.R))
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = false;

            vel = originalVel;
            transform.SetPositionAndRotation(originalTransform.GetPosition(), originalTransform.rotation);
            return;
        }
        else if(Input.GetKeyDown(KeyCode.T))
        {
            if(isPaused)
            {
                rb.isKinematic = false;
                isPaused = false;
            }
            else
            {
                rb.isKinematic = true;
                isPaused = true;
            }
        }

        rb.velocity = vel;
    }
}
