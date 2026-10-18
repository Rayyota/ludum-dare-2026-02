
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class DynamicCable : MonoBehaviour
{
    [Header("Точки крепления")]
    [SerializeField] private RectTransform startPoint;
    [SerializeField] private RectTransform endPoint;

    [Header("Внешний вид")]
    [SerializeField] private float thickness = 12f;

    private RectTransform cable;
    private RectTransform parentRect;

    private void Awake()
    {
        cable = GetComponent<RectTransform>();
        parentRect = cable.parent as RectTransform;

        Image image = GetComponent<Image>();

        if (image != null)
            image.raycastTarget = false;
    }

    private void LateUpdate()
    {
        if (startPoint == null || endPoint == null)
            return;

        Vector2 start = parentRect.InverseTransformPoint(
            startPoint.position
        );

        Vector2 end = parentRect.InverseTransformPoint(
            endPoint.position
        );

        Vector2 direction = end - start;

        // Кабель находится между двумя точками
        cable.anchoredPosition = (start + end) / 2f;

        // Кабель растягивается
        cable.sizeDelta = new Vector2(
            direction.magnitude,
            thickness
        );

        // Кабель поворачивается
        float angle = Mathf.Atan2(
            direction.y,
            direction.x
        ) * Mathf.Rad2Deg;

        cable.localRotation =
            Quaternion.Euler(0f, 0f, angle);
    }
}

