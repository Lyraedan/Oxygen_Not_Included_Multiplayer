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
        RectTransform parent = target.parent as RectTransform;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, eventData.position, eventData.pressEventCamera, out Vector2 localMousePosition))
        {
            target.anchoredPosition = localMousePosition + offset;
        }
    }
}
