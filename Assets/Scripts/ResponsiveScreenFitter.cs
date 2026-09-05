using UnityEngine;
using System.Collections.Generic;
using System.Linq;

// This script runs ON THE ACTUAL DEVICE (phone, tablet, browser) when the game starts.
// It looks at how big the screen REALLY is, then shrinks/grows and repositions
// the whole puzzle so it always fits perfectly - no matter the screen shape.
// It ALSO draws a "frame" (empty outline box) showing exactly where the
// finished picture will be built, sized correctly for any grid (4x4, 5x5, 6x6...).
public class ResponsivePuzzleFitter : MonoBehaviour
{
    [Tooltip("How much empty space to leave around the puzzle (0.1 = 10% padding)")]
    public float paddingPercent = 0.1f;

    // These are shared/public so our drag scripts can check them too,
    // to make sure pieces never get dragged outside the frame
    public static Vector3 FrameCenter;
    public static float FrameWidth;
    public static float FrameHeight;
    public static bool FrameIsReady = false;

    void Start()
    {
        FitPuzzleToScreen();
    }

    void FitPuzzleToScreen()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            Debug.LogWarning("No Main Camera found - can't fit puzzle to screen.");
            return;
        }

        // Step 1: Find every puzzle piece currently in the scene
        PuzzlePiece[] allPieces = FindObjectsOfType<PuzzlePiece>();
        if (allPieces.Length == 0) return;

        // Step 2: Measure the puzzle's ORIGINAL size based on correct positions
        // (find the leftmost, rightmost, topmost, bottommost correct positions)
        float minX = allPieces.Min(p => p.correctPosition.x);
        float maxX = allPieces.Max(p => p.correctPosition.x);
        float minY = allPieces.Min(p => p.correctPosition.y);
        float maxY = allPieces.Max(p => p.correctPosition.y);

        float puzzleWidth = maxX - minX;
        float puzzleHeight = maxY - minY;

        // Add a little extra so we measure block edges, not just centers
        // (roughly one block's worth of size, based on average spacing)
        float blockSizeEstimate = 1.1f; // matches our default blockSpacing
        puzzleWidth += blockSizeEstimate;
        puzzleHeight += blockSizeEstimate;

        // Step 3: Measure how much space the REAL screen/camera gives us right now
        float camHeight = cam.orthographicSize * 2f;
        float camWidth = camHeight * cam.aspect;

        // Shrink the usable area a bit so there's breathing room (padding)
        float usableWidth = camWidth * (1f - paddingPercent);
        float usableHeight = camHeight * (1f - paddingPercent);

        // Step 4: Work out how much we need to SCALE the puzzle to fit
        // (pick the smaller ratio so it fits both width AND height)
        float scaleX = usableWidth / puzzleWidth;
        float scaleY = usableHeight / puzzleHeight;
        float finalScale = Mathf.Min(scaleX, scaleY);

        // Step 5: Apply that scale to every piece's position and size
        Vector3 puzzleCenterOriginal = new Vector3((minX + maxX) / 2f, (minY + maxY) / 2f, 0);

        foreach (PuzzlePiece piece in allPieces)
        {
            // Re-calculate this piece's correct position, scaled and centered on screen
            Vector3 offsetFromCenter = piece.correctPosition - puzzleCenterOriginal;
            Vector3 newCorrectPosition = (offsetFromCenter * finalScale) + cam.transform.position;
            newCorrectPosition.z = 0;

            piece.correctPosition = newCorrectPosition;

            // Also scale the piece's visual size so it matches the new spacing.
            // We shrink it slightly (95%) so pieces have a tiny gap between them
            // and don't visually overlap their neighbors when shuffled apart.
            piece.transform.localScale = Vector3.one * finalScale * 0.95f;
        }

        // Step 6: Remember the puzzle's play area bounds (invisible) so drag
        // scripts can still keep pieces from being dragged way off-screen.
        // We do NOT draw a visible frame here - you're placing your own!
        FrameCenter = cam.transform.position;
        FrameWidth = puzzleWidth * finalScale;
        FrameHeight = puzzleHeight * finalScale;
        FrameIsReady = true;

        // Step 7: Shuffle pieces into their own unique grid slots (no overlap),
        // strictly inside that same play area
        ShuffleWithinFrame(allPieces, FrameCenter, FrameWidth, FrameHeight);

        // Step 8: Also scale how "close" pieces need to be to snap together,
        // so bigger/smaller puzzles feel equally easy to connect
        SnapChecker.snapDistance = 0.6f * finalScale;

        Debug.Log("Puzzle fitted to screen with scale: " + finalScale);
    }

    // Scatters every piece into a random GRID SLOT (like shuffling seats in a
    // classroom) - this guarantees pieces NEVER overlap, since every piece
    // gets its own unique slot, just in the wrong order.
    void ShuffleWithinFrame(PuzzlePiece[] allPieces, Vector3 frameCenter, float frameWidth, float frameHeight)
    {
        // Step 1: Collect the list of "slots" - these are simply each piece's
        // own correct position (already perfectly spaced, no overlap there)
        List<Vector3> slots = new List<Vector3>();
        foreach (PuzzlePiece piece in allPieces)
        {
            slots.Add(piece.correctPosition);
        }

        // Step 2: Shuffle the ORDER of these slots (like shuffling a deck of cards)
        for (int i = slots.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            Vector3 temp = slots[i];
            slots[i] = slots[randomIndex];
            slots[randomIndex] = temp;
        }

        // Step 3: Hand out one shuffled slot to each piece
        // Since every slot was originally a unique, non-overlapping spot,
        // and each piece gets exactly ONE slot, nothing can overlap!
        for (int i = 0; i < allPieces.Length; i++)
        {
            allPieces[i].transform.position = slots[i];
        }
    }
}