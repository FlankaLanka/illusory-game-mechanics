using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FakeCameraRender : MonoBehaviour
{
    public MeshRenderer imagePlane;
    private Camera mainCam;
    public RenderTexture rt;


    private void Awake()
    {
        mainCam = Camera.main;
        //rt = new RenderTexture(Screen.width, Screen.height, 0);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
            TakeSnapshot();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.X))
            imagePlane.gameObject.SetActive(!imagePlane.gameObject.activeInHierarchy);

        if (Input.GetKeyDown(KeyCode.C))
            GetComponent<Collider>().enabled = !GetComponent<Collider>().enabled;
    }

    private void TakeSnapshot()
    {
        RenderTexture original = mainCam.targetTexture;
        mainCam.targetTexture = rt;
        mainCam.Render();
        mainCam.targetTexture = original;

        //get the UVs of plane at the time of taking picture
        Vector2[] corners = GetNormalizedScreenCorners(mainCam, imagePlane);
        imagePlane.material.SetVector("_BL", corners[0]);
        imagePlane.material.SetVector("_BR", corners[1]);
        imagePlane.material.SetVector("_TR", corners[2]);
        imagePlane.material.SetVector("_TL", corners[3]);

        imagePlane.material.SetTexture("_Fake3DRenderTexture", rt);
        imagePlane.gameObject.SetActive(true);

        Debug.Log("Snapshot taken!");
    }


    public static Vector2[] GetNormalizedScreenCorners(Camera cam, Renderer planeRenderer)
    {
        // Get the center and axes of the plane
        Transform t = planeRenderer.transform;
        Bounds bounds = planeRenderer.bounds;

        // The center of the plane
        Vector3 center = t.position;

        // The half-width and half-height of the plane based on the plane's local scale and bounds
        Vector3 right = t.right * bounds.extents.x; // Width direction (local right)
        Vector3 up = t.forward * bounds.extents.y;  // Height direction (local up after -90 rotation)

        // 4 corners of the plane in world space
        Vector3[] worldCorners = new Vector3[4]
        {
            center - right - up, // Bottom-left
            center + right - up, // Bottom-right
            center + right + up, // Top-right
            center - right + up  // Top-left
        };

        // Convert world-space corners to screen-space (viewport space, 0–1 range)
        Vector2[] screenCorners = new Vector2[4];
        for (int i = 0; i < 4; i++)
        {
            Vector3 screenPos = cam.WorldToViewportPoint(worldCorners[i]);
            screenCorners[i] = new Vector2(screenPos.x, screenPos.y); // Normalize to 0–1 range
            Debug.Log(screenCorners[i]);
        }

        return screenCorners;
    }



}
