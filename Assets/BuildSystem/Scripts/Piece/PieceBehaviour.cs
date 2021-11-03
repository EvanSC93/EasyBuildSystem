using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

public enum PieceMoveType
{
    None,
    Ground,
    Wall_Forward,
    Wall_Left,
    Wall_Right
}

public class PieceBehaviour : MonoBehaviour
{
    [SerializeField] private string m_ModelName;
    [SerializeField] private int m_ID;
    [SerializeField] private LayerMask m_LayerMask;
    [SerializeField] private StateType m_CurrentState;

    [SerializeField] private Material m_DefaultPreviewMaterial;
    [SerializeField] private Color m_PreviewAllowedColor = new Color(0.0f, 1.0f, 0, 0.5f);
    [SerializeField] private Color m_PreviewDeniedColor = new Color(1.0f, 0, 0, 0.5f);

    [SerializeField] private GameObject m_Model;
    [SerializeField] private FixationBehaviour m_FixationBehaviour;

    private Bounds m_MeshBounds;
    private Material m_PreviewMaterial;

    private ConditionBehaviour m_Conditions;
    private List<Renderer> m_Renderers;
    private List<Collider> m_Colliders;
    private Dictionary<Renderer, Material[]> m_InitialRenderers = new Dictionary<Renderer, Material[]>();

    private Vector3 m_InitPos;
    private Quaternion m_InitRot;

    private Quaternion m_FixationRot;
    private Quaternion m_CurrentRot;
    
    public Color PreviewAllowedColor => m_PreviewAllowedColor;
    public Color PreviewDeniedColor => m_PreviewDeniedColor;
    public List<Renderer> Renderers => m_Renderers;
    public Bounds MeshBounds => gameObject.GetChildsBounds();
    public Bounds MeshBoundsToWorld => transform.ConvertBoundsToWorld(m_MeshBounds);
    public int ID => m_ID;
    public StateType CurrentState => m_CurrentState;
    public GameObject Model => m_Model;
    
    private void Awake()
    {
        m_Conditions = GetComponent<ConditionBehaviour>();

        m_Renderers = GetComponentsInChildren<Renderer>(true).ToList();

        for (int i = 0; i < m_Renderers.Count; i++)
            m_InitialRenderers.Add(m_Renderers[i], m_Renderers[i].sharedMaterials);

        m_Colliders = GetComponentsInChildren<Collider>(true).ToList();

        for (int i = 0; i < m_Colliders.Count; i++)
        {
            if (m_Colliders[i] != m_Colliders[i])
            {
                Physics.IgnoreCollision(m_Colliders[i], m_Colliders[i]);
            }
        }

        m_PreviewMaterial = new Material(m_DefaultPreviewMaterial);
        m_MeshBounds = gameObject.GetChildsBounds();
    }

    private void Update()
    {
        if (m_FixationBehaviour != null)
        {
            transform.position = m_FixationBehaviour.transform.position;
            m_Model.transform.rotation = m_FixationRot;
        }
    }

    private void InitPosRot()
    {
        m_InitPos = transform.position;
        m_InitRot = m_Model.transform.rotation;
    }

    public void ResetPosRot()
    {
        transform.position = m_InitPos;
        m_Model.transform.rotation = m_InitRot;
    }

    /// <summary>
    /// This method allows to change the piece state (Queue, Preview, Edit, Remove, Placed).
    /// </summary>
    public void ChangeState(StateType state)
    {
        if (BuildBehaviour.instance == null)
        {
            return;
        }

        if (m_CurrentState == state)
        {
            return;
        }

        if (state == StateType.Preview)
        {
            InitPosRot();

            gameObject.ChangeAllMaterialsInChildren(m_Renderers.ToArray(), m_PreviewMaterial);
            gameObject.ChangeAllMaterialsColorInChildren(m_Renderers.ToArray(),
                BuildBehaviour.instance.AllowPlacement ? m_PreviewAllowedColor : m_PreviewDeniedColor);

            EnableAllColliders(false);

            BuildManager.instance.RemovePiece(this);
        }
        else if (state == StateType.Placed)
        {
            gameObject.ChangeAllMaterialsInChildren(m_Renderers.ToArray(), m_InitialRenderers);

            EnableAllColliders(true);
            UpdateName();
            BuildManager.instance.AddPiece(this);
        }

        m_CurrentState = state;

        BuildEvent.instance.OnPieceChangedState.Invoke(this, state);
    }

    private void UpdateName()
    {
        gameObject.name = m_ModelName + "_" + BuildManager.instance.CachedParts.Count;
    }

    /// <summary>
    /// This method allows to enable all the colliders of this piece.
    /// </summary>
    public void EnableAllColliders(bool value)
    {
        for (int i = 0; i < m_Colliders.Count; i++)
        {
            m_Colliders[i].enabled = value;
        }
    }

    /// <summary>
    /// This method allows check all the external condition(s) before placement.
    /// </summary>
    public bool CheckExternalPlacementConditions()
    {
        return m_Conditions.CheckForPlacement();
    }

    public LayerMask GetLayerMask()
    {
        return m_LayerMask;
    }

    public void RotateModel()
    {
        if (m_FixationBehaviour == null)
        {
            m_Model.transform.Rotate(transform.up, 90, Space.World);
            m_CurrentRot = m_Model.transform.localRotation;
        }
        else
        {
            m_FixationRot *= Quaternion.Euler(0, 90f, 0);
        }
        m_MeshBounds = gameObject.GetChildsBounds();
    }

    public void SetFixationBehaviour(FixationBehaviour value)
    {
        m_FixationBehaviour = value;
        
        if (value == null)
        {
            m_Model.transform.localRotation = m_CurrentRot;
            m_FixationRot = Quaternion.identity;
        }
        else
        {
            m_FixationRot = value.transform.rotation;
        }
    }
}