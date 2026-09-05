using UnityEngine;
using System.Collections.Generic;

// This script's ONE JOB: check "am I touching my correct neighbor?"
// and if so, glue us together forever (they become one BlockGroup).
public class SnapChecker : MonoBehaviour
{
    // How close two pieces need to be (in world units) to count as "touching correctly"
    public static float snapDistance = 0.6f;

    // Call this whenever a piece OR a whole group is DROPPED after dragging.
    // Returns TRUE if it successfully glued to something, FALSE if not.
    public static bool CheckForSnap(GameObject droppedObject)
    {
        PuzzlePiece[] myPieces = droppedObject.GetComponentsInChildren<PuzzlePiece>();
        bool didSnapHappen = false;

        foreach (PuzzlePiece myTag in myPieces)
        {
            bool snapped = CheckSinglePieceForSnap(myTag);
            if (snapped) didSnapHappen = true;
        }

        return didSnapHappen;
    }

    private static bool CheckSinglePieceForSnap(PuzzlePiece myTag)
    {
        GameObject droppedPiece = myTag.gameObject;
        bool foundSnap = false;

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
                // They're close enough AND correctly lined up!
                // Before gluing, SNAP the dropped piece to the EXACT perfect spot
                // (no more gap - pieces click together seamlessly)
                SnapIntoExactPosition(myTag, otherTag);

                // Now glue them permanently
                GlueTogether(droppedPiece, otherTag.gameObject);

                foundSnap = true;
            }
        }

        return foundSnap;
    }

    // Looks for ANOTHER piece sitting very close to where you just dropped
    // this one - used for SWAPPING two pieces' places when they don't glue.
    // Returns null if nothing is nearby.
    public static GameObject FindNearbyOccupant(GameObject droppedObject, float radius = 0.5f)
    {
        PuzzlePiece[] myPieces = droppedObject.GetComponentsInChildren<PuzzlePiece>();
        if (myPieces.Length == 0) return null;

        // For simplicity, swapping only works for single (ungrouped) pieces
        if (myPieces.Length > 1) return null;

        GameObject myPiece = myPieces[0].gameObject;
        BlockGroup myGroup = myPiece.GetComponentInParent<BlockGroup>();

        PuzzlePiece[] allPieces = Object.FindObjectsOfType<PuzzlePiece>();
        GameObject closest = null;
        float closestDistance = radius;

        foreach (PuzzlePiece otherTag in allPieces)
        {
            if (otherTag.gameObject == myPiece) continue;

            BlockGroup otherGroup = otherTag.gameObject.GetComponentInParent<BlockGroup>();
            if (myGroup != null && myGroup == otherGroup) continue; // skip my own family

            float dist = Vector3.Distance(droppedObject.transform.position, otherTag.transform.position);
            if (dist < closestDistance)
            {
                closestDistance = dist;
                closest = GetTopLevelTransform(otherTag.transform).gameObject;
            }
        }

        return closest;
    }

    // Checks if two pieces are allowed to be neighbors based on their grid position
    private static bool AreCorrectNeighbors(PuzzlePiece a, PuzzlePiece b)
    {
        int columns = a.gridColumns; // use the REAL width of this puzzle (4, 5, 6...)

        int aRow = a.pieceIndex / columns;
        int aCol = a.pieceIndex % columns;
        int bRow = b.pieceIndex / columns;
        int bCol = b.pieceIndex % columns;

        // Neighbors means: same row & column next to each other (left/right/up/down)
        bool sameRowNextCol = (aRow == bRow) && (Mathf.Abs(aCol - bCol) == 1);
        bool sameColNextRow = (aCol == bCol) && (Mathf.Abs(aRow - bRow) == 1);

        return sameRowNextCol || sameColNextRow;
    }

    // Moves the dropped piece (and its whole glued family, if it has one)
    // so it lines up PERFECTLY with its neighbor - smoothly gliding, no gap!
    private static void SnapIntoExactPosition(PuzzlePiece myTag, PuzzlePiece otherTag)
    {
        // Step 1: Figure out where "myTag" SHOULD be in the world,
        // based on the neighbor's actual position + the correct gap between them
        Vector3 correctGap = myTag.correctPosition - otherTag.correctPosition;
        Vector3 exactTargetPosition = otherTag.transform.position + correctGap;

        // Step 2: How far off are we from that perfect spot?
        Vector3 moveNeeded = exactTargetPosition - myTag.transform.position;

        // Step 3: Find the TOP-LEVEL object (the whole group if it's glued to others,
        // or just itself if it's alone) - this is what we'll smoothly glide
        Transform topLevelObject = GetTopLevelTransform(myTag.transform);
        Vector3 finalDestination = topLevelObject.position + moveNeeded;

        // Step 4: Glide smoothly to that spot instead of teleporting instantly
        float glideTime = 0.15f; // quick but visible - feels satisfying, not sluggish
        CoroutineRunner.Instance.SmoothMoveTo(topLevelObject, finalDestination, glideTime);
    }

    // Finds the "topmost" parent of a piece - either its BlockGroup (family), or itself if alone
    private static Transform GetTopLevelTransform(Transform pieceTransform)
    {
        BlockGroup group = pieceTransform.GetComponentInParent<BlockGroup>();
        if (group != null)
        {
            return group.transform;
        }
        return pieceTransform;
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