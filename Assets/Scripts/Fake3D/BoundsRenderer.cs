using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoundsRenderer : MonoBehaviour
{
    private void OnDrawGizmos()
    {
        // Get the object's bounds
        Bounds bounds = GetComponent<Renderer>().bounds;

        // Set the Gizmo color to something noticeable (e.g., green)
        Gizmos.color = Color.magenta;

        // Draw a wireframe box at the bounds' center with the extents
        Gizmos.DrawWireCube(bounds.center, bounds.size);
    }

}
