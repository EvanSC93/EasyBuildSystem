using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WallLeftBehaviour : MoveTargetBehaviour
{
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    protected override void SetLayerMask()
    {
        m_PieceMoveType = PieceMoveType.Wall_Left;
        gameObject.layer = LayerMask.NameToLayer("Wall_Left");
    }
    
    public override void ClampPosition(PieceBehaviour piece)
    {
        Bounds worldBounds = transform.ConvertBoundsToWorld(MeshBounds);
        Vector3 worldCenter = worldBounds.center;
        float maxZ = worldCenter.z + worldBounds.extents.z;
        float minZ = worldCenter.z - worldBounds.extents.z;

        float maxY = worldCenter.y + worldBounds.extents.y;
        float minY = worldCenter.y - worldBounds.extents.y;

        Bounds pieceBounds = piece.MeshBoundsToWorld;
        float rangeMaxZ = maxZ - pieceBounds.extents.x;
        float rangeMinZ = minZ + pieceBounds.extents.x;

        float rangeMaxY = maxY - pieceBounds.extents.z;
        float rangeMinY = minY + pieceBounds.extents.z;

        Vector3 initPos = piece.transform.position;
        Vector3 targetPos = new Vector3(initPos.x, Mathf.Clamp(initPos.y, rangeMinY, rangeMaxY), Mathf.Clamp(initPos.z, rangeMinZ, rangeMaxZ));
        piece.transform.position = targetPos;
    }

    public override Vector3 PositionToGridPosition(float gridSize, float gridOffset, Vector3 position)
    {
        position -= Vector3.one * gridOffset;
        position /= gridSize;

        position = new Vector3(position.x, Mathf.Round(position.y), Mathf.Round(position.z));

        position *= gridSize;
        position += Vector3.one * gridOffset;
        return position;
    }
}
