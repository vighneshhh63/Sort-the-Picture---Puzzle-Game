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

    [Tooltip("Color of the frame outline showing where the picture will be built")]
    public Color frameColor = new Color(1f, 1f, 1f, 0.4f); // soft transparent white

    private GameObject frameObject;

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

            // Also scale the piece's visual size so it matches the new spacing
            piece.transform.localScale = Vector3.one * finalScale;
        }

        // Step 6: Draw the "frame" FIRST - an empty outline showing exactly where
        // the finished picture belongs, sized to match this puzzle's grid.
        // We do this BEFORE shuffling so we know the frame's exact position/size.
        float finalFrameWidth = puzzleWidth * finalScale;
        float finalFrameHeight = puzzleHeight * finalScale;
        CreateOrUpdateFrame(cam.transform.position, finalFrameWidth, finalFrameHeight);

        // Step 7: NOW shuffle pieces - but ONLY inside the frame's own bounds,
        // never outside it, no matter what screen size we're on
        ShuffleWithinFrame(allPieces, cam.transform.position, finalFrameWidth, finalFrameHeight);

        // Step 8: Also scale how "close" pieces need to be to snap together,
        // so bigger/smaller puzzles feel equally easy to connect
        SnapChecker.snapDistance = 0.6f * finalScale;

        Debug.Log("Puzzle fitted to screen with scale: " + finalScale);
    }

    // Creates (or repositions/resizes if it already exists) the frame outline
    void CreateOrUpdateFrame(Vector3 centerPosition, float width, float height)
    {
        if (frameObject == null)
        {
            frameObject = new GameObject("PuzzleFrame");
            SpriteRenderer sr = frameObject.AddComponent<SpriteRenderer>();

            // Use Unity's simple built-in white square texture as our frame shape
            // pixelsPerUnit matches the texture size so this sprite is exactly
            // 1x1 world unit - that makes scaling it to any width/height precise
            sr.sprite = Sprite.Create(
                Texture2D.whiteTexture,
                new Rect(0, 0, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height),
                new Vector2(0.5f, 0.5f),
                Texture2D.whiteTexture.width
            );
            sr.color = frameColor;

            // Make sure the frame draws BEHIND all the puzzle pieces
            sr.sortingOrder = -1;
        }

        frameObject.transform.position = new Vector3(centerPosition.x, centerPosition.y, 0.1f); // slightly behind pieces
        frameObject.transform.localScale = new Vector3(width, height, 1f);

        // Remember these bounds so drag scripts can clamp pieces to stay inside
        FrameCenter = centerPosition;
        FrameWidth = width;
        FrameHeight = height;
        FrameIsReady = true;
    }

    // Scatters every piece to a random SAFE spot, but STRICTLY inside the frame
    // (never outside it, so the picture never spawns off the target area)
    void ShuffleWithinFrame(PuzzlePiece[] allPieces, Vector3 frameCenter, float frameWidth, float frameHeight)
    {
        // Guess how big one puzzle piece is, so we don't let its edge poke outside the frame
        float pieceHalfSize = 0.5f;
        if (allPieces.Length > 0)
        {
            SpriteRenderer sr = allPieces[0].GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                pieceHalfSize = Mathf.Max(sr.bounds.extents.x, sr.bounds.extents.y);
            }
        }

        float minX = frameCenter.x - (frameWidth / 2f) + pieceHalfSize;
        float maxX = frameCenter.x + (frameWidth / 2f) - pieceHalfSize;
        float minY = frameCenter.y - (frameHeight / 2f) + pieceHalfSize;
        float maxY = frameCenter.y + (frameHeight / 2f) - pieceHalfSize;

        // Safety check: if the frame is too small for even one piece to fit
        // with padding, just shuffle around the exact center instead of crashing
        if (minX > maxX) { minX = maxX = frameCenter.x; }
        if (minY > maxY) { minY = maxY = frameCenter.y; }

        foreach (PuzzlePiece piece in allPieces)
        {
            float randomX = Random.Range(minX, maxX);
            float randomY = Random.Range(minY, maxY);
            piece.transform.position = new Vector3(randomX, randomY, 0);
        }
    }
}