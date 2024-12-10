using UnityEngine;

public class CaptureAndDisplay : MonoBehaviour
{
    public Camera viewfinderCamera;
    public Renderer displayPlaneRenderer;
    public Renderer sidePlaneRenderer;

    private RenderTexture renderTexture;

    public Texture2D capturedImage;
    public Texture2D croppedTexture;

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
        if(Input.GetKeyDown(KeyCode.B))
        {
            Debug.Log("TAKING PIC");
            TakePicture();
        }    
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

        //how many of the big plane picture does it take to fill the screen? About 2.5 width and 1.5 height, accurate enough for now
        croppedTexture = CropTexture(capturedImage, (int)(capturedImage.width / (2.5f)), (int)(capturedImage.height / (1.5f))); 
        sidePlaneRenderer.material.mainTexture = croppedTexture;

        RenderTexture.active = null;

        Debug.Log("PIC TAKEN");
    }

    private void OnDestroy()
    {
        // Release the RenderTexture when the script is destroyed
        if (renderTexture != null)
        {
            renderTexture.Release();
        }
    }

    private Texture2D CropTexture(Texture2D originalTexture, int cropWidth, int cropHeight)
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
        croppedTexture.Apply();

        return croppedTexture;
    }
}
