using UnityEngine;
using System.Collections.Generic;

// This script's ONE JOB: check "am I touching my correct neighbor?"
// and if so, glue us together forever (they become one BlockGroup).
public class SnapChecker : MonoBehaviour
{
    // How close two pieces need to be (in world units) to count as "touching correctly"
    public static float snapDistance = 0.6f;

    // Call this whenever a piece OR a whole group is DROPPED after dragging
    public static void CheckForSnap(GameObject droppedObject)
    {
        // Find every PuzzlePiece "name tag" inside the dropped thing
        // (if it's a single piece, this finds just itself;
        //  if it's a whole glued group, this finds ALL pieces inside it)
        PuzzlePiece[] myPieces = droppedObject.GetComponentsInChildren<PuzzlePiece>();

        foreach (PuzzlePiece myTag in myPieces)
        {
            CheckSinglePieceForSnap(myTag);
        }
    }

    private static void CheckSinglePieceForSnap(PuzzlePiece myTag)
    {
        GameObject droppedPiece = myTag.gameObject;

        // Find EVERY other puzzle piece currently in the scene
        PuzzlePiece[] allPieces = Object.FindObjectsOfType<PuzzlePiece>();

        foreach (PuzzlePiece otherTag in allPieces)
        {
            if (otherTag.gameObject == droppedPiece) continue; // skip myself

            // Skip pieces that are already in the SAME family (no point re-gluing)
            BlockGroup myGroup = droppedPiece.GetComponentInParent<BlockGroup>();
            BlockGroup otherGroup = otherTag.gameObject.GetComponentInParent<BlockGroup>();
            if (myGroup != null && myGroup == otherGroup) continue;

            // STEP 1: Are these two pieces actually NEXT-DOOR NEIGHBORS in the real picture?
            // (e.g. piece 5 is only allowed to glue to pieces 4, 6, 1, or 9 in a 4-wide grid)
            if (!AreCorrectNeighbors(myTag, otherTag)) continue;

            // STEP 2: Is the GAP between their correct home positions matched
            // by the CURRENT gap between where they are right now?
            Vector3 correctGap = otherTag.correctPosition - myTag.correctPosition;
            Vector3 currentGap = otherTag.transform.position - droppedPiece.transform.position;

            float difference = Vector3.Distance(correctGap, currentGap);

            if (difference < snapDistance)
            {
                // They're close enough AND correctly lined up - GLUE THEM!
                GlueTogether(droppedPiece, otherTag.gameObject);
            }
        }
    }

    // Checks if two pieces are allowed to be neighbors based on their grid position
    private static bool AreCorrectNeighbors(PuzzlePiece a, PuzzlePiece b, int columns = 4)
    {
        int aRow = a.pieceIndex / columns;
        int aCol = a.pieceIndex % columns;
        int bRow = b.pieceIndex / columns;
        int bCol = b.pieceIndex % columns;

        // Neighbors means: same row & column next to each other (left/right/up/down)
        bool sameRowNextCol = (aRow == bRow) && (Mathf.Abs(aCol - bCol) == 1);
        bool sameColNextRow = (aCol == bCol) && (Mathf.Abs(aRow - bRow) == 1);

        return sameRowNextCol || sameColNextRow;
    }

    // This is where the GLUE happens - merges two pieces (or groups) into one
    private static void GlueTogether(GameObject pieceA, GameObject pieceB)
    {
        BlockGroup groupA = pieceA.GetComponentInParent<BlockGroup>();
        BlockGroup groupB = pieceB.GetComponentInParent<BlockGroup>();

        // If neither piece has a group yet, make a brand new one
        if (groupA == null && groupB == null)
        {
            GameObject newGroupObj = new GameObject("BlockGroup");
            BlockGroup newGroup = newGroupObj.AddComponent<BlockGroup>();
            newGroup.AddPiece(pieceA);
            newGroup.AddPiece(pieceB);
        }
        // If A already has a group, absorb B into it
        else if (groupA != null && groupB == null)
        {
            groupA.AddPiece(pieceB);
        }
        // If B already has a group, absorb A into it
        else if (groupA == null && groupB != null)
        {
            groupB.AddPiece(pieceA);
        }
        // If BOTH already have (different) groups, merge the two groups into one
        else if (groupA != groupB)
        {
            groupA.MergeWithGroup(groupB);
        }

        Debug.Log("Glued " + pieceA.name + " with " + pieceB.name + "!");
    }
}