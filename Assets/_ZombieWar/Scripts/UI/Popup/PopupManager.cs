using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ZombieWar.UI
{
    public sealed class PopupManager : MonoBehaviour
    {
        private const string LogPrefix = "[Popup]";

        [SerializeField] private PopupBackdrop _backdrop;
        [SerializeField] private Canvas _canvas;
        // Every popup that can ever open, authored inactive under this layer.
        [SerializeField] private List<PopupBase> _popups = new List<PopupBase>(4);

        private readonly List<PopupBase> _stack = new List<PopupBase>(4);

        public PopupBase Top => _stack.Count > 0 ? _stack[_stack.Count - 1] : null;
        public bool HasOpenPopup => _stack.Count > 0;

        private void Awake()
        {
            if (_backdrop == null || _canvas == null)
            {
                Debug.LogError($"{LogPrefix} PopupManager has an unassigned reference.", this);
                return;
            }

            for (int i = 0; i < _popups.Count; i++)
            {
                PopupBase popup = _popups[i];
                if (popup == null)
                {
                    Debug.LogError($"{LogPrefix} Empty slot {i} in the popup list.", this);
                    continue;
                }

                if (popup.transform.parent != transform)
                {
                    Debug.LogError($"{LogPrefix} {popup.name} is not a child of the layer - draw order will be wrong.", popup);
                }

                popup.gameObject.SetActive(false);
                popup.BindManager(this);
            }

            _canvas.enabled = false;
        }

        private void Update()
        {
            if (_stack.Count == 0)
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;
            // Android maps its hardware back button onto Escape in the Input System.
            if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame)
            {
                return;
            }

            PopupBase top = Top;
            if (top.CloseOnBackKey)
            {
                Close(top);
            }
        }

        public void Show(PopupBase popup)
        {
            if (popup == null || _stack.Contains(popup))
            {
                return;
            }

            _canvas.enabled = true;
            _stack.Add(popup);
            popup.PlayShow();
            Restack();
        }

        public void Close(PopupBase popup)
        {
            if (popup == null || !_stack.Remove(popup))
            {
                return;
            }

            popup.PlayClose();
            Restack();
        }

        public void CloseTop()
        {
            PopupBase top = Top;
            if (top != null)
            {
                Close(top);
            }
        }

        public void HandleBackdropClicked()
        {
            PopupBase top = Top;
            if (top != null && top.CloseOnBackdropClick)
            {
                Close(top);
            }
        }

        // Sibling order is draw order: replay the stack bottom-up and slip the shared
        // backdrop in just below the topmost popup that asked for dimming.
        private void Restack()
        {
            int topDimmer = -1;
            for (int i = 0; i < _stack.Count; i++)
            {
                if (_stack[i].DimBackground)
                {
                    topDimmer = i;
                }
            }

            if (topDimmer < 0)
            {
                _backdrop.Hide();
            }
            else
            {
                _backdrop.Show();
            }

            for (int i = 0; i < _stack.Count; i++)
            {
                if (i == topDimmer)
                {
                    _backdrop.transform.SetAsLastSibling();
                }

                _stack[i].transform.SetAsLastSibling();
            }

            if (_stack.Count == 0)
            {
                _canvas.enabled = false;
            }
        }
    }
}
