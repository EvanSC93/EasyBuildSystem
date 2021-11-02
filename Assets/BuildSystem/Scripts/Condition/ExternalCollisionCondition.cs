using System.Linq;
using UnityEngine;

public class ExternalCollisionCondition : ConditionBehaviour
{
    public LayerMask CollisionLayer = 1 << 0;
    [Range(0f, 10f)] public float CollisionClippingSnappingTolerance = 0.99f;

    private void OnDrawGizmosSelected()
    {
        PieceBehaviour piece = GetSelfPiece();
        if (piece == null) return;

        Gizmos.matrix = transform.localToWorldMatrix;

        Gizmos.color = Color.yellow / 2f;
        Gizmos.DrawCube(piece.MeshBounds.center, piece.MeshBounds.size * CollisionClippingSnappingTolerance * 1.001f);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(piece.MeshBounds.center, piece.MeshBounds.size * CollisionClippingSnappingTolerance * 1.001f);
    }

    public override bool CheckForPlacement()
    {
        bool canBePlaced = true;
        
        PieceBehaviour piece = GetSelfPiece();
        Collider[] colliders = PhysicExtension.GetNeighborsTypeByBox<Collider>(
            piece.MeshBoundsToWorld.center,
            piece.MeshBoundsToWorld.extents * CollisionClippingSnappingTolerance,
            transform.rotation,
            CollisionLayer).Where(x => !x.isTrigger).ToArray();

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider temp = colliders[i];
            if (temp != null)
            {
                if (temp.GetComponentInParent<PieceBehaviour>() != null)
                {
                    canBePlaced = false;
                    break;
                }
            }
        }
        
        return canBePlaced;
    }
}