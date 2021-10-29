using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

public class PieceBehaviour : MonoBehaviour
{
    public static bool ShowGizmos = true;

    [SerializeField] private int m_ID;
    
    [SerializeField] private StateType m_CurrentState = StateType.Placed;
    
    [SerializeField] private bool m_RotateAccordingSlope;
    [SerializeField] private bool m_PreviewUseColorLerpTime = false;
    
    [SerializeField] private float m_PreviewColorLerpTime = 15f;

    [SerializeField] private Vector3 m_RotationAxis = Vector3.up * 90;
    [SerializeField] private Vector3 m_PreviewOffset = new Vector3(0, 0, 0);

    [SerializeField] private Material m_DefaultPreviewMaterial;
    [SerializeField] private Color m_PreviewAllowedColor = new Color(0.0f, 1.0f, 0, 0.5f);
    [SerializeField] private Color m_PreviewDeniedColor = new Color(1.0f, 0, 0, 0.5f);

    private Bounds m_MeshBounds;
    private Material m_PreviewMaterial;
    
    private List<ConditionBehaviour> m_Conditions = new List<ConditionBehaviour>();
    private List<Renderer> m_Renderers;
    private List<Collider> m_Colliders;
    private Dictionary<Renderer, Material[]> m_InitialRenderers = new Dictionary<Renderer, Material[]>();
    
    public bool RotateAccordingSlope => m_RotateAccordingSlope;
    public bool PreviewUseColorLerpTime => m_PreviewUseColorLerpTime;
    public float PreviewColorLerpTime => m_PreviewColorLerpTime;
    public Vector3 RotationAxis => m_RotationAxis;
    public Vector3 PreviewOffset => m_PreviewOffset;
    public Color PreviewAllowedColor => m_PreviewAllowedColor;
    public Color PreviewDeniedColor => m_PreviewDeniedColor;
    public List<Renderer> Renderers => m_Renderers;
    public Bounds MeshBounds => gameObject.GetChildsBounds();
    public Bounds MeshBoundsToWorld => transform.ConvertBoundsToWorld(m_MeshBounds);
    public int ID => m_ID;
    public StateType CurrentState => m_CurrentState;
    public StateType LastState { get; set; }

    #region Methods

    private void Awake()
    {
        m_Conditions.AddRange(GetComponents<ConditionBehaviour>());

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

    private void Start()
    {
        if (m_CurrentState != StateType.Preview)
        {
            BuildManager.instance.AddPiece(this);
        }
    }

    private void Reset()
    {
        Debug.LogError("Reset");
        if (m_MeshBounds.size == Vector3.zero)
        {
            
     
        }
    }

    private void Update()
    {

    }

    private void OnDestroy()
    {
        if (m_CurrentState == StateType.Preview)
        {
            return;
        }

        BuildManager.instance.RemovePiece(this);
    }

    private void OnDrawGizmosSelected()
    {
        if (!ShowGizmos) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.1f);
        Gizmos.color = Color.cyan / 2;
        Gizmos.DrawCube(transform.position, Vector3.one * 0.1f);

        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(m_MeshBounds.center, m_MeshBounds.size * 1.001f);
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

        LastState = m_CurrentState;

        if (state == StateType.Queue)
        {
            gameObject.ChangeAllMaterialsInChildren(m_Renderers.ToArray(), m_PreviewMaterial);
            gameObject.ChangeAllMaterialsColorInChildren(m_Renderers.ToArray(), Color.clear);

            EnableAllColliders();
        }
        else if (state == StateType.Preview)
        {
            gameObject.ChangeAllMaterialsInChildren(m_Renderers.ToArray(), m_PreviewMaterial);
            gameObject.ChangeAllMaterialsColorInChildren(m_Renderers.ToArray(),
                BuildBehaviour.instance.AllowPlacement ? m_PreviewAllowedColor : m_PreviewDeniedColor);

            DisableAllColliders();
        }
        else if (state == StateType.Edit)
        {
            // gameObject.ChangeAllMaterialsInChildren(m_Renderers.ToArray(), m_PreviewMaterial);
            // gameObject.ChangeAllMaterialsColorInChildren(m_Renderers.ToArray(),
            //     BuildBehaviour.instance.AllowEdition ? m_PreviewAllowedColor : m_PreviewDeniedColor);
            
            SetRendersEnable(false);
            
            EnableAllColliders();
        }
        else if (state == StateType.Remove)
        {
            gameObject.ChangeAllMaterialsInChildren(m_Renderers.ToArray(), m_PreviewMaterial);
            gameObject.ChangeAllMaterialsColorInChildren(m_Renderers.ToArray(), m_PreviewDeniedColor);

            EnableAllColliders();
        }
        else if (state == StateType.Placed)
        {
            gameObject.ChangeAllMaterialsInChildren(m_Renderers.ToArray(), m_InitialRenderers);

            SetRendersEnable(true);
            
            EnableAllColliders();
        }

        m_CurrentState = state;
       
        BuildEvent.instance.OnPieceChangedState.Invoke(this, state);
    }

    public void SetRendersEnable(bool value)
    {
        foreach (var VARIABLE in m_Renderers)
        {
            VARIABLE.enabled = value;
        }
    }

    /// <summary>
    /// This method allows to enable all the colliders of this piece.
    /// </summary>
    public void EnableAllColliders()
    {
        for (int i = 0; i < m_Colliders.Count; i++)
        {
            m_Colliders[i].enabled = true;
        }
    }

    /// <summary>
    /// This method allows to disable all the colliders of this piece.
    /// </summary>
    public void DisableAllColliders()
    {
        for (int i = 0; i < m_Colliders.Count; i++)
        {
            m_Colliders[i].enabled = false;
        }
    }

    /// <summary>
    /// This method allows to enable all the colliders of this piece.
    /// </summary>
    public void EnableAllCollidersTrigger()
    {
        for (int i = 0; i < m_Colliders.Count; i++)
        {
            m_Colliders[i].isTrigger = true;
        }
    }

    /// <summary>
    /// This method allows to disable all the colliders of this piece.
    /// </summary>
    public void DisableAllCollidersTrigger()
    {
        for (int i = 0; i < m_Colliders.Count; i++)
        {
            m_Colliders[i].isTrigger = false;
        }
    }

    /// <summary>
    /// This method allows check all the external condition(s) before placement.
    /// </summary>
    public bool CheckExternalPlacementConditions(bool isCreat = false)
    {
        for (int i = 0; i < m_Conditions.Count; i++)
        {
            if (!m_Conditions[i].CheckForPlacement(isCreat))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// This method allows check all the external condition(s) before destruction.
    /// </summary>
    public bool CheckExternalDestructionConditions()
    {
        for (int i = 0; i < m_Conditions.Count; i++)
        {
            if (!m_Conditions[i].CheckForDestruction())
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// This method allow check all the external condition(s) before edition.
    /// </summary>
    public bool CheckExternalEditionConditions()
    {
        for (int i = 0; i < m_Conditions.Count; i++)
        {
            if (!m_Conditions[i].CheckForEdition())
            {
                return false;
            }
        }

        return true;
    }

    #endregion Methods
}