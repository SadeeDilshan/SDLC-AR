using UnityEngine;
using UnityEngine.EventSystems;

public class DragRotateStage : MonoBehaviour
{
    public float rotationSpeed = 0.4f;
    private Vector2 lastMousePos;
    private bool isDragging = false;

    void Update()
    {
        // Touch rotation on mobile
        if (Input.touchCount == 1)
        {
            Touch t = Input.GetTouch(0);
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(t.fingerId)) return;
            if (t.phase == TouchPhase.Moved)
            {
                transform.Rotate(Vector3.up, -t.deltaPosition.x * rotationSpeed, Space.World);
            }
        }
        // Mouse drag rotation in Unity Editor
        else if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
            isDragging = true;
            lastMousePos = Input.mousePosition;
        }
        else if (Input.GetMouseButton(0) && isDragging)
        {
            Vector2 delta = (Vector2)Input.mousePosition - lastMousePos;
            transform.Rotate(Vector3.up, -delta.x * rotationSpeed, Space.World);
            lastMousePos = Input.mousePosition;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
        }
    }
}