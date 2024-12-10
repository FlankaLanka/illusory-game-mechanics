using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using static UnityEditor.Rendering.CameraUI;

public class CaptureAndDisplay : MonoBehaviour
{
    [Header("2D visual related")]
    public Camera textureCamera;
    public Renderer displayPlaneRenderer;
    public Renderer sidePlaneRenderer;

    private RenderTexture renderTexture;
    private Texture2D capturedImage;
    private Texture2D croppedTexture;
    private Texture2D whiteTexture;
    private float brightnessMultiplier = 1.25f;

    [Header("3D visual related")]
    public Camera cutterCam;
    public Transform clipboard;

    private bool canPaste = false;
    private bool canCopy = true;

    private Vector3 startingPositionClipboard;
    private Quaternion startingRotationClipboard;

    void Start()
    {
        if (textureCamera == null || displayPlaneRenderer == null)
        {
            Debug.LogError("Viewfinder Camera or Display Plane Renderer is not assigned.");
            return;
        }

        whiteTexture = CreateWhiteTexture(256, 256);
        renderTexture = new RenderTexture(Screen.width, Screen.height, 16, RenderTextureFormat.ARGB32);
        textureCamera.targetTexture = renderTexture;
    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.X))
        {
            displayPlaneRenderer.gameObject.SetActive(!displayPlaneRenderer.gameObject.activeInHierarchy);
        }
        if(Input.GetKeyDown(KeyCode.C))
        {
            if (!canCopy || !displayPlaneRenderer.gameObject.activeInHierarchy)
                return;

            //Debug.Log("TAKING PIC");

            TakePicture();
            SnapshotReality();

            canCopy = false;
            canPaste = true;
        }
        else if (Input.GetKeyDown(KeyCode.V))
        {
            if (!canPaste || !displayPlaneRenderer.gameObject.activeInHierarchy)
                return;

            ShapeReality();
            ClearImageBuffer();

            canPaste = false;
            canCopy = true;
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
        //remove everything in the way
        TrimReality();

        //install what we saved up
        clipboard.position = transform.TransformPoint(startingPositionClipboard);
        clipboard.rotation = transform.rotation * startingRotationClipboard;
        clipboard.gameObject.SetActive(true);
        ClearClipboard(clipboard);
    }


    private void SnapshotReality()
    {
        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(cutterCam);
        List<GameObject> originalObjects = GetObjectsInCameraFrustum(cutterCam, planes);

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

                if (i > 0) //if these objects are clones, destroy. They are clones in iteration 1+
                    Destroy(obj);
            }
            originalObjects = leftObjects;
        }

        //apply relevant stuff to clipboard
        foreach (GameObject obj in originalObjects)
        {
            obj.transform.parent = clipboard;
        }
        startingPositionClipboard = transform.InverseTransformPoint(clipboard.position);
        startingRotationClipboard = Quaternion.Inverse(transform.rotation) * clipboard.rotation;
    }


    private void TrimReality()
    {
        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(cutterCam);

        List<GameObject> originalObjects = GetObjectsInCameraFrustum(cutterCam, planes);

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
    }


    public static List<GameObject> GetObjectsInCameraFrustum(Camera camera, Plane[] frustumPlanes)
    {
        GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
        List<GameObject> objectsInFrustum = new List<GameObject>();

        foreach (GameObject obj in allObjects)
        {
            if (!obj.activeInHierarchy)
                continue;

            // Get the object's renderer bounds
            Renderer objRenderer = obj.GetComponent<Renderer>();
            if (objRenderer == null)
                continue;

            // Check if the object's bounds intersect the frustum
            if (GeometryUtility.TestPlanesAABB(frustumPlanes, objRenderer.bounds) && obj.GetComponent<Sliceable>())
            {
                objectsInFrustum.Add(obj);
            }
        }
        return objectsInFrustum;
    }


    public void ClearClipboard(Transform parentObject)
    {
        int childCount = parentObject.childCount;
        for (int i = childCount - 1; i >= 0; i--)
        {
            Transform child = parentObject.GetChild(i);
            child.parent = null;
        }

    }

    #endregion


    #region 2D Related


    private void ClearImageBuffer()
    {
        displayPlaneRenderer.gameObject.SetActive(false);
        displayPlaneRenderer.material.SetTexture("_PortalRenderTexture", whiteTexture);
        sidePlaneRenderer.material.mainTexture = whiteTexture;
    }

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

        //Debug.Log("PIC TAKEN");
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


    Texture2D CreateWhiteTexture(int width, int height)
    {
        Texture2D texture = new Texture2D(width, height);
        Color[] whitePixels = new Color[width * height];
        for (int i = 0; i < whitePixels.Length; i++)
        {
            whitePixels[i] = Color.white;
        }
        texture.SetPixels(whitePixels);
        texture.Apply();
        return texture;
    }

    #endregion

}
