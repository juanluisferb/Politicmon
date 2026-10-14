using Politicmon.Core;
using UnityEngine;

namespace Politicmon.UI
{
    /// <summary>
    /// Base class for anything that takes over the input while it is open (dialogs, menus, screens).
    /// Only the top-most modal of <see cref="UIManager"/> reads input. Subclasses call Finish(result).
    /// </summary>
    public abstract class Modal : MonoBehaviour
    {
        const float InputDelay = 0.12f;

        AwaitableCompletionSource<int> completion;
        float openedAt;

        public RectTransform Rect => (RectTransform)transform;
        public bool IsOpen => completion != null;

        /// <summary>Build the UI. Called once by UIManager when the modal is shown.</summary>
        public abstract void Build(Vector2 area);

        /// <summary>Read input here; only called while this modal is on top.</summary>
        protected abstract void HandleInput();

        public Awaitable<int> Open()
        {
            completion = new AwaitableCompletionSource<int>();
            openedAt = Time.unscaledTime;
            return completion.Awaitable;
        }

        /// <summary>Resets the input delay so the key that opened us is not read twice.</summary>
        public void Touch() => openedAt = Time.unscaledTime;

        protected void Finish(int result)
        {
            var c = completion;
            completion = null;
            c?.SetResult(result);
        }

        void Update()
        {
            if (completion == null || !UIManager.IsTop(this) || Time.unscaledTime - openedAt < InputDelay)
                return;
            HandleInput();
        }

        // Small helpers so subclasses read like plain game logic.
        protected static bool Pressed(GameButton b) => GameInput.Pressed(b);
        protected static bool Repeated(GameButton b) => GameInput.Repeated(b);

        /// <summary>A tap this frame converted to this modal's local coordinates (top-left origin).</summary>
        protected bool Tapped(out Vector2 local)
        {
            local = Vector2.zero;
            if (!GameInput.Tapped(out var logical))
                return false;
            local = UIManager.LogicalToRoot(logical) - UIFactory.PositionOf(Rect);
            return true;
        }
    }
}
