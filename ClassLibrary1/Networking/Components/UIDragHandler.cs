using UnityEngine;
using UnityEngine.EventSystems;

public class UIDragHandler : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    [SerializeField] public RectTransform target;

    private Vector2 offset;

    private void Awake()
    {
        if (target == null)
            target = GetComponent<RectTransform>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        RectTransform parent = target.parent as RectTransform;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, eventData.position, eventData.pressEventCamera, out Vector2 localMousePosition);
        offset = target.anchoredPosition - localMousePosition;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (target == null)
            return;

        RectTransform parent = target.parent as RectTransform;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, eventData.position, eventData.pressEventCamera, out Vector2 localMousePosition))
        {
            Vector2 newPos = localMousePosition + offset;

            // Clamp to parent/canvas bounds
            RectTransform canvasRect = GameScreenManager.Instance.ssOverlayCanvas.GetComponent<RectTransform>();

            float halfWidth = target.rect.width * 0.5f;
            float halfHeight = target.rect.height * 0.5f;

            float leftLimit = -canvasRect.rect.width * 0.5f + halfWidth;
            float rightLimit = canvasRect.rect.width * 0.5f - halfWidth;
            float bottomLimit = -canvasRect.rect.height * 0.5f + halfHeight;
            float topLimit = canvasRect.rect.height * 0.5f - halfHeight;

            newPos.x = Mathf.Clamp(newPos.x, leftLimit, rightLimit);
            newPos.y = Mathf.Clamp(newPos.y, bottomLimit, topLimit);

            target.anchoredPosition = newPos;
        }
    }

}
