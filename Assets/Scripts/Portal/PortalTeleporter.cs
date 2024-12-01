using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class PortalTeleporter : MonoBehaviour
{
    [System.Serializable]
    public class TravelerData
    {
        public Transform t;
        public float startingDotProduct;

        public GameObject clone;

        public TravelerData(Transform traveler, Transform portal, Vector3 f)
        {
            t = traveler;
            startingDotProduct = Vector3.Dot(traveler.position - portal.position, f);
            clone = null;
        }
    }

    public Transform thisPortalScreen;
    public Transform otherPortal;
    [SerializeField] public List<TravelerData> allTravelers;

    private Vector3 initalForward;

    private void Awake()
    {
        initalForward = thisPortalScreen.transform.forward;
        allTravelers = new();
    }

    private void Update()
    {
        UpdateTravelersClones(allTravelers, thisPortalScreen.transform, otherPortal);


        List<TravelerData> TravelersToRemove = new();
        TravelerData[] allTravelersArray = allTravelers.ToArray();

        for (int i = 0; i < allTravelersArray.Length; i++)
        {
            float curDotProduct = Vector3.Dot(allTravelersArray[i].t.position - thisPortalScreen.transform.position, initalForward);
            if (AreOppositeSigns(curDotProduct, allTravelersArray[i].startingDotProduct))
            {
                Teleport(allTravelersArray[i].t, allTravelersArray[i].t.position - thisPortalScreen.transform.position);

                DeleteClone(allTravelersArray[i], "update"); //arrays allow ref access, was not working with list
                TravelersToRemove.Add(allTravelersArray[i]);
            }
        }


        allTravelers.RemoveAll(item => TravelersToRemove.Contains(item));
    }

    private void OnTriggerEnter(Collider other)
    {
        TravelerData newcomer = new TravelerData(other.transform, thisPortalScreen.transform, initalForward);

        //if already in list, return
        foreach(TravelerData traveler in allTravelers)
        {
            if (newcomer.t == traveler.t)
                return;
        }

        if(other.tag != "Player" && other.GetComponent<ClonableObjectSliceMaterialSetter>() != null)
        {
            CreateClone(newcomer);
        }
        allTravelers.Add(newcomer);
    }

    private void OnTriggerExit(Collider other)
    {
        Debug.Log("EXITER");

        TravelerData exiter = new TravelerData(other.transform, thisPortalScreen.transform, initalForward);

        TravelerData TravelerToRemove = null;
        foreach (TravelerData traveler in allTravelers)
        {
            if (exiter.t != traveler.t)
                continue;
            TravelerToRemove = traveler;
        }


        //there are 2 possible conditions where a traveler is removed, either they teleported to the other portal (handled in Update()),
        //or they left they way they came in, never teleporting. This case handles for latter.
        if (TravelerToRemove != null && allTravelers.Contains(TravelerToRemove))
        {
            DeleteClone(TravelerToRemove, "trigger");
            allTravelers.Remove(TravelerToRemove);
        }
    }

    private void CreateClone(TravelerData newcomer)
    {
        Debug.Log("CALLED CREATECLONE");

        GameObject newclone = new GameObject(name: newcomer.t.name + " (portal clone)");
        newcomer.clone = newclone;

        MeshFilter cloneMeshFilter = newclone.AddComponent<MeshFilter>();
        cloneMeshFilter.mesh = newcomer.t.GetComponent<MeshFilter>().mesh;
        MeshRenderer cloneMeshRenderer = newclone.AddComponent<MeshRenderer>();
        cloneMeshRenderer.material = new Material(newcomer.t.GetComponent<MeshRenderer>().material);
        cloneMeshRenderer.receiveShadows = false;

        //get relative transforms
        MatchTransformRelative(newcomer.t, thisPortalScreen.transform, newclone.transform, otherPortal);

        //set clip shader
        Vector3 planeDirection = initalForward;
        if (Vector3.Dot(newcomer.t.position - thisPortalScreen.position, initalForward) > 0)
            planeDirection = -planeDirection;

        newcomer.t.GetComponent<MeshRenderer>().material.SetVector("_PlanePoint",
            new Vector4(thisPortalScreen.position.x, thisPortalScreen.position.y, thisPortalScreen.position.z, 0));
        newcomer.t.GetComponent<MeshRenderer>().material.SetVector("_PlaneNormal",
            new Vector4(planeDirection.x, planeDirection.y, planeDirection.z, 0));
        newcomer.t.GetComponent<MeshRenderer>().material.SetInt("_EnableSlice", 1);

        cloneMeshRenderer.material.SetVector("_PlanePoint", new Vector4(otherPortal.position.x, otherPortal.position.y, otherPortal.position.z, 0));
        cloneMeshRenderer.material.SetVector("_PlaneNormal", new Vector4(-planeDirection.x, -planeDirection.y, -planeDirection.z, 0));
        cloneMeshRenderer.material.SetInt("_EnableSlice", 1);
    }


    private void DeleteClone(TravelerData exiter, string here)
    {
        Debug.Log("CALLED DELETECLONE from " + here);
        if(exiter.clone == null)
        {
            Debug.Log("No clone exists");
            return;
        }

        Destroy(exiter.clone.gameObject);
        exiter.clone = null;
    }

    private void UpdateTravelersClones(List<TravelerData> allTravelers, Transform curPortal, Transform otherPortal)
    {
        foreach(TravelerData traveler in allTravelers)
        {
            if(traveler.clone != null)
            {
                MatchTransformRelative(traveler.t, curPortal, traveler.clone.transform, otherPortal);
            }
        }
    }

    void MatchTransformRelative(Transform sourceA, Transform sourceB, Transform targetC, Transform targetD)
    {
        // Calculate the relative position and rotation of A with respect to B
        Vector3 relativePosition = sourceB.InverseTransformPoint(sourceA.position);
        Quaternion relativeRotation = Quaternion.Inverse(sourceB.rotation) * sourceA.rotation;

        // Apply the relative position and rotation to C with respect to D
        targetC.position = targetD.TransformPoint(relativePosition);
        targetC.rotation = targetD.rotation * relativeRotation;
    }

    private void Teleport(Transform traveler, Vector3 displacement)
    {
        CharacterController controller = traveler.GetComponent<CharacterController>();
        if (controller)
            controller.enabled = false;

        //local space of object to current portal
        Matrix4x4 travelerToPortalA = thisPortalScreen.transform.worldToLocalMatrix * traveler.localToWorldMatrix;
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
