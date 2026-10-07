using UnityEngine;

namespace BurgerIdle.UI
{
    /// <summary>
    /// 런타임 생성 UI용 스프라이트. 이미지 에셋 없이 도형으로 그린다.
    /// </summary>
    public static class UiSprites
    {
        static Sprite circle;

        public static Sprite Circle => circle != null ? circle : circle = CreateCircle(128);

        static Sprite CreateCircle(int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[size * size];
            var center = (size - 1) * 0.5f;
            var radius = size * 0.5f;

            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                var alpha = Mathf.Clamp01(radius - distance); // 가장자리 1px 안티앨리어싱
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255));
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
