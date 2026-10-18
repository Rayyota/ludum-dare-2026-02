using UnityEngine;
using UnityEngine.UI;

namespace Tamagotchi
{
    [ExecuteAlways]
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class HeartIcon : MaskableGraphic
    {
        [SerializeField] private bool filled = true;
        [SerializeField] private Color fullColor = new Color32(240, 137, 132, 255);
        [SerializeField] private Color emptyColor = new Color32(67, 81, 81, 255);
        [SerializeField] private Sprite fullSprite;
        [SerializeField] private Sprite emptySprite;

        private Sprite CurrentSprite => filled ? fullSprite : emptySprite;
        public override Texture mainTexture => CurrentSprite != null ? CurrentSprite.texture : base.mainTexture;

        public bool Filled
        {
            get => filled;
            set
            {
                if (filled == value) return;
                filled = value;
                SetMaterialDirty();
                SetVerticesDirty();
            }
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            var rect = GetPixelAdjustedRect();
            if (CurrentSprite != null)
            {
                var uv = UnityEngine.Sprites.DataUtility.GetOuterUV(CurrentSprite);
                mesh.AddVert(new Vector2(rect.xMin, rect.yMin), color, new Vector2(uv.x, uv.y));
                mesh.AddVert(new Vector2(rect.xMin, rect.yMax), color, new Vector2(uv.x, uv.w));
                mesh.AddVert(new Vector2(rect.xMax, rect.yMax), color, new Vector2(uv.z, uv.w));
                mesh.AddVert(new Vector2(rect.xMax, rect.yMin), color, new Vector2(uv.z, uv.y));
                mesh.AddTriangle(0, 1, 2);
                mesh.AddTriangle(2, 3, 0);
                return;
            }
            const int steps = 48;
            var tint = (filled ? fullColor : emptyColor) * color;
            mesh.AddVert(rect.center, tint, Vector2.zero);
            for (int i = 0; i <= steps; i++)
            {
                float angle = Mathf.PI * 2f * i / steps;
                float x = 16f * Mathf.Pow(Mathf.Sin(angle), 3);
                float y = 13f * Mathf.Cos(angle) - 5f * Mathf.Cos(2 * angle) -
                          2f * Mathf.Cos(3 * angle) - Mathf.Cos(4 * angle);
                var point = new Vector2(rect.xMin + (x + 18f) / 36f * rect.width,
                    rect.yMin + (y + 19f) / 34f * rect.height);
                mesh.AddVert(point, tint, Vector2.zero);
                if (i > 0) mesh.AddTriangle(0, i, i + 1);
            }
        }
    }
}
