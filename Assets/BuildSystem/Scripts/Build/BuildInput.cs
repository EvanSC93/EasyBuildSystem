using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class BuildInput : MonoBehaviour
{
    [SerializeField] private KeyCode m_BuilderValidateModeKey = KeyCode.Mouse0;
    [SerializeField] private bool m_CanOperation = true;
    [SerializeField] private float m_OperationDelay = 0.2f;
    [SerializeField] private GameObject m_BtnGroup;
    private int m_SelectedIndex;
    private float m_OperationDelayTemp;

    #region Methods

    private void Awake()
    {
        
    }

    private void Start()
    {
        m_OperationDelayTemp = m_OperationDelay;
    }

    private void Update()
    {
        switch (BuildBehaviour.instance.CurrentModeType)
        {
            case BuildModeType.None:
                UpdateNone();
                break;
            case BuildModeType.Placement:
                UpdatePlacement();
                break;
            case BuildModeType.Destruction:
                UpdateDestruction();
                break;
            case BuildModeType.Edition:
                UpdateEdition();
                break;
        }
    }

    private void UpdateNone()
    {
        if (Input.GetKey(m_BuilderValidateModeKey))
        {
            if (Physics.Raycast(BuildBehaviour.instance.GetRay, out RaycastHit Hit, BuildBehaviour.instance.DetectionDistance, BuildManager.instance.BuildableLayer))
            {
                if (Hit.collider.GetComponentInParent<PieceBehaviour>())
                {
                    Edit();
                }
              
            }
        }
    }
    
    private void UpdateEdition()
    {
        if (IsPointerOverUIElement())
        {
            return;
        }
        
        BuildBehaviour.instance.UpdateModes();
        BuildBehaviour.instance.EditPrefab();
    }

    private void UpdateDestruction()
    {
        BuildBehaviour.instance.UpdateModes();
    }
    
    private void UpdatePlacement()
    {
        if (IsPointerOverUIElement())
        {
            return;
        }

        if (Input.GetKey(m_BuilderValidateModeKey))
        {
            BuildBehaviour.instance.UpdateModes();
        }
    }
    
    private void UpdatePrefabSelection()
    {
        float WheelAxis = Input.GetAxis("Mouse ScrollWheel");

        if (WheelAxis > 0)
        {
            if (m_SelectedIndex < BuildManager.instance.Pieces.Count - 1)
            {
                m_SelectedIndex++;
            }
            else
            {
                m_SelectedIndex = 0;
            }
        }
        else if (WheelAxis < 0)
        {
            if (m_SelectedIndex > 0)
            {
                m_SelectedIndex--;
            }
            else
            {
                m_SelectedIndex = BuildManager.instance.Pieces.Count - 1;
            }
        }

        if (m_SelectedIndex == -1)
        {
            return;
        }

        if (BuildManager.instance.Pieces.Count != 0)
        {
            BuildBehaviour.instance.SelectPrefab(BuildManager.instance.Pieces[m_SelectedIndex]);
        }
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
    
    public void Delete()
    {
        BuildBehaviour.instance.ChangeMode(BuildModeType.Destruction);
        m_BtnGroup.SetActive(false);
    }

    public void Rotate()
    {
        BuildBehaviour.instance.RotatePreview(BuildBehaviour.instance.SelectedPrefab.RotationAxis);
        BuildBehaviour.instance.UpdateRotation();
    }

    public void Edit()
    {
        BuildBehaviour.instance.ChangeMode(BuildModeType.Edition);
        m_BtnGroup.SetActive(true);
    }

    public void Preview(int index)
    {
        BuildBehaviour.instance.SelectPrefab(BuildManager.instance.Pieces[index]);
        BuildBehaviour.instance.ChangeMode(BuildModeType.Placement);
        m_BtnGroup.SetActive(true);
    }

    public void Cancel()
    {
        BuildBehaviour.instance.ChangeMode(BuildModeType.None);
        m_BtnGroup.SetActive(false);
    }

    public void Sure()
    {
        BuildBehaviour.instance.PlacePrefab();
        BuildBehaviour.instance.ChangeMode(BuildModeType.None);
        m_BtnGroup.SetActive(false);
    }

    #endregion
}