using UnityEngine;

// This little script just REMEMBERS where a block is supposed to go
// when the puzzle is finished. Think of it like a name tag that says
// "I belong at THIS spot!"
public class PuzzlePiece : MonoBehaviour
{
    public Vector3 correctPosition; // where this piece belongs when solved
    public int pieceIndex;          // which piece number this is (0, 1, 2...)
}