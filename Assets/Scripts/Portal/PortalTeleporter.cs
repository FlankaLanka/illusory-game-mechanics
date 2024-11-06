using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class PortalTeleporter : MonoBehaviour
{
    [System.Serializable]
    public struct TravelerData
    {
        public Transform t;
        public float startingDotProduct;

        public TravelerData(Transform traveler, Transform portal, Vector3 f)
        {
            t = traveler;
            startingDotProduct = Vector3.Dot(traveler.position - portal.position, f);
        }
    }

    public Transform otherPortal;
    [SerializeField] public List<TravelerData> travellers;


    private Vector3 initalForward;

    private void Awake()
    {
        initalForward = transform.forward;
        travellers = new();
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("GOIJNG");
        TravelerData newcomer = new TravelerData(other.transform, transform, initalForward);

        //if already in list, return
        foreach(TravelerData traveler in travellers)
        {
            if (newcomer.t == traveler.t)
                return;
        }

        travellers.Add(newcomer);
    }

    private void OnTriggerExit(Collider other)
    {
        Debug.Log("EXITEREIER");

        TravelerData exiter = new TravelerData(other.transform, transform, initalForward);
        TravelerData? TravelerToRemove = null;
        foreach (TravelerData traveler in travellers)
        {
            if (exiter.t != traveler.t)
                continue;

            Debug.Log(exiter.startingDotProduct);
            Debug.Log(traveler.startingDotProduct);

            if (AreOppositeSigns(exiter.startingDotProduct, traveler.startingDotProduct))
            {
                Debug.Log("teleporting");
                Teleport(exiter.t, otherPortal.position, exiter.t.position - transform.position);
            }

            TravelerToRemove = traveler;
        }

        if (TravelerToRemove.HasValue)
            travellers.Remove(TravelerToRemove.Value);
    }


    private void Teleport(Transform traveler, Vector3 endPos, Vector3 displacement)
    {
        traveler.position = endPos + displacement * 1.05f;
        Debug.Log("TELEPORTED0");
    }

    bool AreOppositeSigns(float number1, float number2)
    {
        return (number1 > 0 && number2 < 0) || (number1 < 0 && number2 > 0);
    }

}
