using System;
using UnityEngine;

public class BuildBehaviour : MonoBehaviour
{
    public static BuildBehaviour instance;

    [SerializeField] private MovementType m_PreviewMovementType;

    [SerializeField] private BuildModeType m_CurrentModeType;
    [SerializeField] private BuildModeType m_LastModeType;
    
    [SerializeField] private float m_DetectionDistance = 10f;
    [SerializeField] private float m_PreviewGridSize = 1.0f;
    [SerializeField] private float m_PreviewGridOffset;
    [SerializeField] private float m_PreviewSmoothTime = 5.0f;
    [SerializeField] private bool m_PreviewMovementOnlyAllowed;

    private PieceBehaviour m_SelectedPrefab;
    [SerializeField] private PieceBehaviour m_CurrentPreview;
    [SerializeField] private PieceBehaviour m_CurrentEditionPreview;

    private Vector3 m_CurrentRotationOffset;

    private bool m_AllowPlacement;
    private bool m_AllowEdition;

    private Camera m_Camera;
    private Transform m_CameraTrans;
    private Vector3 m_LastAllowedPoint;
    private Vector3 m_LastPoint;

    public BuildModeType CurrentModeType => m_CurrentModeType;

    public virtual Ray GetRay => m_Camera.ScreenPointToRay(Input.mousePosition);

    public float DetectionDistance => m_DetectionDistance;
    
    public bool AllowPlacement => m_AllowPlacement;
    public bool AllowEdition => m_AllowEdition;

    public virtual void Awake()
    {
        instance = this;
    }

    public virtual void Start()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        m_Camera = Camera.main;
        m_CameraTrans = m_Camera.gameObject.transform;

