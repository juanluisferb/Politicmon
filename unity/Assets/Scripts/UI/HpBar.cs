using UnityEngine;
using UnityEngine.UI;

namespace Politicmon.UI
{
    /// <summary>A small PS (HP) or EXP bar: dark border, light background, colored fill.</summary>
    public class HpBar : MonoBehaviour
    {
        static readonly Color Good = UIFactory.Hex("#40b848");
        static readonly Color Mid = UIFactory.Hex("#e8b800");
        static readonly Color Low = UIFactory.Hex("#e04030");
        static readonly Color ExpColor = UIFactory.Hex("#4890e0");

        Image fill;
        float width;
        bool isExp;

        public static HpBar Create(Transform parent, float x, float y, float width, bool exp = false)
        {
            float height = exp ? 3 : 4;
            var root = UIFactory.Container(parent, exp ? "ExpBar" : "HpBar", x, y, width, height);
            UIFactory.Solid(root, UIFactory.TextColor, 0, 0, width, height);
            UIFactory.Solid(root, UIFactory.Paper, 1, 1, width - 2, height - 2);
            var bar = root.gameObject.AddComponent<HpBar>();
            bar.fill = UIFactory.Solid(root, Good, 1, 1, width - 2, height - 2);
            bar.width = width - 2;
            bar.isExp = exp;
            return bar;
        }

        public void SetRatio(float ratio)
        {
            ratio = Mathf.Clamp01(ratio);
            float w = Mathf.Floor(width * ratio);
            if (ratio > 0f)
                w = Mathf.Max(1f, w);
            fill.rectTransform.sizeDelta = new Vector2(w, fill.rectTransform.sizeDelta.y);
            fill.color = isExp ? ExpColor : ratio > 0.5f ? Good : ratio > 0.2f ? Mid : Low;
        }
    }
}
