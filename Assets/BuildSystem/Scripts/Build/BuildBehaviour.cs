using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class BuildBehaviour : MonoBehaviour
{
    public static BuildBehaviour instance;

    [SerializeField] private MovementType m_PreviewMovementType;

    [SerializeField] private BuildModeType m_CurrentModeType;

    [SerializeField] private float m_DetectionDistance = 10f;
    [SerializeField] private float m_PreviewGridSize = 1.0f;
    [SerializeField] private float m_PreviewGridOffset;
    [SerializeField] private float m_PreviewSmoothTime = 5.0f;
    [SerializeField] private bool m_PreviewMovementOnlyAllowed;

    [SerializeField] private PieceBehaviour m_CurrentPreview;

    [SerializeField] private bool m_AllowPlacement;
    [SerializeField] private bool m_IsNew;

    public PieceBehaviour CurrentPreview => m_CurrentPreview;

    public bool IsNew => m_IsNew;

    private Camera m_Camera;
    private Transform m_CameraTrans;
    private Vector3 m_LastAllowedPoint;
    private Vector3 m_LastPoint;
    public virtual Ray GetRay => m_Camera.ScreenPointToRay(Input.mousePosition);
    public float DetectionDistance => m_DetectionDistance;
    public bool AllowPlacement => m_AllowPlacement;

    public virtual void Awake()
    {
        instance = this;
    }

    public virtual void Start()
    {
        m_Camera = Camera.main;
        m_CameraTrans = m_Camera.gameObject.transform;
    }

    private void Update()
    {
        UpdateModes();
    }

    public void UpdatePreview()
    {
        if (m_CurrentPreview == null)
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (Physics.Raycast(GetRay, out RaycastHit hit, m_DetectionDistance, BuildManager.instance.BuildableLayer))
                {
                    PieceBehaviour currentPiece = hit.collider.GetComponentInParent<PieceBehaviour>();
                    if (currentPiece != null)
                    {
                        m_CurrentPreview = currentPiece;
                        m_CurrentPreview.ChangeState(StateType.Preview);
                    }
                }
            }
        }
        else
        {
            m_AllowPlacement = CheckPlacementConditions();
            m_CurrentPreview.gameObject.ChangeAllMaterialsColorInChildren(m_CurrentPreview.Renderers.ToArray(),
                m_AllowPlacement ? m_CurrentPreview.PreviewAllowedColor : m_CurrentPreview.PreviewDeniedColor);

            if (Input.GetMouseButtonDown(0))
            {
                if (Physics.Raycast(GetRay, out RaycastHit hit, m_DetectionDistance, BuildManager.instance.BuildableLayer))
                {
                    PieceBehaviour currentPiece = hit.collider.GetComponentInParent<PieceBehaviour>();
                    if (currentPiece != null)
                    {
                        if (m_CurrentPreview == currentPiece)
                        {

                        }
                        else
                        {
                            if (m_AllowPlacement)
                            {
                                PlacePrefab();
                                m_CurrentPreview = currentPiece;
                                m_CurrentPreview.ChangeState(StateType.Preview);
                            }
                        }
                    }
                }
            }

            if (Input.GetMouseButton(0) && !IsPointerOverUIElement())
            {
                Physics.Raycast(GetRay, out RaycastHit hit, m_DetectionDistance, BuildManager.instance.GroundLayer);
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
                    {
                        m_CurrentPreview.transform.position = nextPoint;
                    }

                    m_LastPoint = new Vector3(0, 1000f, 0);
                    return;
                }

                if (m_LastPoint == new Vector3(0, 1000f, 0))
                {
                    m_LastPoint = m_CurrentPreview.transform.position;
                }

                m_CurrentPreview.transform.position = m_LastPoint;
            }
        }
    }
    
    public bool CheckPlacementConditions()
    {
        if (m_CurrentPreview == null)
        {
            return false;
        }

        if (Vector3.Distance(m_CameraTrans.position, m_CurrentPreview.transform.position) > m_DetectionDistance)
        {
            return false;
        }

        if (!m_CurrentPreview.CheckExternalPlacementConditions())
        {
            return false;
        }

        return true;
    }

    public void RotatePreview(Vector3 rotateAxis)
    {
        if (m_CurrentPreview == null)
        {
            return;
        }

        Vector3 initRot = m_CurrentPreview.transform.rotation.eulerAngles;
        initRot += rotateAxis;
        m_CurrentPreview.transform.rotation = Quaternion.Euler(initRot);
    }

    /// <summary>
    /// Check if the cursor is above a UI element or if the ciruclar menu is open.
    /// </summary>
    private bool IsPointerOverUIElement()
    {
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            return false;
        }

        if (EventSystem.current == null)
        {
            return false;
        }

        PointerEventData EventData = new PointerEventData(EventSystem.current)
        {
            position = new Vector2(Input.mousePosition.x, Input.mousePosition.y)
        };

        List<RaycastResult> Results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(EventData, Results);
        return Results.Count > 0;
    }

    public virtual void PlacePrefab()
    {
        m_CurrentPreview.ChangeState(StateType.Placed);
        m_CurrentPreview = null;
        m_AllowPlacement = false;
        m_IsNew = false;
    }

    public void ResetPreview()
    {
        m_CurrentPreview.ResetPosRot();
    }
    
    public void CreatePreview(GameObject prefab)
    {
        if (m_CurrentPreview != null)
        {
            Debug.LogError("Has m_CurrentPreview");
        }
        else
        {
            m_IsNew = true;

            m_CurrentPreview = Instantiate(prefab).GetComponent<PieceBehaviour>();
            m_CurrentPreview.transform.eulerAngles = Vector3.zero;
            m_CurrentPreview.transform.position += m_CurrentPreview.PreviewOffset;
            m_AllowPlacement = CheckPlacementConditions();

            m_CurrentPreview.ChangeState(StateType.Preview);

            //Debug.LogError("CreatePreview : " + m_CurrentPreview.name);
        }
    }
    
    public void ClearPreview()
    {
        if (m_CurrentPreview != null)
        {
            BuildEvent.instance.OnPieceDestroyed.Invoke(m_CurrentPreview);

            Destroy(m_CurrentPreview.gameObject);

            m_AllowPlacement = false;
            m_IsNew = false;
            m_CurrentPreview = null;
        }
    }

    /// <summary>
    /// This method allows to update all the builder (Placement, Destruction, Edition).
    /// </summary>
    public virtual void UpdateModes()
    {
        if (m_CurrentModeType == BuildModeType.Placement)
        {
            UpdatePreview();
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

        m_CurrentModeType = modeType;
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