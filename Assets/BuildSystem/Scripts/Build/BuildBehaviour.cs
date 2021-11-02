using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class BuildBehaviour : MonoBehaviour
{
    public static BuildBehaviour instance;

    [SerializeField] private MovementType m_PreviewMovementType;

    [SerializeField] private BuildModeType m_CurrentModeType;

    [SerializeField] private float m_Sensor = 50;
    [SerializeField] private float m_DetectionDistance = 10f;
    [SerializeField] private float m_PreviewGridSize = 1.0f;
    [SerializeField] private float m_PreviewGridOffset;
    [SerializeField] private bool m_PreviewMovementOnlyAllowed;

    [SerializeField] private PieceBehaviour m_CurrentPreview;
    [SerializeField] private FixationBehaviour m_FixationBehaviour;

    [SerializeField] private bool m_AllowPlacement;
    [SerializeField] private bool m_IsNew;

    [SerializeField] private bool m_CanMove;
    [SerializeField] private Vector3 m_MoveInitPos;
    [SerializeField] private Vector3 m_MoveMousePos;

    [SerializeField] private MoveTargetBehaviour m_MoveTargetBehaviour;

    public PieceBehaviour CurrentPreview => m_CurrentPreview;

    public bool IsNew => m_IsNew;

    private Camera m_Camera;
    private Transform m_CameraTrans;

    private Vector3 m_LastAllowedPoint;
    private Vector3 m_LastPoint;

    private Quaternion m_CurrentPreviewRot;

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
        if (!IsPointerOverUIElement())
        {
            if (Input.GetMouseButtonDown(0))
            {
                GetOnePreview();
                CheckCanMove();
                CheckCanPlace();
            }

            if (Input.GetMouseButton(0))
            {
                Moving();
            }

            if (Input.GetMouseButtonUp(0))
            {
                if (m_CurrentPreview != null)
                {
                    m_MoveInitPos = m_CurrentPreview.transform.position;
                    m_MoveMousePos = Input.mousePosition;
                }
            }
        }
    }

    private void GetOnePreview()
    {
        bool result = GetTypeFromParentRaycastHit(BuildManager.instance.BuildableLayer, out RaycastHit pieceRaycastHit, out PieceBehaviour piece);
        if (result)
        {
            if (m_CurrentPreview == null)
            {
                if (piece != null)
                {
                    m_CurrentPreview = piece;
                    m_CurrentPreviewRot = m_CurrentPreview.transform.rotation;
                    m_CurrentPreview.ChangeState(StateType.Preview);
                }
            }
            else
            {
                if (piece != null)
                {
                    CheckPreviewNewOrOld(piece);
                }
            }
        }
    }

    private void CheckPreviewNewOrOld(PieceBehaviour piece)
    {
        if (m_AllowPlacement && m_CurrentPreview != piece)
        {
            PlacePreview();
            m_CurrentPreview = piece;
            m_CurrentPreviewRot = m_CurrentPreview.transform.rotation;
            m_CurrentPreview.ChangeState(StateType.Preview);
        }
    }

    private void CheckCanMove()
    {
        if (m_CurrentPreview != null)
        {
            LayerMask layer = m_CurrentPreview.GetLayerMask();
            bool moveResult = GetRaycastHit(layer, out RaycastHit hit);

            if (moveResult)
            {
                m_CanMove = true;
                m_MoveInitPos = hit.point;
                m_MoveMousePos = Input.mousePosition;
                m_MoveTargetBehaviour = hit.collider.GetComponent<MoveTargetBehaviour>();
            }
        }
    }

    private void CheckCanPlace()
    {
        if (m_CurrentPreview != null)
        {
            m_AllowPlacement = CheckPlacementConditions();
            m_CurrentPreview.gameObject.ChangeAllMaterialsColorInChildren(m_CurrentPreview.Renderers.ToArray(),
                m_AllowPlacement ? m_CurrentPreview.PreviewAllowedColor : m_CurrentPreview.PreviewDeniedColor);
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

    private void Moving()
    {
        if (m_CanMove)
        {
            CheckSameMoveTarget();

            Vector3 deltaMousePos = Input.mousePosition - m_MoveMousePos;

            if (m_MoveTargetBehaviour.PieceMoveType == PieceMoveType.Ground)
            {
                GroundMove(deltaMousePos);
            }
            else if (m_MoveTargetBehaviour.PieceMoveType == PieceMoveType.Wall_Forward)
            {
                WallForwardMove(deltaMousePos);
            }
            else if (m_MoveTargetBehaviour.PieceMoveType == PieceMoveType.Wall_Left)
            {
                WallLeftMove(deltaMousePos);
            }
            else if (m_MoveTargetBehaviour.PieceMoveType == PieceMoveType.Wall_Right)
            {
                WallRightMove(deltaMousePos);
            }
            
            CheckHasFixation();
            CheckCanPlace();
        }
    }

    private void GroundMove(Vector3 deltaMousePos)
    {
        Vector3 targetPoint = transform.position;
        targetPoint = m_MoveInitPos + new Vector3(deltaMousePos.x / m_Sensor, 0, deltaMousePos.y / m_Sensor);
        m_CurrentPreview.transform.rotation = Quaternion.identity;
        if (m_PreviewMovementType == MovementType.Grid)
        {
            targetPoint = m_MoveTargetBehaviour.PositionToGridPosition(m_PreviewGridSize, m_PreviewGridOffset, targetPoint);
        }

        m_CurrentPreview.transform.position = targetPoint;
        m_MoveTargetBehaviour.ClampPosition(m_CurrentPreview);
    }

    private void WallForwardMove(Vector3 deltaMousePos)
    {
        Vector3 targetPoint = transform.position;
        targetPoint = m_MoveInitPos + new Vector3(deltaMousePos.x / m_Sensor, deltaMousePos.y / m_Sensor, 0);
        m_CurrentPreview.transform.rotation = Quaternion.Euler(-90f, 0, 0f);
        if (m_PreviewMovementType == MovementType.Grid)
        {
            targetPoint = m_MoveTargetBehaviour.PositionToGridPosition(m_PreviewGridSize, m_PreviewGridOffset, targetPoint);
        }

        m_CurrentPreview.transform.position = targetPoint;
        m_MoveTargetBehaviour.ClampPosition(m_CurrentPreview);
    }

    private void WallLeftMove(Vector3 deltaMousePos)
    {
        Vector3 targetPoint = transform.position;
        LayerMask layer = m_CurrentPreview.GetLayerMask();
        bool moveResult = GetRaycastHit(layer, out RaycastHit hit);
        if (moveResult)
        {
            targetPoint = hit.point;
            m_CurrentPreview.transform.rotation = Quaternion.Euler(-90f, 0f, -90f);
            if (m_PreviewMovementType == MovementType.Grid)
            {
                targetPoint = m_MoveTargetBehaviour.PositionToGridPosition(m_PreviewGridSize, m_PreviewGridOffset, targetPoint);
            }

            m_CurrentPreview.transform.position = targetPoint;
            m_MoveTargetBehaviour.ClampPosition(m_CurrentPreview);

            m_MoveInitPos = hit.point;
            m_MoveMousePos = Input.mousePosition;
        }
        else
        {
            targetPoint = m_MoveInitPos + new Vector3(0, -deltaMousePos.x / m_Sensor, deltaMousePos.y / m_Sensor);
            if (m_PreviewMovementType == MovementType.Grid)
            {
                targetPoint = m_MoveTargetBehaviour.PositionToGridPosition(m_PreviewGridSize, m_PreviewGridOffset, targetPoint);
            }
            m_CurrentPreview.transform.position = targetPoint;
            m_MoveTargetBehaviour.ClampPosition(m_CurrentPreview);
        }
    }

    private void WallRightMove(Vector3 deltaMousePos)
    {
        Vector3 targetPoint = transform.position;
        LayerMask layer = m_CurrentPreview.GetLayerMask();
        bool moveResult = GetRaycastHit(layer, out RaycastHit hit);
        if (moveResult)
        {
            targetPoint = hit.point;
            m_CurrentPreview.transform.rotation = Quaternion.Euler(-90f, 180f, -90f);
            if (m_PreviewMovementType == MovementType.Grid)
            {
                targetPoint = m_MoveTargetBehaviour.PositionToGridPosition(m_PreviewGridSize, m_PreviewGridOffset, targetPoint);
            }

            m_CurrentPreview.transform.position = targetPoint;
            m_MoveTargetBehaviour.ClampPosition(m_CurrentPreview);
            
            m_MoveInitPos = hit.point;
            m_MoveMousePos = Input.mousePosition;
        }
        else
        {
            targetPoint = m_MoveInitPos + new Vector3(0, deltaMousePos.x / m_Sensor, deltaMousePos.y / m_Sensor);
            if (m_PreviewMovementType == MovementType.Grid)
            {
                targetPoint = m_MoveTargetBehaviour.PositionToGridPosition(m_PreviewGridSize, m_PreviewGridOffset, targetPoint);
            }
            m_CurrentPreview.transform.position = targetPoint;
            m_MoveTargetBehaviour.ClampPosition(m_CurrentPreview);
        }
    }

    private void CheckSameMoveTarget()
    {
        LayerMask newLayer = m_CurrentPreview.GetLayerMask();
        bool newResult = GetRaycastHit(newLayer, out RaycastHit newHit);

        if (newResult)
        {
            MoveTargetBehaviour temp = newHit.collider.GetComponent<MoveTargetBehaviour>();
            if (temp != m_MoveTargetBehaviour)
            {
                CheckCanMove();
            }
        }
    }

    private void CheckHasFixation()
    {
        bool fixationResult = GetTypeFromParentRaycastHit(BuildManager.instance.FixationLayer, out RaycastHit fixationRaycastHit, out FixationBehaviour fixation);

        if (m_FixationBehaviour != fixation)
        {
            m_CurrentPreview.SetFixationBehaviour(fixation);
            m_FixationBehaviour = fixation;
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
            m_CurrentPreview.ChangeState(StateType.Preview);
            CheckCanPlace();
        }
    }

    public void PlacePreview()
    {
        m_CurrentPreview.ChangeState(StateType.Placed);
        m_CanMove = false;
        m_CurrentPreview = null;
        m_AllowPlacement = false;
        m_IsNew = false;
    }

    public void ResetPreview()
    {
        m_CurrentPreview.ResetPosRot();
    }

    public void RotatePreview()
    {
        if (m_CurrentPreview == null)
        {
            return;
        }

        m_CurrentPreview.RotateModel();
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