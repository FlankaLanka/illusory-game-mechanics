using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(BoxCollider))]
public class ObjectMover : MonoBehaviour
{
    public Vector3 vel;
    private Matrix4x4 originalTransform;
    // Start is called before the first frame update
    void Start()
    {
        GetComponent<Rigidbody>().velocity = vel;
        originalTransform = transform.localToWorldMatrix;

        GetComponent<Rigidbody>().velocity = vel;

    }

    // Update is called once per frame
    void Update()
    {
        GetComponent<Rigidbody>().velocity = vel;

        if(Input.GetKeyDown(KeyCode.R))
        {
            transform.SetPositionAndRotation(originalTransform.GetPosition(), originalTransform.rotation);
        }
    }
}
