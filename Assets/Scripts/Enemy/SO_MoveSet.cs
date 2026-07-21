using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewMoveSet", menuName = "Scriptable Objects/MoveSet")]
public class SO_MoveSet : ScriptableObject
{
    public enum PieceType
    {
        Pawn, Rook, Knight, Bishop, Queen, King
    }

    public PieceType pieceType;

    [SerializeField] private List<Vector3Int> offsets;
    [SerializeField] private int range;

    public IReadOnlyList<Vector3Int> Offsets => offsets;
    public int Range => range;

    private void OnValidate()
    {
        switch (pieceType)
        {
            case PieceType.Pawn:
                offsets = new List<Vector3Int> 
                { 
                    Vector3Int.right,
                    Vector3Int.left,
                    Vector3Int.up,
                    Vector3Int.down, 
                };
                range = 1;
                break;

            case PieceType.Rook:
                offsets = new List<Vector3Int>
                {
                    Vector3Int.right,
                    Vector3Int.left,
                    Vector3Int.up,
                    Vector3Int.down,
                };
                range = 5;
                break;

            case PieceType.Knight:
                offsets = new List<Vector3Int>
                {
                    new Vector3Int( 1,2,0),
                    new Vector3Int( -1,2,0),
                    new Vector3Int( 2,1,0),
                    new Vector3Int( 2,-1,0),
                    new Vector3Int(-2,1,0),
                    new Vector3Int(-2,-1,0),
                    new Vector3Int(-1,-2,0),
                    new Vector3Int(1, -2,0),
                };
                range = 1;
                break;
            
            case PieceType.Bishop:
                offsets = new List<Vector3Int>
                {
                    new Vector3Int( 1,1,0),
                    new Vector3Int( 1,-1,0),
                    new Vector3Int( -1,1,0),
                    new Vector3Int( -1,-1,0)
                };
                range = 5;
                break;
            
            case PieceType.Queen:
                offsets = new List<Vector3Int>
                {
                    Vector3Int.right,
                    Vector3Int.left,
                    Vector3Int.up,
                    Vector3Int.down,
                    new Vector3Int( 1,1,0),
                    new Vector3Int( 1,-1,0),
                    new Vector3Int( -1,1,0),
                    new Vector3Int( -1,-1,0)
                };
                range = 5;
                break;
            
            case PieceType.King:
                offsets = new List<Vector3Int>
                {
                    Vector3Int.right,
                    Vector3Int.left,
                    Vector3Int.up,
                    Vector3Int.down,
                    new Vector3Int( 1,1,0),
                    new Vector3Int( 1,-1,0),
                    new Vector3Int( -1,1,0),
                    new Vector3Int( -1,-1,0)
                };
                range = 1;
                break;
        }
    }
}