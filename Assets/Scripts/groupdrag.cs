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
            transform.position = mouseWorldPosition + offset;
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
}