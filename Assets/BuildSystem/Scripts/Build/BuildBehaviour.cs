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
    [SerializeField] private bool m_PreviewMovementOnlyAllowed;

    [SerializeField] private PieceBehaviour m_CurrentPreview;
    [SerializeField] private FixationBehaviour m_FixationBehaviour;
    
    [SerializeField] private bool m_AllowPlacement;
    [SerializeField] private bool m_IsNew;

    public PieceBehaviour CurrentPreview => m_CurrentPreview;

    public bool IsNew => m_IsNew;

    private Camera m_Camera;
    private Transform m_CameraTrans;
    
    private Quaternion m_PieceQuaternion;
    private Quaternion m_FixationQuaternion;
    
    private Vector3 m_LastAllowedPoint;
    private Vector3 m_LastPoint;

    public Ray GetRay => m_Camera.ScreenPointToRay(Input.mousePosition);
    public bool AllowPlacement => m_AllowPlacement;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        m_Camera = Camera.main;
        m_CameraTrans = m_Camera.gameObject.transform;
        m_PieceQuaternion = transform.rotation;
    }

    private void Update()
    {
        UpdateModes();
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

    /// <summary>
    /// This method allows to update all the builder (Placement, Destruction, Edition).
    /// </summary>
    private void UpdateModes()
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

    private void UpdatePreview()
    {
        bool result = GetTypeFromParentRaycastHit(BuildManager.instance.BuildableLayer, out RaycastHit pieceRaycastHit, out PieceBehaviour piece);
        if (m_CurrentPreview == null)
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (result)
                {
                    if (piece != null)
                    {
                        m_CurrentPreview = piece;
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

            CheckPreviewNewOrOld(result, pieceRaycastHit, piece);
            UpdatePreviewPosition();
        }
    }

    private bool GetTypeFromParentRaycastHit<T>(LayerMask layer, out RaycastHit hit, out T piece) where T : MonoBehaviour
    {
        bool result = false;
        hit = new RaycastHit();
        piece = null;

        if (Physics.Raycast(GetRay, out RaycastHit temp, m_DetectionDistance, layer))
        {
            result = true;
            hit = temp;
            piece = hit.collider.GetComponentInParent<T>();
        }

        return result;
    }

    private bool GetRaycastHit(LayerMask layer, out RaycastHit hit)
    {
        bool result = false;
        hit = new RaycastHit();

        if (Physics.Raycast(GetRay, out RaycastHit temp, m_DetectionDistance, layer))
        {
            result = true;
            hit = temp;
        }

        return result;
    }

    private void CheckPreviewNewOrOld(bool resultPiece,RaycastHit pieceRaycastHit, PieceBehaviour piece)
    {
        if (Input.GetMouseButtonDown(0) && resultPiece && piece != null)
        {
            if (m_AllowPlacement && m_CurrentPreview != piece)
            {
                PlacePreview();
                m_CurrentPreview = piece;
                m_CurrentPreview.ChangeState(StateType.Preview);
            }
        }
    }

    private void UpdatePreviewPosition()
    {
        if (Input.GetMouseButton(0) && !IsPointerOverUIElement())
        {
            LayerMask layer = 0;
            if (m_CurrentPreview.PieceMoveType == PieceMoveType.Ground)
            {
                layer = BuildManager.instance.GroundLayer;
            }
            else if (m_CurrentPreview.PieceMoveType == PieceMoveType.Wall)
            {
                layer = BuildManager.instance.WallLayer;
            }
            
            bool result = GetRaycastHit(layer, out RaycastHit hit);
            bool fixationResult = GetTypeFromParentRaycastHit(BuildManager.instance.FixationLayer, out RaycastHit fixationRaycastHit, out FixationBehaviour fixation);
            
            if (result)
            {
                Vector3 targetPoint = hit.point;

                if (m_PreviewMovementType == MovementType.Grid)
                    targetPoint = MathExtension.PositionToGridPosition(m_PreviewGridSize, m_PreviewGridOffset, targetPoint);
                
                // if (m_PreviewMovementOnlyAllowed)
                // {
                //     m_CurrentPreview.transform.position = targetPoint;
                //
                //     if (m_CurrentPreview.CheckExternalPlacementConditions() && CheckPlacementConditions())
                //     {
                //         m_LastAllowedPoint = m_CurrentPreview.transform.position;
                //     }
                //     else
                //     {
                //         m_CurrentPreview.transform.rotation = Quaternion.FromToRotation(m_CameraTrans.up, hit.normal) * m_CameraTrans.rotation * m_PieceQuaternion;
                //         m_CurrentPreview.transform.position = m_LastAllowedPoint;
                //     }
                // }
                // else
                // {
                //     m_CurrentPreview.transform.rotation = Quaternion.FromToRotation(m_CameraTrans.up, hit.normal) * m_CameraTrans.rotation * m_PieceQuaternion;
                //     m_CurrentPreview.transform.position = targetPoint;
                // }

                if (m_CurrentPreview.PieceMoveType == PieceMoveType.Ground)
                {
                    targetPoint += m_CurrentPreview.PreviewOffset * hit.normal;
                }
                
                m_CurrentPreview.transform.rotation = Quaternion.FromToRotation(m_CameraTrans.up, hit.normal) * m_CameraTrans.rotation * m_PieceQuaternion;
                m_CurrentPreview.transform.position = targetPoint;

                if (fixationResult && fixation != null)
                {
                    if (m_FixationBehaviour != fixation)
                    {
                        m_FixationBehaviour = fixation;
                        m_FixationQuaternion = fixation.transform.rotation;
                    }

                    Vector3 fixationPoint = fixation.transform.position;

                    if (m_CurrentPreview.PieceMoveType == PieceMoveType.Ground)
                    {
                        fixationPoint += m_CurrentPreview.PreviewOffset * hit.normal;
                    }

                    m_CurrentPreview.transform.position = fixationPoint;
                    m_CurrentPreview.transform.rotation = m_FixationQuaternion;
                }
                else
                {
                    m_FixationBehaviour = null;
                }
                
                m_LastPoint = new Vector3(0, 1000f, 0);
            }
            else
            {
                if (m_LastPoint == new Vector3(0, 1000f, 0))
                {
                    m_LastPoint = m_CurrentPreview.transform.position;
                }

                m_CurrentPreview.transform.position = m_LastPoint;
            }
        }
    }

    private void ClearPreview()
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

    private bool CheckPlacementConditions()
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
            //m_CurrentPreview.transform.eulerAngles = Vector3.zero;
            //m_CurrentPreview.transform.position += m_CurrentPreview.PreviewOffset * Vector3.up;
            m_AllowPlacement = CheckPlacementConditions();

            m_CurrentPreview.ChangeState(StateType.Preview);

            //Debug.LogError("CreatePreview : " + m_CurrentPreview.name);
        }
    }
    
    public void PlacePreview()
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
    
    public void RotatePreview(Vector3 rotateAxis)
    {
        if (m_CurrentPreview == null)
        {
            return;
        }

        m_CurrentPreview.transform.Rotate(m_CurrentPreview.transform.up, 90, Space.World);
        if (m_FixationBehaviour == null)
        {
            m_PieceQuaternion = m_CurrentPreview.transform.rotation;
        }
        else
        {
            m_FixationQuaternion = m_CurrentPreview.transform.rotation;
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
}