using System;
using UnityEngine;
using UnityEngine.Events;

public class BuildEvent : MonoBehaviour
{
    #region Fields

    public static BuildEvent instance;

    [Serializable] public class StorageLoadingResult : UnityEvent<PieceBehaviour[]> { }
    public StorageLoadingResult OnStorageLoadingResult;

    [Serializable] public class StorageSavingResult : UnityEvent<PieceBehaviour[]> { }
    public StorageSavingResult OnStorageSavingResult;

    [Serializable] public class PieceInstantiated : UnityEvent<PieceBehaviour> { }
    public PieceInstantiated OnPieceInstantiated;

    [Serializable] public class PieceDestroyed : UnityEvent<PieceBehaviour> { }
    public PieceDestroyed OnPieceDestroyed;

    [Serializable] public class PieceChangedState : UnityEvent<PieceBehaviour, StateType> { }
    public PieceChangedState OnPieceChangedState;

    [Serializable] public class ChangedBuildMode : UnityEvent<BuildModeType> { }
    public ChangedBuildMode OnChangedBuildMode;

    #endregion

    #region Methods

    private void Awake()
    {
        instance = this;
    }

    private void Update()
    {

    }

    #endregion
}