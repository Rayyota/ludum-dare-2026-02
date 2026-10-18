using UnityEngine;
using UnityEngine.UI;

namespace Tamagotchi
{
    [RequireComponent(typeof(Image))]
    public sealed class PetSpriteAnimation : MonoBehaviour
    {
        [SerializeField] private Sprite[] frames;
        [SerializeField] private float[] frameDurations;
        private Image image;
        private int frame;
        private float elapsed;

        private void OnEnable()
        {
            image = GetComponent<Image>();
            frame = 0;
            elapsed = 0;
            ShowFrame();
        }

        private void Update()
        {
            if (frames == null || frames.Length < 2) return;
            elapsed += Time.deltaTime;
            while (elapsed >= Duration(frame))
            {
                elapsed -= Duration(frame);
                frame = (frame + 1) % frames.Length;
                ShowFrame();
            }
        }

        private float Duration(int index) => frameDurations != null && index < frameDurations.Length
            ? Mathf.Max(0.01f, frameDurations[index]) : 0.1f;

        private void ShowFrame()
        {
            if (frames != null && frames.Length > 0) image.sprite = frames[frame];
        }
    }
}
