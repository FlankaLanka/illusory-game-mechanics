using UnityEngine;

public class CustomCameraFrustum : MonoBehaviour
{
    private Plane[] frustumPlanes; // Stores the planes of the frustum

    public Camera targetCamera; // Reference to the camera you want to visualize
    public Color frustumColor = Color.cyan; // Color for visualizing the frustum

    public MeshRenderer cameraImagePlane;

    private void Awake()
    {
        RenderTexture renderTexture = new RenderTexture(Screen.width, Screen.height, 16);
        targetCamera.targetTexture = renderTexture;
        cameraImagePlane.sharedMaterial.mainTexture = renderTexture;
    }

    void OnDrawGizmos()
    {
        if (targetCamera == null) return;

        // Get the frustum planes
        frustumPlanes = GeometryUtility.CalculateFrustumPlanes(targetCamera);

        // Draw the frustum using Gizmos
        Gizmos.color = frustumColor;
        DrawFrustum(targetCamera);
    }

    /// <summary>
    /// Draws the frustum in the Scene view.
    /// </summary>
    private void DrawFrustum(Camera camera)
    {
        Matrix4x4 frustumMatrix = camera.projectionMatrix * camera.worldToCameraMatrix;
        Matrix4x4 inverseFrustum = frustumMatrix.inverse;

        Vector3[] frustumCorners = new Vector3[8];

        // Define clip space corners
        Vector3[] clipSpaceCorners = {
            new Vector3(-1, -1, -1), // Near Bottom Left
            new Vector3(1, -1, -1),  // Near Bottom Right
            new Vector3(1, 1, -1),   // Near Top Right
            new Vector3(-1, 1, -1),  // Near Top Left
            new Vector3(-1, -1, 1),  // Far Bottom Left
            new Vector3(1, -1, 1),   // Far Bottom Right
            new Vector3(1, 1, 1),    // Far Top Right
            new Vector3(-1, 1, 1)    // Far Top Left
        };

        // Transform clip space corners to world space
        for (int i = 0; i < 8; i++)
        {
            Vector4 worldCorner = inverseFrustum * new Vector4(clipSpaceCorners[i].x, clipSpaceCorners[i].y, clipSpaceCorners[i].z, 1);
            frustumCorners[i] = worldCorner / worldCorner.w;
        }

        // Draw lines connecting the corners to form the frustum
        Gizmos.DrawLine(frustumCorners[0], frustumCorners[1]); // Near Bottom
        Gizmos.DrawLine(frustumCorners[1], frustumCorners[2]); // Near Right
        Gizmos.DrawLine(frustumCorners[2], frustumCorners[3]); // Near Top
        Gizmos.DrawLine(frustumCorners[3], frustumCorners[0]); // Near Left

        Gizmos.DrawLine(frustumCorners[4], frustumCorners[5]); // Far Bottom
        Gizmos.DrawLine(frustumCorners[5], frustumCorners[6]); // Far Right
        Gizmos.DrawLine(frustumCorners[6], frustumCorners[7]); // Far Top
        Gizmos.DrawLine(frustumCorners[7], frustumCorners[4]); // Far Left

        Gizmos.DrawLine(frustumCorners[0], frustumCorners[4]); // Connect Near Bottom Left to Far Bottom Left
        Gizmos.DrawLine(frustumCorners[1], frustumCorners[5]); // Connect Near Bottom Right to Far Bottom Right
        Gizmos.DrawLine(frustumCorners[2], frustumCorners[6]); // Connect Near Top Right to Far Top Right
        Gizmos.DrawLine(frustumCorners[3], frustumCorners[7]); // Connect Near Top Left to Far Top Left
    }

    /// <summary>
    /// Returns the planes of the frustum.
    /// </summary>
    public Plane[] GetFrustumPlanes()
    {
        if (frustumPlanes == null && targetCamera != null)
        {
            frustumPlanes = GeometryUtility.CalculateFrustumPlanes(targetCamera);
        }
        return frustumPlanes;
    }
}
