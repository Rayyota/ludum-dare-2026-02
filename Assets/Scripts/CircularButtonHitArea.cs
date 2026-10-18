using UnityEngine;

namespace Tamagotchi
{
    public sealed class CircularButtonHitArea : MonoBehaviour, ICanvasRaycastFilter
    {
        public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
        {
            var transformRect = (RectTransform)transform;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(transformRect, screenPoint, eventCamera, out var point))
                return false;
            var rect = transformRect.rect;
            if (rect.width <= 0 || rect.height <= 0) return false;
            point -= rect.center;
            var normalized = new Vector2(point.x * 2 / rect.width, point.y * 2 / rect.height);
            return normalized.sqrMagnitude <= 1;
        }
    }
}