        if (m_Camera == null)
        {
            Debug.LogWarning("<b>Easy Build System</b> : The Builder Behaviour require a camera!");
        }
    }

    #region 放置预览相关代码

    /// <summary>
    /// 更新预览物体
    /// </summary>
    public void UpdatePreview()
    {
        UpdateFreeMovement();

        m_CurrentPreview.gameObject.ChangeAllMaterialsColorInChildren(m_CurrentPreview.Renderers.ToArray(),
            CheckPlacementConditions() ? m_CurrentPreview.PreviewAllowedColor : m_CurrentPreview.PreviewDeniedColor);
    }

    /// <summary>
    /// 检查是否符合放置条件
    /// </summary>
    /// <returns></returns>
    public bool CheckPlacementConditions(bool isCreat = false)
    {
        if (m_CurrentPreview == null)
        {
            return false;
        }

        if (Vector3.Distance(m_CameraTrans.position, m_CurrentPreview.transform.position) > m_DetectionDistance)
        {
            return false;
        }

        if (!m_CurrentPreview.CheckExternalPlacementConditions(isCreat))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// 旋转预览物体
    /// </summary>
    /// <param name="rotateAxis"></param>
    public void RotatePreview(Vector3 rotateAxis)
    {
        if (m_CurrentPreview == null)
        {
            return;
        }

        m_CurrentRotationOffset += rotateAxis;
    }

    /// <summary>
    /// 无SocketBehaviour(吸附功能脚本)时，自由移动
    /// </summary>
    public void UpdateFreeMovement()
    {
        if (m_CurrentPreview == null)
        {
            return;
        }

        m_CurrentPreview.SetRendersEnable(true);
        
        float distance = m_DetectionDistance;

        Physics.Raycast(GetRay, out RaycastHit hit, distance, BuildManager.instance.BuildableLayer);

        if (hit.collider != null)
        {
            Vector3 targetPoint = hit.point + m_CurrentPreview.PreviewOffset;
            Vector3 nextPoint = targetPoint;

            if (m_PreviewMovementType == MovementType.Smooth)
                nextPoint = Vector3.Lerp(m_CurrentPreview.transform.position, nextPoint, m_PreviewSmoothTime * Time.deltaTime);
            else if (m_PreviewMovementType == MovementType.Grid)
                nextPoint = MathExtension.PositionToGridPosition(m_PreviewGridSize, m_PreviewGridOffset, nextPoint);

            if (m_PreviewMovementOnlyAllowed)
            {
                m_CurrentPreview.transform.position = nextPoint;

                if (m_CurrentPreview.CheckExternalPlacementConditions() && CheckPlacementConditions())
                {
                    m_LastAllowedPoint = m_CurrentPreview.transform.position;
                }
                else
                {
                    m_CurrentPreview.transform.position = m_LastAllowedPoint;
                }
            }
            else
                m_CurrentPreview.transform.position = nextPoint;

            m_CurrentPreview.transform.rotation = Quaternion.Euler(m_CurrentRotationOffset);

            m_LastPoint = new Vector3(0, 1000f, 0);
            return;
        }

        m_CurrentPreview.transform.rotation = Quaternion.Euler(m_CurrentRotationOffset);

        if (m_LastPoint == new Vector3(0, 1000f, 0))
        {
            m_LastPoint = m_CurrentPreview.transform.position;
        }

        m_CurrentPreview.transform.position = m_LastPoint;
    }

    public void UpdateRotation()
    {
        m_CurrentPreview.transform.rotation = Quaternion.Euler(m_CurrentRotationOffset);
    }

    /// <summary>
    /// 放置预览物体
    /// </summary>
    /// <param name="group"></param>
    public virtual void PlacePrefab()
    {
        m_AllowPlacement = CheckPlacementConditions();

        if (!m_AllowPlacement)
        {
            return;
        }

        if (m_CurrentEditionPreview != null)
        {
            Destroy(m_CurrentEditionPreview.gameObject);
        }

        m_CurrentPreview.ChangeState(StateType.Placed);
        BuildEvent.instance.OnPieceInstantiated.Invoke(m_CurrentPreview);
        m_CurrentPreview = null;
        m_CurrentRotationOffset = Vector3.zero;
        m_AllowPlacement = false;
    }

    /// <summary>
    /// 创建预览物体
    /// </summary>
    /// <param name="prefab"></param>
    /// <returns></returns>
    public void CreatePreview(GameObject prefab,bool enableRender = true)
    {
        if (m_CurrentPreview != null)
        {
            Debug.LogError("Has m_CurrentPreview");
        }
        else
        {
            m_CurrentPreview = Instantiate(prefab).GetComponent<PieceBehaviour>();
            m_CurrentPreview.transform.eulerAngles = Vector3.zero;
            m_CurrentRotationOffset = Vector3.zero;
            
            m_AllowPlacement = CheckPlacementConditions(true);

            m_CurrentPreview.ChangeState(StateType.Preview);

            m_CurrentPreview.SetRendersEnable(enableRender);
            
            //Debug.LogError("CreatePreview : " + m_CurrentPreview.name);
        }
    }

    /// <summary>
    /// 清除当前预览
    /// </summary>
    public void ClearPreview()
    {
        if (m_CurrentPreview != null)
        {
            BuildEvent.instance.OnPieceDestroyed.Invoke(m_CurrentPreview);
            
            Destroy(m_CurrentPreview.gameObject);

            m_AllowPlacement = false;

            m_CurrentPreview = null; 
        }
    }

    #endregion Placement

    #region Destruction

    /// <summary>
    /// This method allows to update the destruction preview.
    /// </summary>
    public void UpdateRemovePreview()
    {
        if (m_CurrentEditionPreview != null)
        {
            DestroyObjs();
        }
    }

    /// <summary>
    /// This method allows to remove the current preview.
    /// </summary>
    public void DestroyObjs()
    {
        //Debug.LogError("DestroyObjs");
        
        Destroy(m_CurrentEditionPreview.gameObject);

        ChangeMode(BuildModeType.None);
    }

    #endregion Destruction

    #region Edition

    /// <summary>
    /// This method allows to update the edition mode.
    /// </summary>
    public void UpdateEditionPreview()
    {
        m_AllowEdition = m_CurrentEditionPreview;

        if (m_CurrentEditionPreview != null && m_AllowEdition)
        {
            m_CurrentEditionPreview.ChangeState(StateType.Edit);
        }

        float distance = m_DetectionDistance;

        if (Physics.Raycast(GetRay, out RaycastHit Hit, distance, BuildManager.instance.BuildableLayer))
        {
            PieceBehaviour Piece = Hit.collider.GetComponentInParent<PieceBehaviour>();

            if (Piece != null)
            {
                if (m_CurrentEditionPreview != null)
                {
                    if (m_CurrentEditionPreview.GetInstanceID() != Piece.GetInstanceID())
                    {
                        ClearEditionPreview();

                        m_CurrentEditionPreview = Piece;
                    }
                }
                else
                {
                    m_CurrentEditionPreview = Piece;
                }
            }
            else
            {
                ClearEditionPreview();
            }
        }
        else
        {
            ClearEditionPreview();
        }
    }

    /// <summary>
    /// This method allows to check the internal edition conditions.
    /// </summary>
    public bool CheckEditionConditions()
    {
        if (m_CurrentEditionPreview == null)
        {
            return false;
        }

        if (!m_CurrentEditionPreview.CheckExternalEditionConditions())
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// This method allows to edit the current preview.
    /// </summary>
    public virtual void EditPrefab()
    {
        m_AllowEdition = CheckEditionConditions();

        if (!m_AllowEdition)
        {
            return;
        }

        PieceBehaviour pieceTemp = m_CurrentEditionPreview;

        pieceTemp.ChangeState(StateType.Edit);

        SelectPrefab(pieceTemp);

        ChangeMode(BuildModeType.Placement);
    }

    /// <summary>
    /// This method allows to clear the current edition preview.
    /// </summary>
    public void ClearEditionPreview()
    {
        if (m_CurrentEditionPreview == null)
        {
            return;
        }

        m_CurrentEditionPreview.ChangeState(m_CurrentEditionPreview.LastState);

        m_AllowEdition = false;

        m_CurrentEditionPreview = null;
    }

    #endregion Edition

    /// <summary>
    /// This method allows to update all the builder (Placement, Destruction, Edition).
    /// </summary>
    public virtual void UpdateModes()
    {
        //Debug.LogError("UpdateModes");
        if (!Application.isPlaying)
        {
            return;
        }
        
        if (BuildManager.instance == null)
        {
            return;
        }

        if (BuildManager.instance.Pieces == null)
        {
            return;
        }

        if (m_CurrentModeType == BuildModeType.Placement)
        {
            UpdatePreview();
        }
        else if (m_CurrentModeType == BuildModeType.Destruction)
        {
            UpdateRemovePreview();
        }
        else if (m_CurrentModeType == BuildModeType.Edition)
        {
            UpdateEditionPreview();
        }
        else if (m_CurrentModeType == BuildModeType.None)
        {
            ClearPreview();
        }
    }

    /// <summary>
    /// This method allows to change mode.
    /// </summary>
    public void ChangeMode(BuildModeType modeType)
    {
        //Debug.LogError("ChangeMode : " + m_LastModeType + " - " + m_CurrentModeType + " - " + modeType);
        if (m_CurrentModeType == modeType)
        {
            return;
        }

        if (modeType == BuildModeType.Placement)
        {
            if (m_CurrentModeType == BuildModeType.Edition)
            {
                CreatePreview(m_SelectedPrefab.gameObject, false);
            }
            else
            {
                CreatePreview(m_SelectedPrefab.gameObject);
            }
        }

        if (m_CurrentModeType == BuildModeType.Placement)
        {
            ClearPreview();
        }

        if (modeType == BuildModeType.None)
        {
            ClearPreview();
            ClearEditionPreview();
        }

        m_LastModeType = m_CurrentModeType;

        m_CurrentModeType = modeType;

        BuildEvent.instance.OnChangedBuildMode.Invoke(m_CurrentModeType);
    }

    /// <summary>
    /// This method allows to select a prefab.
    /// </summary>
    public void SelectPrefab(PieceBehaviour prefab)
    {
        if (prefab == null)
        {
            return;
        }

        m_SelectedPrefab = BuildManager.instance.GetPieceById(prefab.ID);
    }

    private void OnDrawGizmosSelected()
    {
        if (m_Camera == null)
        {
            m_Camera = GetComponent<Camera>();
            return;
        }

        Gizmos.color = Color.magenta;
        Gizmos.DrawLine(GetRay.origin, GetRay.direction * m_DetectionDistance);
    }

}