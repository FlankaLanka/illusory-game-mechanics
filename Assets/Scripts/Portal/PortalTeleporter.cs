using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

[RequireComponent(typeof(PortalScreenTransform))]
[RequireComponent(typeof(PortalCameraTransform))]
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

    public Transform otherPortal;
    public Transform thisPortalCamera;
    public Transform player;
    [SerializeField] public List<TravelerData> allTravelers;


    private void Awake()
    {
        allTravelers = new();
    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.Q))
        {
            if(allTravelers.Count > 0)
                Debug.Log(DistanceFromPointToPlane(transform.position, transform.forward, allTravelers[0].t.position));
        }


        UpdateTravelersClones(allTravelers, transform, otherPortal);

        //determine which travelers have moved past the portal plane and need to be teleported
        List<TravelerData> TravelersToRemove = new();
        foreach(TravelerData trav in allTravelers)
        {
            float curDotProduct = Vector3.Dot(trav.t.position - transform.position, transform.forward);
            if (AreOppositeSigns(curDotProduct, trav.startingDotProduct))
            {
                Teleport(trav.t, trav.t.position - transform.position);

                DeleteClone(trav, "update");

                //create a new traveler here before teleporting, dont want to rely on ontriggerenter to register a traveler
                PortalEnterEvent(trav.t, otherPortal.GetComponent<PortalTeleporter>(), this, "update");

                TravelersToRemove.Add(trav);
            }
        }
        allTravelers.RemoveAll(item => TravelersToRemove.Contains(item));
    }

    private void LateUpdate()
    {
        //possible TODO: haven't ran into errors yet, but I believe updating here might not be correct since Physics Thread is separate
        foreach (TravelerData trav in allTravelers)
        {
            DetermineProperCollisions(trav, player, this.transform, otherPortal);
        }
    }

    public void DetermineProperCollisions(TravelerData traveler, Transform player, Transform portalRef, Transform otherPortalRef)
    {
        //no need to check player collisions with itself
        if (traveler.t.tag == "Player")
            return;

        //if player and object are on opposite sides of portal
        if(AreOppositeSigns(Vector3.Dot(player.position - portalRef.position, portalRef.forward),
                            Vector3.Dot(traveler.t.position - portalRef.position, portalRef.forward)))
        {
            Physics.IgnoreCollision(player.GetComponent<Collider>(), traveler.t.GetComponent<Collider>());
        }
        else
        {
            Physics.IgnoreCollision(player.GetComponent<Collider>(), traveler.t.GetComponent<Collider>(), false);
        }

        //remember, clone's main body is actually invisible and on other side of portal, so swap the false tag
        if (AreOppositeSigns(Vector3.Dot(player.position - otherPortalRef.position, otherPortalRef.forward),
                             Vector3.Dot(traveler.clone.transform.position - otherPortalRef.position, otherPortalRef.forward)))
        {
            Physics.IgnoreCollision(player.GetComponent<Collider>(), traveler.clone.GetComponent<Collider>(), false);
        }
        else
        {
            Physics.IgnoreCollision(player.GetComponent<Collider>(), traveler.clone.GetComponent<Collider>());
        }
    }

    public void PortalEnterEvent(Transform curTraveler, PortalTeleporter portalToEnter, PortalTeleporter theOtherPortal, string here)
    {
        TravelerData newcomer = new TravelerData(curTraveler, portalToEnter.transform, portalToEnter.transform.forward);
        //if already in list, return
        foreach (TravelerData traveler in portalToEnter.allTravelers)
        {
            if (newcomer.t == traveler.t)
                return;
        }

        //fix screen orientation and camera again for edge cases
        if(curTraveler.tag == "Player")
        {
            portalToEnter.transform.GetComponent<PortalScreenTransform>().FixScreenCubeSideRelativeToPlayer();
            portalToEnter.transform.GetComponent<PortalCameraTransform>().MatchTransformRelative();
            portalToEnter.thisPortalCamera.GetComponent<ObliqueCameraProjection>().ApplyObliqueCameraProjection();
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //finally invoke events

        //only create clone if clonable, clones dont have this script so they wont clone themselves infinitely
        if (newcomer.t.GetComponent<ClonableObjectSliceMaterialSetter>() != null)
        {
            CreateClone(newcomer, portalToEnter.transform, theOtherPortal.transform, here);
        }

        //hardcode velocity change for now if called via update (teleported)
        if (newcomer.t.GetComponent<ObjectMover>() && here == "update")
        {
            ObjectMover mover = newcomer.t.GetComponent<ObjectMover>();
            Vector3 localVelocity = theOtherPortal.transform.InverseTransformDirection(mover.vel);
            mover.vel = portalToEnter.transform.TransformDirection(localVelocity);
        }

        portalToEnter.allTravelers.Add(newcomer);
    }

    private void OnTriggerEnter(Collider other)
    {
        PortalEnterEvent(other.transform, this, otherPortal.GetComponent<PortalTeleporter>(), "trigger");
    }

    private void OnTriggerExit(Collider other)
    {
        TravelerData exiter = new TravelerData(other.transform, transform, transform.forward);

        TravelerData TravelerToRemove = null;
        foreach (TravelerData traveler in allTravelers)
        {
            if (exiter.t != traveler.t)
                continue;
            TravelerToRemove = traveler;
        }

        if (TravelerToRemove != null && allTravelers.Contains(TravelerToRemove))
        {
            DeleteClone(TravelerToRemove, "trigger");
            allTravelers.Remove(TravelerToRemove);
        }
    }

    private void CreateClone(TravelerData newcomer, Transform portalToEnter, Transform clonesPortal, string here)
    {
        //Debug.Log("CALLED CREATECLONE from " + here);

        GameObject newclone = new GameObject(name: newcomer.t.name + " (portal clone)");
        newcomer.clone = newclone;

        MeshFilter cloneMeshFilter = newclone.AddComponent<MeshFilter>();
        cloneMeshFilter.mesh = newcomer.t.GetComponent<MeshFilter>().mesh;
        MeshRenderer cloneMeshRenderer = newclone.AddComponent<MeshRenderer>();
        MeshRenderer mainMeshRenderer = newcomer.t.GetComponent<MeshRenderer>();
        cloneMeshRenderer.material = new Material(mainMeshRenderer.material);
        cloneMeshRenderer.receiveShadows = mainMeshRenderer.receiveShadows;
        cloneMeshRenderer.shadowCastingMode = mainMeshRenderer.shadowCastingMode;

        Collider mainCollider = newcomer.t.GetComponent<Collider>();
        if(mainCollider is BoxCollider mainBox)
        {
            BoxCollider cloneCollider = newclone.AddComponent<BoxCollider>();
            cloneCollider.size = mainBox.size;
        }
        else
        {
            //handle other types of colliders
        }


        //get relative transforms
        MatchTransformRelative(newcomer.t, portalToEnter, newclone.transform, clonesPortal);

        //do physics
        DetermineProperCollisions(newcomer, player, portalToEnter, clonesPortal);

        //do shading
        Vector3 planeDirectionPortalA = portalToEnter.forward;
        Vector3 planeDirectionPortalB = clonesPortal.forward;
        if (Vector3.Dot(newcomer.t.position - portalToEnter.position, portalToEnter.forward) > 0)
        {
            planeDirectionPortalA = -planeDirectionPortalA;
            planeDirectionPortalB = -planeDirectionPortalB;
        }

        mainMeshRenderer.material.SetVector("_PlanePoint", new Vector4(portalToEnter.position.x, portalToEnter.position.y, portalToEnter.position.z, 0));
        mainMeshRenderer.material.SetVector("_PlaneNormal", new Vector4(planeDirectionPortalA.x, planeDirectionPortalA.y, planeDirectionPortalA.z, 0));
        mainMeshRenderer.material.SetInt("_EnableSlice", 1);

        cloneMeshRenderer.material.SetVector("_PlanePoint", new Vector4(clonesPortal.position.x, clonesPortal.position.y, clonesPortal.position.z, 0));
        cloneMeshRenderer.material.SetVector("_PlaneNormal", new Vector4(-planeDirectionPortalB.x, -planeDirectionPortalB.y, -planeDirectionPortalB.z, 0));
        cloneMeshRenderer.material.SetInt("_EnableSlice", 1);

        cloneMeshRenderer.material.SetInt("_IsClone", 1);

        //for player specifically, disable back face culling to prevent camera blocking view
        if(newcomer.t.tag == "Player")
        {
            cloneMeshRenderer.material.SetInt("_BUILTIN_CullMode", (int)UnityEngine.Rendering.CullMode.Back);
        }
    }


    private void DeleteClone(TravelerData exiter, string here)
    {
        //Debug.Log("CALLED DELETECLONE from " + here);
        if(exiter.clone == null)
        {
            Debug.Log("No clone exists for this travelling object.");
            return;
        }

        exiter.t.GetComponent<MeshRenderer>().material.SetInt("_EnableSlice", 0);
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
        Matrix4x4 travelerToPortalA = transform.worldToLocalMatrix * traveler.localToWorldMatrix;
        //world space of object if it were local to out portal
        Matrix4x4 travelerNewWorldMatrix = otherPortal.localToWorldMatrix * travelerToPortalA;
        traveler.position = travelerNewWorldMatrix.GetPosition();
        traveler.rotation = travelerNewWorldMatrix.rotation;
        traveler.localScale = travelerNewWorldMatrix.lossyScale;
        //Debug.Log("TELEPORTED");

        if (controller)
            controller.enabled = true;
    }

    bool AreOppositeSigns(float number1, float number2)
    {
        return (number1 > 0 && number2 < 0) || (number1 < 0 && number2 > 0);
    }

    private float DistanceFromPointToPlane(Vector3 planePoint, Vector3 planeNormal, Vector3 point)
    {
        Vector3 normalizedNormal = planeNormal.normalized;
        Vector3 pointToPlane = point - planePoint;
        float distance = Vector3.Dot(pointToPlane, normalizedNormal);
        return distance;
    }

    public Vector3 CalculateAdjustmentVector(Vector3 planeNormal, float distanceToPlane, float minDistance)
    {
        if (Mathf.Abs(distanceToPlane) < minDistance)
        {
            float adjustmentMagnitude = minDistance - Mathf.Abs(distanceToPlane);
            Vector3 adjustmentDirection = distanceToPlane > 0 ? planeNormal : -planeNormal;
            return adjustmentDirection * adjustmentMagnitude;
        }
        return Vector3.zero;
    }
}
