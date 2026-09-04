using UnityEngine;
using System.Collections.Generic;

// A BlockGroup is like a "family" of puzzle pieces that are glued together.
// Once pieces are in the same family, dragging ONE drags them ALL.
public class BlockGroup : MonoBehaviour
{
    public List<GameObject> pieces = new List<GameObject>();

    // Add a single piece into this family
    public void AddPiece(GameObject piece)
    {
        // Make this piece a "child" of the group in the Hierarchy
        // so moving the group automatically moves the piece too.
        // "true" means: keep its exact current world position, don't jump anywhere!
        piece.transform.SetParent(this.transform, true);

        if (!pieces.Contains(piece))
        {
            pieces.Add(piece);
        }

        // IMPORTANT: once a piece is glued, it should no longer be
        // draggable BY ITSELF - only the whole group should move together.
        // We disable its own drag script and let the GROUP handle dragging instead.
        SimpleDrag individualDrag = piece.GetComponent<SimpleDrag>();
        if (individualDrag != null)
        {
            individualDrag.enabled = false;
        }

        // Make sure the GROUP itself can be dragged as a whole
        if (GetComponent<GroupDrag>() == null)
        {
            gameObject.AddComponent<GroupDrag>();
            gameObject.AddComponent<BoxCollider2D>(); // simple click area for the whole group
        }
    }

    // Absorb ALL the pieces from another group into this one, then delete the empty group
    public void MergeWithGroup(BlockGroup otherGroup)
    {
        // Copy the list first since we'll be modifying otherGroup while looping
        List<GameObject> otherPieces = new List<GameObject>(otherGroup.pieces);

        foreach (GameObject piece in otherPieces)
        {
            AddPiece(piece);
        }

        // The other group is now empty - destroy its leftover empty container
        Destroy(otherGroup.gameObject);
    }
}