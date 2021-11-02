using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveTargetBehaviour : MonoBehaviour
{
    [SerializeField] protected PieceMoveType m_PieceMoveType;
    private Bounds m_MeshBounds;
    
    public Bounds MeshBounds => gameObject.GetChildsBounds();
    public PieceMoveType PieceMoveType => m_PieceMoveType;
    
    private void Awake()
    {
        SetLayerMask();
    }

    // Start is called before the first frame update
    void Start()
    {
       
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    protected virtual void SetLayerMask()
    {
        
    }

    public virtual void ClampPosition(PieceBehaviour piece)
    {
        
    }

    public virtual Vector3 PositionToGridPosition(float gridSize, float gridOffset, Vector3 position)
    {
        return Vector3.zero;
    }
}
