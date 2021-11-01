﻿using System.Collections.Generic;
using UnityEngine;

public class BuildManager : MonoBehaviour
{
    public static BuildManager instance;

    [SerializeField] private LayerMask m_BuildableLayer = 1 << 0;
    [SerializeField] private LayerMask m_GroundLayer;
    [SerializeField] private LayerMask m_WallLayer;
    [SerializeField] private LayerMask m_FixationLayer;
    
    [SerializeField] private SupportType m_BuildableSurfaces = SupportType.SurfaceCollider;
    [SerializeField] private StateType DefaultState = StateType.Placed;
    [SerializeField] private List<PieceBehaviour> m_Pieces = new List<PieceBehaviour>();
    [SerializeField] private List<PieceBehaviour> m_CachedParts = new List<PieceBehaviour>();

    public LayerMask BuildableLayer => m_BuildableLayer;
    public LayerMask GroundLayer => m_GroundLayer;
    public LayerMask WallLayer => m_WallLayer;
    public LayerMask FixationLayer => m_FixationLayer;
    
    public List<PieceBehaviour> CachedParts => m_CachedParts;
    public List<PieceBehaviour> Pieces => m_Pieces;

    public void OnEnable()
    {
        instance = this;
    }

    private void Awake()
    {
        instance = this;
    }

    /// <summary>
    /// This method allows to add a piece from the manager cache.
    /// </summary>
    public void AddPiece(PieceBehaviour piece)
    {
        if (piece == null)
        {
            return;
        }

        m_CachedParts.Add(piece);
    }

    /// <summary>
    /// This method allows to remove a piece from the manager cache.
    /// </summary>
    public void RemovePiece(PieceBehaviour piece)
    {
        if (piece == null)
        {
            return;
        }

        m_CachedParts.Remove(piece);
    }

    /// <summary>
    /// This method allows to get a prefab by id.
    /// </summary>
    public PieceBehaviour GetPieceById(int id)
    {
        return Pieces.Find(entry => entry.ID == id);
    }

    /// <summary>
    /// This method allows to place a piece.
    /// </summary>
    public PieceBehaviour PlacePrefab(PieceBehaviour piece, Vector3 position, Vector3 rotation, Vector3 scale, bool createGroup = true)
    {
        GameObject placedObject = Instantiate(piece.gameObject, position, Quaternion.Euler(rotation));

        placedObject.transform.localScale = scale;

        PieceBehaviour placedPart = placedObject.GetComponent<PieceBehaviour>();

        placedPart.ChangeState(DefaultState);

        BuildEvent.instance.OnPieceInstantiated.Invoke(placedPart);

        return placedPart;
    }

    /// <summary>
    /// This method allows to check if the collider is a buildable surface.
    /// </summary>
    public bool IsBuildableSurface(Collider collider)
    {
        if (m_BuildableSurfaces == SupportType.AnyCollider)
        {
            return true;
        }
        else if (m_BuildableSurfaces == SupportType.TerrainCollider)
        {
            return collider.GetComponent<TerrainCollider>();
        }
        else if (m_BuildableSurfaces == SupportType.SurfaceCollider)
        {
            return collider.GetComponent<SurfaceCollider>();
        }

        return false;
    }

}