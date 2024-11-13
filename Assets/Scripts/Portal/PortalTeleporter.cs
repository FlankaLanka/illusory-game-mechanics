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

    private void Update()
    {
        List<TravelerData> TravelersToRemove = new();
        foreach (TravelerData traveler in travellers)
        {
            float curDotProduct = Vector3.Dot(traveler.t.position - transform.position, initalForward);
            if (AreOppositeSigns(curDotProduct, traveler.startingDotProduct))
            {
                Teleport(traveler.t, traveler.t.position - transform.position);
                TravelersToRemove.Add(traveler);
            }
        }
        travellers.RemoveAll(item => TravelersToRemove.Contains(item));
    }

    private void OnTriggerEnter(Collider other)
    {
        TravelerData newcomer = new TravelerData(other.transform, transform, initalForward);

        //if already in list, return
        foreach(TravelerData traveler in travellers)
        {
            if (newcomer.t == traveler.t)
                return;
        }
        travellers.Add(newcomer);

        if(other.tag != "Player")
            CreateClone(newcomer.t);
    }

    private void OnTriggerExit(Collider other)
    {
        Debug.Log("EXITER");

        TravelerData exiter = new TravelerData(other.transform, transform, initalForward);

        TravelerData? TravelerToRemove = null;
        foreach (TravelerData traveler in travellers)
        {
            if (exiter.t != traveler.t)
                continue;
            TravelerToRemove = traveler;
        }

        if (TravelerToRemove.HasValue && travellers.Contains(TravelerToRemove.Value))
            travellers.Remove(TravelerToRemove.Value);

        if (other.tag != "Player")
            DeleteClone(exiter.t);
    }

    private void CreateClone(Transform newcomer)
    {

    }


    private void DeleteClone(Transform exiter)
    {

    }


    private void Teleport(Transform traveler, Vector3 displacement)
    {
        CharacterController controller = traveler.GetComponent<CharacterController>();
        if (controller)
            controller.enabled = false;

        //local space of object to current portal
        Matrix4x4 travelerToPortalA = transform.worldToLocalMatrix * traveler.localToWorldMatrix;
        //world space of object if it were local to out portal
        Matrix4x4 travelerNewWorldMatrix = otherPortal.localToWorldMatrix * travelerToPortalA;
        traveler.position = travelerNewWorldMatrix.GetPosition();
        traveler.rotation = travelerNewWorldMatrix.rotation;
        traveler.localScale = travelerNewWorldMatrix.lossyScale;
        Debug.Log("TELEPORTED");

        if (controller)
            controller.enabled = true;
    }

    bool AreOppositeSigns(float number1, float number2)
    {
        return (number1 > 0 && number2 < 0) || (number1 < 0 && number2 > 0);
    }

}
