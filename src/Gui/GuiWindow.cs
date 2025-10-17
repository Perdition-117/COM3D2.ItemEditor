using UnityEngine;

namespace ItemEditor.Gui;

internal class GuiWindow {
	private readonly int _id;
	private readonly GUI.WindowFunction _renderFunction;
	private readonly Vector2 _size;
	private readonly string _title;

	private Rect _rect;
	private Rect _dragRect = new(0, 0, 0, 40);
	private bool _visible;
	private bool _setFocus;

	public GuiWindow(int id, GUI.WindowFunction renderFunction, int width, int height, string title) {
		_id = id;
		_renderFunction = renderFunction;
		_size = new(width, height);
		_title = title;
	}

	public bool Visible {
		get => _visible;
		set {
			if (value) {
				_setFocus = true;
			}
			_visible = value;
		}
	}

	public void Render() {
		if (!_visible) return;

		if (_rect == default) {
			_rect = GetRect(_size);
		}

		var originalSkin = GUI.skin;
		GUI.skin = GuiSkin.Skin;
		_rect = GUILayout.Window(_id, _rect, Render, GUIContent.none);
		GUI.skin = originalSkin;

		if (_setFocus) {
			_setFocus = false;
			GUI.FocusWindow(_id);
			GUI.BringWindowToFront(_id);
		}

		// intercept mouse input
		// Event.current.isMouse/isScrollWheel does not intercept GUI control input
		// Input.GetMouseButtonUp does not intercept mouse drag input
		if (MouseIsOver()) {
			var isMouseButtonDown = Input.GetMouseButton(0) || Input.GetMouseButton(1) || Input.GetMouseButton(2);
			if (isMouseButtonDown || Input.mouseScrollDelta != Vector2.zero) {
				Input.ResetInputAxes();
			}
		}
	}

	private void Render(int id) {
		_dragRect.width = _rect.width;
		GUILayout.Box(_title, GuiSkin.TitleBarStyle);
		GUI.DragWindow(_dragRect);
		_renderFunction(id);
	}

	private static Rect GetRect(Vector2 windowSize) {
		var windowPosition = (new Vector2(Screen.width, Screen.height) - windowSize) / 2;
		return new(windowPosition, windowSize);
	}

	public void Focus() => _setFocus = true;

	private bool MouseIsOver() => _visible && _rect.Contains(Event.current.mousePosition);
}
