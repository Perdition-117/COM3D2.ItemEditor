using System.Linq;
using UnityEngine;

namespace ItemEditor.Gui;

internal static class GuiSkin {
	private const float BackgroundBrightness = 0.1f;
	private const float ForegroundBrightness = 0.35f;
	private const float ContainerBrightness = 0.25f;
	private static readonly Color TitleBarColor = new(0, 0.4f, 0.8f, 0.5f);
	private static readonly Color BoxHeadingColor = GrayscaleColor(0.5f, 0.5f);
	private static readonly Color TextFieldColor = GrayscaleColor(0, 0.5f);
	private static readonly Color ToggleMarkColor = GrayscaleColor(1, 0.7f);
	private const int ToggleMarkInset = 3;

	public static GUISkin Skin { get; }
	public static GUIStyle TitleBarStyle { get; }
	public static GUIStyle HeadingStyle { get; }
	public static GUIStyle BoxHeadingStyle { get; }
	public static GUIStyle HeadingBoxStyle { get; }

	static GuiSkin() {
		Skin = Object.Instantiate(GUI.skin);

		var foregroundColor = GrayscaleColor(ForegroundBrightness);
		var background = new Background(BackgroundBrightness);
		var foreground = new Background(ForegroundBrightness);
		var container = new Background(ContainerBrightness);
		var toggleBackground = new Background(ToggleMarkColor, foregroundColor, ToggleMarkInset);

		var padding = new RectOffset(6, 6, 6, 6);
		var margin = new RectOffset(4, 4, 4, 4);

		Skin.font = Resources.FindObjectsOfTypeAll<Font>().FirstOrDefault(o => o.name == "NotoSansCJKjp-DemiLight");
		Skin.settings.selectionColor = new(0.2f, 0.6f, 1, 0.7f);
		Skin.settings.cursorFlashSpeed = 1;

		var backgroundStyle = new GUIStyle() {
			normal = CreateGuiStyleState(background.Inactive),
			onNormal = CreateGuiStyleState(background.Active),
		};

		var buttonStyle = new GUIStyle() {
			normal = CreateGuiStyleState(foreground.Inactive),
			hover = CreateGuiStyleState(foreground.Hover),
			active = CreateGuiStyleState(foreground.Pressed),
		};

		Skin.window = new(backgroundStyle) {
			padding = padding,
			stretchWidth = false,
		};

		Skin.box = new() {
			margin = margin,
			normal = CreateGuiStyleState(container.Inactive),
		};

		Skin.verticalScrollbar = new(backgroundStyle) {
			fixedWidth = 10,
		};

		Skin.verticalScrollbarThumb = buttonStyle;

		Skin.label = new(Skin.label) {
			padding = new(4, 4, 6, 6),
			normal = new() { textColor = Color.white },
			wordWrap = false,
		};

		Skin.textField = new() {
			padding = new(8, 8, 6, 6),
			margin = margin,
			fixedHeight = 28,
			alignment = TextAnchor.MiddleLeft,
			clipping = TextClipping.Clip,
			normal = CreateGuiStyleState(TextFieldColor),
		};

		Skin.button = new(buttonStyle) {
			padding = new(8, 8, 6, 6),
			margin = margin,
			alignment = TextAnchor.MiddleCenter,
		};

		Skin.toggle = new(buttonStyle) {
			margin = new(6, 6, 8, 8),
			border = new(ToggleMarkInset, ToggleMarkInset, ToggleMarkInset, ToggleMarkInset),
			fixedHeight = 16,
			fixedWidth = 16,
			contentOffset = new(24, 0),
			alignment = TextAnchor.MiddleLeft,
			onNormal = CreateGuiStyleState(toggleBackground.Inactive),
			onHover = CreateGuiStyleState(toggleBackground.Hover),
			onActive = CreateGuiStyleState(toggleBackground.Pressed),
		};

		TitleBarStyle = new(Skin.label) {
			alignment = TextAnchor.MiddleCenter,
			normal = CreateGuiStyleState(TitleBarColor),
		};

		HeadingStyle = new(Skin.label) {
			alignment = TextAnchor.MiddleCenter,
		};

		BoxHeadingStyle = new(TitleBarStyle) {
			margin = new(margin.left, margin.right, margin.top, 0),
			normal = CreateGuiStyleState(BoxHeadingColor),
		};

		HeadingBoxStyle = new(Skin.box) {
			margin = new(margin.left, margin.right, 0, margin.bottom),
		};
	}

	private static Color GrayscaleColor(float brightness, float alpha = 1) => new(brightness, brightness, brightness, alpha);

	private static GUIStyleState CreateGuiStyleState(Texture2D texture) => new() {
		background = texture,
		textColor = Color.white,
	};

	private static GUIStyleState CreateGuiStyleState(Color textureColor) => CreateGuiStyleState(Background.CreateTexture(textureColor));
}
