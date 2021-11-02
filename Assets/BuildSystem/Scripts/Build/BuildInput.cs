using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class BuildInput : MonoBehaviour
{
    [SerializeField] private GameObject m_BtnGroup;
    
    private void Awake()
    {
        
    }

    private void Start()
    {

    }

    private void Update()
    {

    }

    /// <summary>
    /// This method allows to select a prefab.
    /// </summary>
    public GameObject SelectPrefab(PieceBehaviour prefab)
    {
        return BuildManager.instance.GetPieceById(prefab.ID).gameObject;
    }
    
    public void Rotate()
    {
        if (BuildBehaviour.instance.CurrentPreview == null)
        {
            return;
        }
        BuildBehaviour.instance.RotatePreview();
    }

    public void New(int index)
    {
        if (BuildBehaviour.instance.CurrentPreview != null)
        {
            return;
        }
        
        GameObject temp = SelectPrefab(BuildManager.instance.Pieces[index]);
        BuildBehaviour.instance.CreatePreview(temp);
        
        BuildBehaviour.instance.ChangeMode(BuildModeType.Placement);
        m_BtnGroup.SetActive(true);
    }

    public void Preview()
    {
        BuildBehaviour.instance.ChangeMode(BuildModeType.Placement);
        m_BtnGroup.SetActive(true);
    }

    public void Sure()
    {
        if (BuildBehaviour.instance.CurrentPreview != null)
        {
            if (!BuildBehaviour.instance.AllowPlacement)
            {
                return;
            }
            BuildBehaviour.instance.PlacePreview();
            BuildBehaviour.instance.ChangeMode(BuildModeType.None);
        }
        
        m_BtnGroup.SetActive(false);
    }
    
    public void Delete()
    {
        if (BuildBehaviour.instance.CurrentPreview != null)
        {
            BuildBehaviour.instance.ChangeMode(BuildModeType.None);
        }
        
        m_BtnGroup.SetActive(false);
    }
    
    public void Cancel()
    {
        if (BuildBehaviour.instance.CurrentPreview != null)
        {
            if (BuildBehaviour.instance.IsNew)
            {
                BuildBehaviour.instance.ChangeMode(BuildModeType.None);
            }
            else
            {
                BuildBehaviour.instance.ResetPreview();
                BuildBehaviour.instance.PlacePreview();
                BuildBehaviour.instance.ChangeMode(BuildModeType.None);
            }
        }
        
        m_BtnGroup.SetActive(false);
    }
}