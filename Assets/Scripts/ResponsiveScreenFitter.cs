using UnityEngine;
using System.Collections.Generic;
using System.Linq;

// This script runs ON THE ACTUAL DEVICE (phone, tablet, browser) when the game starts.
// It looks at how big the screen REALLY is, then shrinks/grows and repositions
// the whole puzzle so it always fits perfectly - no matter the screen shape.
public class ResponsivePuzzleFitter : MonoBehaviour
{
    [Tooltip("How much empty space to leave around the puzzle (0.1 = 10% padding)")]
    public float paddingPercent = 0.1f;

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

            // Also scale the piece's visual size so it matches the new spacing
            piece.transform.localScale = Vector3.one * finalScale;
        }

        // Step 6: NOW shuffle pieces randomly, safely inside the real screen bounds
        ShuffleWithinScreen(allPieces, cam, usableWidth, usableHeight);

        // Step 7: Also scale how "close" pieces need to be to snap together,
        // so bigger/smaller puzzles feel equally easy to connect
        SnapChecker.snapDistance = 0.6f * finalScale;

        Debug.Log("Puzzle fitted to screen with scale: " + finalScale);
    }

    void ShuffleWithinScreen(PuzzlePiece[] allPieces, Camera cam, float usableWidth, float usableHeight)
    {
        float minX = cam.transform.position.x - (usableWidth / 2f);
        float maxX = cam.transform.position.x + (usableWidth / 2f);
        float minY = cam.transform.position.y - (usableHeight / 2f);
        float maxY = cam.transform.position.y + (usableHeight / 2f);

        foreach (PuzzlePiece piece in allPieces)
        {
            float randomX = Random.Range(minX, maxX);
            float randomY = Random.Range(minY, maxY);
            piece.transform.position = new Vector3(randomX, randomY, 0);
        }
    }
}