using UnityEngine;
using System.Collections.Generic;
using System.Linq;

// This script makes the puzzle fit PERFECTLY inside a frame that YOU place
// in the Hierarchy yourself. Whatever size/shape your frame is, the puzzle
// will scale itself to match it exactly - and pieces (even glued groups)
// can never be shuffled, dragged, or glued outside that frame.
public class ResponsivePuzzleFitter : MonoBehaviour
{
    [Tooltip("Drag your own Frame GameObject here - the puzzle will fit exactly inside it")]
    public GameObject frameReference;

    // Shared/public so drag scripts can check these too, to keep
    // pieces (and glued groups) from ever leaving the frame
    public static Vector3 FrameCenter;
    public static float FrameWidth;
    public static float FrameHeight;
    public static bool FrameIsReady = false;

    void Start()
    {
        FitPuzzleToFrame();
    }

    void FitPuzzleToFrame()
    {
        if (frameReference == null)
        {
            Debug.LogWarning("No Frame assigned on ResponsivePuzzleFitter! Drag your frame object into the 'Frame Reference' slot in the Inspector.");
            return;
        }

        // Step 1: Measure the frame's REAL size and position in the world
        // (using its SpriteRenderer if it has one, otherwise its transform scale)
        Vector3 frameCenter;
        float frameWidth;
        float frameHeight;

        SpriteRenderer frameSprite = frameReference.GetComponent<SpriteRenderer>();
        if (frameSprite != null)
        {
            frameCenter = frameSprite.bounds.center;
            frameWidth = frameSprite.bounds.size.x;
            frameHeight = frameSprite.bounds.size.y;
        }
        else
        {
            // Fallback: just use the object's transform position/scale directly
            frameCenter = frameReference.transform.position;
            frameWidth = frameReference.transform.localScale.x;
            frameHeight = frameReference.transform.localScale.y;
        }

        // Step 2: Find every puzzle piece currently in the scene
        PuzzlePiece[] allPieces = FindObjectsOfType<PuzzlePiece>();
        if (allPieces.Length == 0) return;

        // Step 3: Measure the puzzle's ORIGINAL size based on correct positions
        float minX = allPieces.Min(p => p.correctPosition.x);
        float maxX = allPieces.Max(p => p.correctPosition.x);
        float minY = allPieces.Min(p => p.correctPosition.y);
        float maxY = allPieces.Max(p => p.correctPosition.y);

        float puzzleWidth = maxX - minX;
        float puzzleHeight = maxY - minY;

        // Add a little extra so we measure block edges, not just centers
        float blockSizeEstimate = 1.1f; // matches our default blockSpacing
        puzzleWidth += blockSizeEstimate;
        puzzleHeight += blockSizeEstimate;

        // Step 4: Work out how much to SCALE the puzzle so it fills the frame
        // (using the smaller ratio keeps the picture from stretching weirdly,
        // so it fits fully inside without spilling over either edge)
        float scaleX = frameWidth / puzzleWidth;
        float scaleY = frameHeight / puzzleHeight;
        float finalScale = Mathf.Min(scaleX, scaleY);

        // Step 5: Apply that scale to every piece's correct position and visual size
        Vector3 puzzleCenterOriginal = new Vector3((minX + maxX) / 2f, (minY + maxY) / 2f, 0);

        foreach (PuzzlePiece piece in allPieces)
        {
            Vector3 offsetFromCenter = piece.correctPosition - puzzleCenterOriginal;
            Vector3 newCorrectPosition = (offsetFromCenter * finalScale) + frameCenter;
            newCorrectPosition.z = 0;

            piece.correctPosition = newCorrectPosition;

            // Shrink pieces slightly (95%) so there's a tiny visible gap
            // between them and they never look like they're overlapping
            piece.transform.localScale = Vector3.one * finalScale * 0.95f;
        }

        // Step 6: Remember the frame's bounds so drag scripts (and shuffle)
        // can keep everything locked inside it - pieces AND glued groups
        FrameCenter = frameCenter;
        FrameWidth = frameWidth;
        FrameHeight = frameHeight;
        FrameIsReady = true;

        // Step 7: Shuffle pieces into their own unique grid slots (no overlap),
        // strictly inside the frame
        ShuffleWithinFrame(allPieces, FrameCenter, FrameWidth, FrameHeight);

        // Step 8: Scale the snap distance too, so bigger/smaller puzzles
        // feel equally easy to connect
        SnapChecker.snapDistance = 0.6f * finalScale;

        Debug.Log("Puzzle fitted exactly to frame with scale: " + finalScale);
    }

    // Scatters every piece into a random GRID SLOT (like shuffling seats in a
    // classroom) - this guarantees pieces NEVER overlap, since every piece
    // gets its own unique slot, just in the wrong order.
    void ShuffleWithinFrame(PuzzlePiece[] allPieces, Vector3 frameCenter, float frameWidth, float frameHeight)
    {
        List<Vector3> slots = new List<Vector3>();
        foreach (PuzzlePiece piece in allPieces)
        {
            slots.Add(piece.correctPosition);
        }

        // Shuffle the order of these slots (like shuffling a deck of cards)
        for (int i = slots.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            Vector3 temp = slots[i];
            slots[i] = slots[randomIndex];
            slots[randomIndex] = temp;
        }

        // Hand out one shuffled slot to each piece - guaranteed no overlap
        for (int i = 0; i < allPieces.Length; i++)
        {
            allPieces[i].transform.position = slots[i];
        }
    }
}