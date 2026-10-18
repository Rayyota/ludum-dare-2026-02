using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Tamagotchi
{
    [RequireComponent(typeof(Button))]
    public sealed class ButtonClickSound : MonoBehaviour, IPointerDownHandler, ISubmitHandler
    {
        [SerializeField] private AudioSource source;

        public void OnPointerDown(PointerEventData data)
        {
            if (data.button == PointerEventData.InputButton.Left) Play();
        }

        public void OnSubmit(BaseEventData data) => Play();

        private void Play()
        {
            var button = GetComponent<Button>();
            if (button.IsInteractable() && source != null && source.clip != null)
                source.PlayOneShot(source.clip);
        }
    }
}
