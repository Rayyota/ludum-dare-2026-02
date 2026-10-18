
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ContactDrag : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Точка правильного разъёма")]
    [SerializeField] private RectTransform targetSnapPoint;

    [Header("Точка на подвижном штекере")]
    [SerializeField] private RectTransform plugSnapPoint;

    [Header("Настройки")]
    [SerializeField] private float snapDistance = 32f;

    private RectTransform rect;
    private RectTransform parentRect;
    private ContactMatchingGame game;

    private Vector2 startPosition;
    private Vector2 pointerOffset;
    private bool connected;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        parentRect = rect.parent as RectTransform;
    }

    public void Initialize(ContactMatchingGame manager)
    {
        game = manager;
        connected = false;
    }

    public Vector2 GetPosition()
    {
        return rect.anchoredPosition;
    }

    public void ResetContact(Vector2 position)
    {
        connected = false;
        startPosition = position;
        rect.anchoredPosition = position;
        var image = GetComponent<Image>();
        if (image != null) image.raycastTarget = true;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!CanDrag())
            return;

        if (GetPointerPosition(eventData, out Vector2 pos))
        {
            pointerOffset = rect.anchoredPosition - pos;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!CanDrag())
            return;

        if (GetPointerPosition(eventData, out Vector2 pos))
        {
            rect.anchoredPosition = pos + pointerOffset;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!CanDrag())
            return;

        if (targetSnapPoint == null || plugSnapPoint == null)
        {
            Debug.LogError(
                "Не назначены точки соединения!",
                this
            );

            rect.anchoredPosition = startPosition;
            return;
        }

        Vector3 targetLocal =
            parentRect.InverseTransformPoint(
                targetSnapPoint.position
            );

        Vector3 plugLocal =
            parentRect.InverseTransformPoint(
                plugSnapPoint.position
            );

        float distance = Vector2.Distance(
            new Vector2(targetLocal.x, targetLocal.y),
            new Vector2(plugLocal.x, plugLocal.y)
        );

        if (distance <= snapDistance)
        {
            Vector3 offset =
                targetSnapPoint.position - plugSnapPoint.position;

            rect.position += offset;

            connected = true;

            Image image = GetComponent<Image>();
            if (image != null)
                image.raycastTarget = false;

            game.ContactConnected();
        }
        else
        {
            rect.anchoredPosition = startPosition;
        }
    }

    private bool CanDrag()
    {
        return !connected &&
               game != null &&
               !game.IsGameEnded;
    }

    private bool GetPointerPosition(
        PointerEventData eventData,
        out Vector2 position)
    {
        return RectTransformUtility
            .ScreenPointToLocalPointInRectangle(
                parentRect,
                eventData.position,
                eventData.pressEventCamera,
                out position
            );
    }
}
