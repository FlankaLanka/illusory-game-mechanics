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
    public Transform topLeftPositions;
    public Transform bottomRightPositions;

    public Transform shapedRealityParent;

    void Start()
    {
        if (viewfinderCamera == null || displayPlaneRenderer == null)
        {
            Debug.LogError("Viewfinder Camera or Display Plane Renderer is not assigned.");
            return;
        }

        renderTexture = new RenderTexture(Screen.width, Screen.height, 16, RenderTextureFormat.ARGB32);
        viewfinderCamera.targetTexture = renderTexture;

        Debug.Log("Viewfinder setup complete.");
    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.C))
        {
            if (!canTakeSnapshot)
                return;

            Debug.Log("TAKING PIC");
            TakePicture();
            CaptureReality();
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

    private void CaptureReality()
    {
        Plane[] cameraPlanes = GeometryUtility.CalculateFrustumPlanes(cutterCam);

        //represent as normal and point
        (Vector3, Vector3)[] planes = new (Vector3, Vector3)[6];
        planes[0] = (cameraPlanes[0].normal, topLeftPositions.position);
        planes[1] = (cameraPlanes[1].normal, bottomRightPositions.position);
        planes[2] = (cameraPlanes[2].normal, topLeftPositions.position);
        planes[3] = (cameraPlanes[3].normal, bottomRightPositions.position);
        planes[4] = (cameraPlanes[4].normal, topLeftPositions.position);
        //planePoints[5] = (cameraPlanes[5].normal, topLeftPositions.position);

        GameObject[] objectsInFrustum = GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None);

        for (int i = 0; i < 4; i++)
        {
            List<GameObject> updatedObjectsinFrustum = new();

            foreach (GameObject obj in objectsInFrustum)
            {
                GameObject left, right;
                (left, right) = MeshSlicer.Cut(obj, planes[i].Item1, planes[i].Item2);

                Destroy(right);

                if (left == null || left.GetComponent<MeshFilter>().mesh.vertices.Length <= 0)
                {
                    Destroy(left);
                }
                else
                {
                    updatedObjectsinFrustum.Add(left);
                }
            }

            objectsInFrustum = updatedObjectsinFrustum.ToArray();
        }

        foreach (GameObject obj in objectsInFrustum)
        {
            obj.transform.parent = shapedRealityParent;
        }
    }


    private void ShapeReality()
    {

    }



    public static Vector3[] GetQuadCorners(GameObject quad)
    {
        MeshFilter meshFilter = quad.GetComponent<MeshFilter>();
        if (meshFilter == null)
        {
            Debug.LogError("The GameObject does not have a MeshFilter component.");
            return null;
        }

        Mesh mesh = meshFilter.sharedMesh;
        if (mesh == null || mesh.vertices.Length < 4)
        {
            Debug.LogError("The Mesh does not have enough vertices to form a quad.");
            return null;
        }

        // Get the first 4 vertices in local space
        Vector3[] localCorners = new Vector3[4]
        {
            mesh.vertices[0],
            mesh.vertices[1],
            mesh.vertices[2],
            mesh.vertices[3]
        };

        // Convert local space vertices to world space
        Vector3[] worldCorners = new Vector3[4];
        for (int i = 0; i < 4; i++)
        {
            worldCorners[i] = quad.transform.TransformPoint(localCorners[i]);
        }

        return worldCorners;
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
