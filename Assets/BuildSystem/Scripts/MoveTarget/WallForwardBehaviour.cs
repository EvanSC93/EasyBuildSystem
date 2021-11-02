using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WallForwardBehaviour : MoveTargetBehaviour
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
        m_PieceMoveType = PieceMoveType.Wall_Forward;
        gameObject.layer = LayerMask.NameToLayer("Wall_Forward");
    }

    public override void ClampPosition(PieceBehaviour piece)
    {
        Bounds worldBounds = transform.ConvertBoundsToWorld(MeshBounds);
        Vector3 worldCenter = worldBounds.center;
        float maxX = worldCenter.x + worldBounds.extents.x;
        float minX = worldCenter.x - worldBounds.extents.x;

        float maxY = worldCenter.y + worldBounds.extents.y;
        float minY = worldCenter.y - worldBounds.extents.y;

        Bounds pieceBounds = piece.MeshBoundsToWorld;
        float rangeMaxX = maxX - pieceBounds.extents.x;
        float rangeMinX = minX + pieceBounds.extents.x;

        float rangeMaxY = maxY - pieceBounds.extents.z;
        float rangeMinY = minY + pieceBounds.extents.z;

        Vector3 initPos = piece.transform.position;
        Vector3 targetPos = new Vector3(Mathf.Clamp(initPos.x, rangeMinX, rangeMaxX), Mathf.Clamp(initPos.y, rangeMinY, rangeMaxY), initPos.z);
        piece.transform.position = targetPos;
    }
    
    public override Vector3 PositionToGridPosition(float gridSize, float gridOffset, Vector3 position)
    {
        position -= Vector3.one * gridOffset;
        position /= gridSize;
            
        position = new Vector3(Mathf.Round(position.x), Mathf.Round(position.y), position.z);

        position *= gridSize;
        position += Vector3.one * gridOffset;
        return position;
    }
}
