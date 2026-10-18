using UnityEngine;
using UnityEngine.EventSystems;

namespace Tamagotchi.Minigames
{
    public sealed class TimingHitArea : MonoBehaviour, IPointerDownHandler
    {
        [SerializeField] private TimingMinigame game;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left && game != null)
                game.AttemptHit();
        }
    }
}
