using UnityEngine;

public class ConditionBehaviour : MonoBehaviour
{
    private PieceBehaviour m_Piece;

    public PieceBehaviour GetSelfPiece()
    {
        if (m_Piece == null)
        {
            m_Piece = GetComponent<PieceBehaviour>();
        }

        return m_Piece;
    }

    public virtual bool CheckForPlacement()
    {
        return true;
    }
}