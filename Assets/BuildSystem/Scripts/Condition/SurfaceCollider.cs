using UnityEngine;

[AddComponentMenu("Easy Build System/Features/Colliders/Surface Collider")]
public class SurfaceCollider : MonoBehaviour
{
    private Bounds SurfaceBounds;

    private void OnDrawGizmosSelected()
    {
        if (SurfaceBounds.size == Vector3.zero)
        {
            SurfaceBounds = gameObject.GetChildsBounds();
            return;
        }

        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = Color.cyan / 2f;
        Gizmos.DrawCube(SurfaceBounds.center, SurfaceBounds.size);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(SurfaceBounds.center, SurfaceBounds.size);
    }
}