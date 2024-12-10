using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class CaptureAndDisplay : MonoBehaviour
{
    [Header("2D visual related")]
    public Camera viewfinderCamera;
    public Renderer displayPlaneRenderer;
    public Renderer sidePlaneRenderer;

    private RenderTexture renderTexture;
    private Texture2D capturedImage;
    private Texture2D croppedTexture;
    private float brightnessMultiplier = 1.25f;

    [Header("3D visual related")]
    public CustomCameraFrustum customFrustum;
    public bool canShapeReality = false;
    public bool canTakeSnapshot = true;

    public Camera cutterCam;
    public Transform shapedRealityParent;

    private Vector3 startingPositionReality;
    private Quaternion startingRotationReality;

    void Start()
    {
        if (viewfinderCamera == null || displayPlaneRenderer == null)
        {
            Debug.LogError("Viewfinder Camera or Display Plane Renderer is not assigned.");
            return;
        }

        renderTexture = new RenderTexture(Screen.width, Screen.height, 16, RenderTextureFormat.ARGB32);
        viewfinderCamera.targetTexture = renderTexture;
    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.C))
        {
            if (!canTakeSnapshot)
                return;

            Debug.Log("TAKING PIC");

            TakePicture();

            GameObject[] output = SnapshotReality();
            foreach(GameObject obj in output)
            {
                obj.transform.parent = shapedRealityParent;
            }
            startingPositionReality = transform.InverseTransformPoint(shapedRealityParent.position);
            startingRotationReality = Quaternion.Inverse(transform.rotation) * shapedRealityParent.rotation;
        }
        else if (Input.GetKeyDown(KeyCode.V))
        {
            ShapeReality();
        }
    }

    private void OnDestroy()
    {
        if (renderTexture != null)
            renderTexture.Release();
    }


    #region 3D Related


    private void ShapeReality()
    {
        GameObject[] output = TrimReality();

        //install what we saved up before in place
        shapedRealityParent.position = transform.TransformPoint(startingPositionReality);
        shapedRealityParent.rotation = transform.rotation * startingRotationReality;

        shapedRealityParent.gameObject.SetActive(true);

        int childCount = shapedRealityParent.childCount;
        for (int i = childCount - 1; i >= 0; i--)
        {
            Transform child = shapedRealityParent.GetChild(i);
            child.parent = null;
        }

        displayPlaneRenderer.gameObject.SetActive(false);

    }

    private GameObject[] SnapshotReality()
    {
        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(cutterCam);
        List<GameObject> originalObjects = GetObjectsInCameraFrustum(cutterCam);

        for (int i = 0; i < 4; i++) //dont want to cull near/far (indices 4+5) plane
        {
            List<GameObject> leftObjects = new();

            foreach (GameObject obj in originalObjects)
            {
                GameObject left, right;
                (left, right) = MeshSlicer.Cut(obj, planes[i].normal, planes[i].normal * -planes[i].distance);

                Destroy(right);

                if (left == null || left.GetComponent<MeshFilter>().mesh.vertices.Length <= 0)
                {
                    Destroy(left);
                }
                else
                {
                    leftObjects.Add(left);
                }

                if (i > 0) //if these objects are already clones, destroy. They are clones if in iteration 1+
                    Destroy(obj);
            }
            originalObjects = leftObjects;
        }

        return originalObjects.ToArray();
    }


    private GameObject[] TrimReality()
    {
        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(cutterCam);

        List<GameObject> originalObjects = GetObjectsInCameraFrustum(cutterCam);

        for (int i = 0; i < 4; i++) //dont want to cull near/far (indices 4+5) plane
        {
            List<GameObject> leftObjects = new();
            foreach (GameObject obj in originalObjects)
            {
                GameObject left, right;
                (left,right) = MeshSlicer.Cut(obj, planes[i].normal, planes[i].normal * -planes[i].distance);

                if (left == null || left.GetComponent<MeshFilter>().mesh.vertices.Length <= 0)
                {
                    Destroy(left);
                }
                else
                {
                    leftObjects.Add(left);
                }

                Destroy(obj);
            }
            originalObjects = leftObjects;
        }

        foreach(GameObject obj in originalObjects)
        {
            Destroy(obj);
        }

        return null;
    }


    public static List<GameObject> GetObjectsInCameraFrustum(Camera camera)
    {
        // Calculate the planes of the camera's frustum
        Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(camera);

        // Get all objects in the scene
        GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();

        // List to store objects within the frustum
        List<GameObject> objectsInFrustum = new List<GameObject>();

        foreach (GameObject obj in allObjects)
        {
            // Skip inactive objects
            if (!obj.activeInHierarchy) continue;

            // Get the object's renderer bounds
            Renderer objRenderer = obj.GetComponent<Renderer>();
            if (objRenderer == null) continue; // Skip if no renderer is present

            // Check if the object's bounds intersect the frustum
            if (GeometryUtility.TestPlanesAABB(frustumPlanes, objRenderer.bounds) && obj.GetComponent<Sliceable>())
            {
                objectsInFrustum.Add(obj);
            }
        }

        return objectsInFrustum;
    }


    #endregion


    #region 2D Related


    public void TakePicture()
    {
        if (renderTexture == null || displayPlaneRenderer == null)
        {
            Debug.LogError("RenderTexture or Display Plane Renderer is not set up.");
            return;
        }

        RenderTexture.active = renderTexture;

        capturedImage = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.ARGB32, false);
        capturedImage.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        capturedImage.Apply();
        displayPlaneRenderer.material.SetTexture("_PortalRenderTexture", capturedImage);

        //how many of the planes does it take to fill the screen? About 2.5 width and 1.5 height, accurate enough for now
        croppedTexture = CropTexture(capturedImage, (int)(capturedImage.width / (2.5f)), (int)(capturedImage.height / (1.5f)), brightnessMultiplier);
        sidePlaneRenderer.material.mainTexture = croppedTexture;

        RenderTexture.active = null;

        Debug.Log("PIC TAKEN");
    }


    private Texture2D CropTexture(Texture2D originalTexture, int cropWidth, int cropHeight, float brightnessMultiplier)
    {
        if (originalTexture == null)
        {
            Debug.LogError("Original texture is null.");
            return null;
        }

        int originalWidth = originalTexture.width;
        int originalHeight = originalTexture.height;
        Debug.Log(originalWidth + " " + originalHeight);

        // Ensure crop dimensions are valid
        if (cropWidth <= 0 || cropHeight <= 0 || cropWidth > originalWidth || cropHeight > originalHeight)
        {
            Debug.LogError("Invalid crop dimensions.");
            return null;
        }

        // Calculate the cropping rectangle
        int startX = (originalWidth - cropWidth) / 2;
        int startY = (originalHeight - cropHeight) / 2;

        // Get pixel data from the original texture
        Color[] pixelData = originalTexture.GetPixels(startX, startY, cropWidth, cropHeight);

        // Create a new Texture2D for the cropped texture
        Texture2D croppedTexture = new Texture2D(cropWidth, cropHeight, originalTexture.format, false);
        croppedTexture.SetPixels(pixelData);

        // increase brightness
        Color[] pixels = croppedTexture.GetPixels();
        for (int i = 0; i < pixels.Length; i++)
        {
            Color pixel = pixels[i];
            pixel.r = Mathf.Clamp01(pixel.r * brightnessMultiplier);
            pixel.g = Mathf.Clamp01(pixel.g * brightnessMultiplier);
            pixel.b = Mathf.Clamp01(pixel.b * brightnessMultiplier);
            pixels[i] = new Color(pixel.r, pixel.g, pixel.b, pixel.a);
        }
        croppedTexture.SetPixels(pixels);



        croppedTexture.Apply();

        return croppedTexture;
    }


    #endregion

}
