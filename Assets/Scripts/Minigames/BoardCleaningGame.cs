using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Tamagotchi.Minigames
{
    public sealed class BoardCleaningGame : RepairChallenge, IPointerDownHandler, IPointerUpHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private RectTransform board;
        [SerializeField] private Image[] dirt;
        [SerializeField, Min(1)] private float scrubDistance = 100;
        private float[] cleaned;
        private bool dragging;
        private Vector2 previous;

        protected override void Prepare()
        {
            dragging = false;
            cleaned = new float[dirt.Length];
            foreach (var patch in dirt)
            {
                patch.enabled = true;
                patch.color = Color.white;
            }
        }

        public void OnPointerDown(PointerEventData data)
        {
            if (!IsRunning || data.button != PointerEventData.InputButton.Left) return;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(board, data.position, data.pressEventCamera, out previous))
                dragging = board.rect.Contains(previous);
        }

        public void OnBeginDrag(PointerEventData data)
        {
            if (!dragging) OnPointerDown(data);
        }

        public void OnDrag(PointerEventData data)
        {
            if (!IsRunning || !dragging || data.button != PointerEventData.InputButton.Left) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(board, data.position, data.pressEventCamera, out var point)) return;
            Scrub(previous, point);
            previous = point;
        }

        private void Scrub(Vector2 from, Vector2 to)
        {
            float distance = Vector2.Distance(from, to);
            if (distance < 0.1f) return;
            int steps = Mathf.Max(1, Mathf.CeilToInt(distance / 8));
            float strength = Mathf.Min(distance, 240) / steps / Mathf.Max(1, scrubDistance);
            for (int step = 1; step <= steps; step++)
            {
                var sample = Vector2.Lerp(from, to, step / (float)steps);
                if (!board.rect.Contains(sample)) continue;
                var world = board.TransformPoint(sample);
                for (int i = 0; i < dirt.Length; i++)
                {
                    if (cleaned[i] >= 1) continue;
                    var rect = dirt[i].rectTransform;
                    if (!rect.rect.Contains(rect.InverseTransformPoint(world))) continue;
                    cleaned[i] = Mathf.Min(1, cleaned[i] + strength);
                    dirt[i].color = new Color(1, 1, 1, 1 - cleaned[i]);
                    if (cleaned[i] >= 1) dirt[i].enabled = false;
                }
            }
            for (int i = 0; i < cleaned.Length; i++) if (cleaned[i] < 1) return;
            dragging = false;
            Finish(true);
        }

        public void OnPointerUp(PointerEventData data) => dragging = false;
        public void OnEndDrag(PointerEventData data) => dragging = false;
    }
}
