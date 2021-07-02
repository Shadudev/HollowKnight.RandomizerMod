using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RandomizerMod.Extensions
{
    public class RandoMenuItem<T> where T : IEquatable<T>
    {
        public static readonly Color TRUE_COLOR = Color.Lerp(Color.white, Color.yellow, 0.5f);
        public static readonly Color FALSE_COLOR = Color.grey;
        public static readonly Color LOCKED_TRUE_COLOR = Color.Lerp(Color.grey, Color.yellow, 0.5f);
        public static readonly Color LOCKED_FALSE_COLOR = Color.Lerp(Color.grey, Color.black, 0.5f);

        public delegate void RandoMenuItemChanged(RandoMenuItem<T> item);

        private readonly FixVerticalAlign _align;
        private readonly T[] _selections;
        private readonly Text _text;
        private int _currentSelection;
        private bool _locked = false;

        public RandoMenuItem(MenuButton baseObj, Vector2 position, string name, params T[] values)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (baseObj == null)
            {
                throw new ArgumentNullException(nameof(baseObj));
            }

            if (values == null || values.Length == 0)
            {
                throw new ArgumentNullException(nameof(values));
            }

            _selections = values;
            Name = name;

            Button = baseObj.Clone(name + "Button", MenuButton.MenuButtonType.Activate, position, string.Empty);

            _text = Button.transform.Find("Text").GetComponent<Text>();
            _text.fontSize = 36;
            _align = Button.gameObject.GetComponentInChildren<FixVerticalAlign>(true);

            Button.ClearEvents();
            Button.AddEvent(EventTriggerType.Submit, GotoNext);

            RefreshText();
        }

        public T CurrentSelection => _selections[_currentSelection];

        public MenuButton Button { get; }

        public string Name { get; set; }

        public event RandoMenuItemChanged Changed
        {
            add => ChangedInternal += value;
            remove => ChangedInternal -= value;
        }

        private event RandoMenuItemChanged ChangedInternal;
        
        public void SetName(string n)
        {
            Name = n;
            RefreshText(false);
        }

        public void SetSelection(T obj)
        {
            if (_locked) return;

            for (int i = 0; i < _selections.Length; i++)
            {
                if (_selections[i].Equals(obj))
                {
                    _currentSelection = i;
                    break;
                }
            }

            RefreshText(false);
        }

        private void GotoNext(BaseEventData data = null)
        {
            if (_locked) return;

            _currentSelection++;
            if (_currentSelection >= _selections.Length)
            {
                _currentSelection = 0;
            }

            RefreshText();
        }

        private void GotoPrev(BaseEventData data = null)
        {
            if (_locked) return;

            _currentSelection--;
            if (_currentSelection < 0)
            {
                _currentSelection = _selections.Length - 1;
            }

            RefreshText();
        }

        private void RefreshText(bool invokeEvent = true)
        {
            if (typeof(T) == typeof(bool))
            {
                _text.text = Name;
            }
            else
            {
                _text.text = Name + ": " + _selections[_currentSelection];
            }

            _align.AlignText();
            SetColor();

            if (invokeEvent)
            {
                ChangedInternal?.Invoke(this);
            }
        }

        internal void SetColor(Color? c = null)
        {
            if (c is Color forceColor)
            {
                _text.color = forceColor;
                return;
            }

            if (!(_selections[_currentSelection] is bool value))
            {
                if (_locked)
                {
                    _text.color = LOCKED_FALSE_COLOR;
                }
                else
                {
                    _text.color = Color.white;
                }
                return;
            }

            if (!_locked && value)
            {
                _text.color = TRUE_COLOR;
            }
            else if (!_locked && !value)
            {
                _text.color = FALSE_COLOR;
            }
            else if (_locked && value)
            {
                _text.color = LOCKED_TRUE_COLOR;
            }
            else if (_locked && value)
            {
                _text.color = LOCKED_FALSE_COLOR;
            }
            else
            {
                _text.color = Color.red;
            }
        }

        internal Color GetColor()
        {
            return _text.color;
        }

        internal void Lock()
        {
            _locked = true;
            SetColor();
        }

        internal void Unlock()
        {
            _locked = false;
            SetColor();
        }
    }
}
