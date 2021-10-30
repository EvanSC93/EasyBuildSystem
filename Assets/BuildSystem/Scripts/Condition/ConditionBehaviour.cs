using UnityEngine;

public class ConditionBehaviour : MonoBehaviour
{
    private PieceBehaviour _Piece;
    public PieceBehaviour Piece
    {
        get
        {
            if (_Piece == null)
                _Piece = GetComponent<PieceBehaviour>();
            return _Piece;
        }
        set { }
    }

    public virtual bool CheckForPlacement() { return true; }
    public virtual bool CheckForDestruction() { return true; }
    public virtual bool CheckForEdition() { return true; }
}