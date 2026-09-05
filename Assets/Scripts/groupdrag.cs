using UnityEngine;

// Just like SimpleDrag, but this one drags an ENTIRE glued-together family at once
public class GroupDrag : MonoBehaviour
{
    private bool isBeingDragged = false;
    private Vector3 offset;

    void Update()
    {
        if (Camera.main == null) return;

        Vector3 rawMouse = Input.mousePosition;
        if (float.IsInfinity(rawMouse.x) || float.IsInfinity(rawMouse.y) ||
            float.IsNaN(rawMouse.x) || float.IsNaN(rawMouse.y))
        {
            return;
        }

        Vector3 screenPos = rawMouse;
        screenPos.z = Mathf.Abs(Camera.main.transform.position.z);
        Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(screenPos);
        mouseWorldPosition.z = 0;

        if (Input.GetMouseButtonDown(0))
        {
            Collider2D hit = Physics2D.OverlapPoint(mouseWorldPosition);
            // Check if we clicked on THIS group OR any child piece belonging to it
            if (hit != null && hit.transform.IsChildOf(this.transform))
            {
                isBeingDragged = true;
                offset = transform.position - mouseWorldPosition;
            }
        }

        if (isBeingDragged)
        {
            Vector3 targetPosition = mouseWorldPosition + offset;
            transform.position = ClampToFrame(targetPosition);
        }

        if (Input.GetMouseButtonUp(0))
        {
            if (isBeingDragged)
            {
                isBeingDragged = false;
                // When we let go of a group, check if it can snap to any other piece/group too!
                SnapChecker.CheckForSnap(this.gameObject);
            }
        }
    }

    // Keeps a position from going outside the puzzle frame's boundaries
    private Vector3 ClampToFrame(Vector3 position)
    {
        if (!ResponsivePuzzleFitter.FrameIsReady) return position;

        float halfWidth = ResponsivePuzzleFitter.FrameWidth / 2f;
        float halfHeight = ResponsivePuzzleFitter.FrameHeight / 2f;
        Vector3 center = ResponsivePuzzleFitter.FrameCenter;

        float clampedX = Mathf.Clamp(position.x, center.x - halfWidth, center.x + halfWidth);
        float clampedY = Mathf.Clamp(position.y, center.y - halfHeight, center.y + halfHeight);

        return new Vector3(clampedX, clampedY, position.z);
    }
}