using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PortalTeleporter : MonoBehaviour
{
    public Transform red;
    public Transform blue;

    public float portalCooldown = 3f;
    private float timer = 0f;

    private void Update()
    {
        if (timer > 0)
            timer -= Time.deltaTime;
        else
            timer = 0f;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
            rb.velocity = new Vector3(0, 0, 0.1f);
    }

    private void OnTriggerExit(Collider other)
    {
        if (timer > 0)
            return;

        Debug.Log("In");

        if(other.transform == red)
        {
            Debug.Log("bobob");
            Teleport(red, blue, transform.position - red.position);
        }
        else if (other.transform == blue)
        {
            Debug.Log("zasarrarr");
            Teleport(blue, red, transform.position - blue.position);
        }

        timer = portalCooldown;
    }


    private void Teleport(Transform start, Transform end, Vector3 displacement)
    {
        transform.position = end.position + displacement;
    }
}
