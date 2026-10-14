using System.Collections.Generic;
using Politicmon.Audio;
using Politicmon.Core;
using UnityEngine;

namespace Politicmon.UI
{
    /// <summary>
    /// The text box at the bottom of the screen: word wraps, paginates (3 lines per page),
    /// types the text letter by letter and waits for A / B / tap to continue.
    /// </summary>
    public class DialogBox : Modal
    {
        public const int LinesPerPage = 3;
        const float CharsPerSecond = 60f;
        const int PadX = 8;
        const int PadY = 7;

        PixelText label;
        PixelText arrow;
        bool typing;
        bool waiting;
        bool advance;

        public static int BoxHeight => LinesPerPage * PixelFont.LineHeight + PadY * 2 - 2;
        public float TextWidth => Rect.sizeDelta.x - PadX * 2;

        public override void Build(Vector2 area)
        {
            UIFactory.Panel(transform, 0, 0, area.x, BoxHeight).name = "Frame";
            label = UIFactory.Label(transform, "", PadX, PadY);
            label.rectTransform.sizeDelta = new Vector2(area.x - PadX * 2, BoxHeight - PadY);
            arrow = UIFactory.Label(transform, "▼", area.x - 13, BoxHeight - 10);
            arrow.enabled = false;
        }

        public void Layout(Vector2 area)
        {
            UIFactory.Place(Rect, 0, area.y - BoxHeight, area.x, BoxHeight);
            foreach (Transform child in transform)
                Destroy(child.gameObject);
            Build(area);
        }

        public List<string> Paginate(string text)
        {
            var lines = PixelFont.Wrap(text, TextWidth);
            var pages = new List<string>();
            for (int i = 0; i < lines.Count; i += LinesPerPage)
                pages.Add(string.Join("\n", lines.GetRange(i, Mathf.Min(LinesPerPage, lines.Count - i))));
            return pages;
        }

        public void ShowInstant(string text)
        {
            label.Text = string.Join("\n", PixelFont.Wrap(text, TextWidth));
            label.VisibleCharacters = -1;
            arrow.enabled = false;
        }

        /// <summary>Shows the text page by page. If waitLast is false the last page stays on screen.</summary>
        public async Awaitable Play(string text, bool waitLast)
        {
            var pages = Paginate(text);
            for (int i = 0; i < pages.Count; i++)
            {
                await TypePage(pages[i]);
                if (i < pages.Count - 1 || waitLast)
                    await WaitForAdvance();
            }
        }

        async Awaitable TypePage(string page)
        {
            label.Text = page;
            label.VisibleCharacters = 0;
            arrow.enabled = false;
            typing = true;
            advance = false;
            float shown = 0f;
            while (typing)
            {
                await Async.NextFrame();
                shown += Time.deltaTime * CharsPerSecond;
                label.VisibleCharacters = (int)shown;
                if (shown >= page.Length || advance)
                    typing = false;
            }
            label.VisibleCharacters = -1;
            advance = false;
        }

        async Awaitable WaitForAdvance()
        {
            waiting = true;
            advance = false;
            Touch();
            float blink = 0f;
            while (!advance)
            {
                blink += Time.deltaTime;
                arrow.enabled = blink % 0.8f < 0.5f;
                await Async.NextFrame();
            }
            waiting = false;
            arrow.enabled = false;
            AudioManager.Sfx("select");
        }

        protected override void HandleInput()
        {
            if ((typing || waiting) && (Pressed(GameButton.A) || Pressed(GameButton.B) || GameInput.Tapped(out _)))
                advance = true;
        }
    }
}
