using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

public class PieceBehaviour : MonoBehaviour
{
    public static bool ShowGizmos = true;

    [SerializeField] private int m_ID;
    
    [SerializeField] private StateType m_CurrentState;
    
    [SerializeField] private Vector3 m_PreviewOffset = new Vector3(0, 0, 0);

    [SerializeField] private Material m_DefaultPreviewMaterial;
    [SerializeField] private Color m_PreviewAllowedColor = new Color(0.0f, 1.0f, 0, 0.5f);
    [SerializeField] private Color m_PreviewDeniedColor = new Color(1.0f, 0, 0, 0.5f);

    private Bounds m_MeshBounds;
    private Material m_PreviewMaterial;
    
    private ConditionBehaviour m_Conditions;
    private List<Renderer> m_Renderers;
    private List<Collider> m_Colliders;
    private Dictionary<Renderer, Material[]> m_InitialRenderers = new Dictionary<Renderer, Material[]>();

    private Vector3 m_InitPos;
    private Quaternion m_InitRot;
    
    public Vector3 PreviewOffset => m_PreviewOffset;
    public Color PreviewAllowedColor => m_PreviewAllowedColor;
    public Color PreviewDeniedColor => m_PreviewDeniedColor;
    public List<Renderer> Renderers => m_Renderers;
    public Bounds MeshBounds => gameObject.GetChildsBounds();
    public Bounds MeshBoundsToWorld => transform.ConvertBoundsToWorld(m_MeshBounds);
    public int ID => m_ID;
    public StateType CurrentState => m_CurrentState;

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

    private void InitPosRot()
    {
        m_InitPos = transform.position;
        m_InitRot = transform.rotation;
    }

    public void ResetPosRot()
    {
        transform.position = m_InitPos;
        transform.rotation = m_InitRot;
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
            
            BuildManager.instance.AddPiece(this);
        }

        m_CurrentState = state;
       
        BuildEvent.instance.OnPieceChangedState.Invoke(this, state);
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
        if (!m_Conditions.CheckForPlacement())
        {
            return false;
        }

        return true;
    }
}