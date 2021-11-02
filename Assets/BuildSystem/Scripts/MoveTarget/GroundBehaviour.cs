using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GroundBehaviour : MoveTargetBehaviour
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
        m_PieceMoveType = PieceMoveType.Ground;
        gameObject.layer = LayerMask.NameToLayer("Ground");
    }

    public override void ClampPosition(PieceBehaviour piece)
    {
        Bounds worldBounds = transform.ConvertBoundsToWorld(MeshBounds);
        Vector3 worldCenter = worldBounds.center;
        float maxX = worldCenter.x + worldBounds.extents.x;
        float minX = worldCenter.x - worldBounds.extents.x;

        float maxZ = worldCenter.z + worldBounds.extents.z;
        float minZ = worldCenter.z - worldBounds.extents.z;

        Bounds pieceBounds = piece.MeshBoundsToWorld;
        float rangeMaxX = maxX - pieceBounds.extents.x;
        float rangeMinX = minX + pieceBounds.extents.x;

        float rangeMaxZ = maxZ - pieceBounds.extents.z;
        float rangeMinZ = minZ + pieceBounds.extents.z;

        Vector3 initPos = piece.transform.position;
        Vector3 targetPos = new Vector3(Mathf.Clamp(initPos.x, rangeMinX, rangeMaxX), initPos.y, Mathf.Clamp(initPos.z, rangeMinZ, rangeMaxZ));
        piece.transform.position = targetPos;
    }
    
    public override Vector3 PositionToGridPosition(float gridSize, float gridOffset, Vector3 position)
    {
        position -= Vector3.one * gridOffset;
        position /= gridSize;
            
        position = new Vector3(Mathf.Round(position.x), position.y, Mathf.Round(position.z));

        position *= gridSize;
        position += Vector3.one * gridOffset;
        return position;
    }
}
