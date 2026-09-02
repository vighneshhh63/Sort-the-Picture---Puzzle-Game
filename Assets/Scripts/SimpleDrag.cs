using UnityEngine;

// This is our block's "brain"
// It says: "when the mouse grabs me, follow the mouse until it lets go"
public class SimpleDrag : MonoBehaviour
{
    private bool isBeingDragged = false; // Is someone holding me right now?
    private Vector3 offset;              // Remembers WHERE on the block you clicked

    // This runs EVERY SINGLE FRAME (like, 60 times per second!)
    void Update()
    {
        // Step 1: Where is the mouse right now, in the game world?
        Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPosition.z = 0; // keep it flat, like a 2D picture

        // Step 2: Did the player just click the mouse button?
        if (Input.GetMouseButtonDown(0))
        {
            // Is the mouse actually touching THIS block? (invisible bubble check)
            Collider2D hit = Physics2D.OverlapPoint(mouseWorldPosition);
            if (hit != null && hit.gameObject == this.gameObject)
            {
                isBeingDragged = true;
                offset = transform.position - mouseWorldPosition;
            }
        }

        // Step 3: If we ARE being dragged, follow the mouse
        if (isBeingDragged)
        {
            transform.position = mouseWorldPosition + offset;
        }

        // Step 4: Did the player let go of the mouse button?
        if (Input.GetMouseButtonUp(0))
        {
            isBeingDragged = false;
        }
    }
}