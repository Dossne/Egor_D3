using UnityEngine;

namespace FarmMergerBattle.Utils
{
    public static class PlaceholderSpriteFactory
    {
        public static Sprite Create(Color color)
        {
            var tex = new Texture2D(32, 32);
            var pixels = new Color[32 * 32];
            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        }
    }
}
