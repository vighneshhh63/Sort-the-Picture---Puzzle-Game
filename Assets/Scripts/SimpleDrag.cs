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
        // Safety check: if there's no camera tagged MainCamera, stop here
        if (Camera.main == null) return;

        // Safety check: sometimes mouse position briefly reports weird/infinite values
        // (like when clicking outside the Game window) - skip this frame if so
        Vector3 rawMouse = Input.mousePosition;
        if (float.IsInfinity(rawMouse.x) || float.IsInfinity(rawMouse.y) ||
            float.IsNaN(rawMouse.x) || float.IsNaN(rawMouse.y))
        {
            return;
        }

        // Step 1: Where is the mouse right now, in the game world?
        Vector3 screenPos = rawMouse;
        screenPos.z = Mathf.Abs(Camera.main.transform.position.z); // distance from camera to the 2D plane
        Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(screenPos);
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

        // Step 3: If we ARE being dragged, follow the mouse (but stay inside the frame!)
        if (isBeingDragged)
        {
            Vector3 targetPosition = mouseWorldPosition + offset;
            transform.position = ClampToFrame(targetPosition);
        }

        // Step 4: Did the player let go of the mouse button?
        if (Input.GetMouseButtonUp(0))
        {
            if (isBeingDragged)
            {
                isBeingDragged = false;
                // Just dropped it - check if it can glue to a neighbor!
                SnapChecker.CheckForSnap(this.gameObject);
            }
        }
    }

    // Keeps a position from going outside the puzzle frame's boundaries
    private Vector3 ClampToFrame(Vector3 position)
    {
        if (!ResponsivePuzzleFitter.FrameIsReady) return position; // frame not ready yet, allow free movement

        float halfWidth = ResponsivePuzzleFitter.FrameWidth / 2f;
        float halfHeight = ResponsivePuzzleFitter.FrameHeight / 2f;
        Vector3 center = ResponsivePuzzleFitter.FrameCenter;

        float clampedX = Mathf.Clamp(position.x, center.x - halfWidth, center.x + halfWidth);
        float clampedY = Mathf.Clamp(position.y, center.y - halfHeight, center.y + halfHeight);

        return new Vector3(clampedX, clampedY, position.z);
    }
}