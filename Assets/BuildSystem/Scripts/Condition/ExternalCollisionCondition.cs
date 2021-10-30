using System.Linq;
using UnityEngine;

public class ExternalCollisionCondition : ConditionBehaviour
{
    public LayerMask CollisionLayer = 1 << 0;
    [Range(0f, 10f)] public float CollisionClippingTolerance = 1f;
    [Range(0f, 10f)] public float CollisionClippingSnappingTolerance = 0.99f;
    public bool RequireBuildableSurface;
    public bool CollisionIgnoreWhenSnap;

    public static bool ShowGizmos = true;

    #region Methods

    private void OnDrawGizmosSelected()
    {
        if (!ShowGizmos) return;

        if (Piece == null) return;

        Gizmos.matrix = transform.localToWorldMatrix;

        Gizmos.color = Color.cyan / 2f;
        Gizmos.DrawCube(Piece.MeshBounds.center, Piece.MeshBounds.size * CollisionClippingTolerance * 1.001f);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(Piece.MeshBounds.center, Piece.MeshBounds.size * CollisionClippingTolerance * 1.001f);

        Gizmos.color = Color.yellow / 2f;
        Gizmos.DrawCube(Piece.MeshBounds.center, Piece.MeshBounds.size * CollisionClippingSnappingTolerance * 1.001f);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(Piece.MeshBounds.center, Piece.MeshBounds.size * CollisionClippingSnappingTolerance * 1.001f);
    }

    public override bool CheckForPlacement()
    {
        bool hasBuildableSurface = false;
        bool canBePlaced = true;

        Collider[] colliders = PhysicExtension.GetNeighborsTypeByBox<Collider>(
            Piece.MeshBoundsToWorld.center,
            Piece.MeshBoundsToWorld.extents * CollisionClippingTolerance,
            transform.rotation,
            CollisionLayer).Where(x => !x.isTrigger).ToArray();

        if (RequireBuildableSurface)
        {
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null)
                    if (BuildManager.instance.IsBuildableSurface(colliders[i]))
                        hasBuildableSurface = true;
            }

            if (!hasBuildableSurface)
            {
                return false;
            }

        }
        else
            hasBuildableSurface = true;

        if (hasBuildableSurface && !CollisionIgnoreWhenSnap)
        {
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null)
                {
                    if (RequireBuildableSurface)
                    {
                        if (colliders[i].GetComponentInParent<PieceBehaviour>() == null && !BuildManager.instance.IsBuildableSurface(colliders[i]))
                        {
                            canBePlaced = false;
                        }
                    }
                    else
                    {
                        if (colliders[i].GetComponentInParent<PieceBehaviour>() == null)
                        {
                            canBePlaced = false;
                        }
                    }
                }
            }
        }

        if (canBePlaced && !CollisionIgnoreWhenSnap)
        {
            colliders = PhysicExtension.GetNeighborsTypeByBox<Collider>(Piece.MeshBoundsToWorld.center,
                Piece.MeshBoundsToWorld.extents * CollisionClippingSnappingTolerance,
                transform.rotation,
                CollisionLayer).Where(x => !x.isTrigger).ToArray();

            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null)
                {
                    if (colliders[i].GetComponentInParent<PieceBehaviour>() != null)
                    {
                        canBePlaced = false;
                    }
                }
            }
        }
        
        return hasBuildableSurface && canBePlaced;
    }

    #endregion
}