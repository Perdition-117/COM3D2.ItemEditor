using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ItemEditor.Gui;

internal class Background {
	private static readonly Color InactiveMod = new(1, 1, 1, 0.5f);
	private static readonly Color HoverMod = new(0.05f, 0.05f, 0.05f);
	private static readonly Color PressedMod = new(0.15f, 0.15f, 0.15f);

	// hack to prevent garbage collecting textures
	private static readonly HashSet<Texture2D> TextureCache = new();

	public Background(Color insetColor, Color baseColor = default, int borderSize = 0) {
		Active = CreateTexture(insetColor, baseColor, borderSize);
		Inactive = CreateTexture(insetColor, baseColor * InactiveMod, borderSize);
		Hover = CreateTexture(insetColor, baseColor + HoverMod, borderSize);
		Pressed = CreateTexture(insetColor, baseColor + PressedMod, borderSize);
	}

	public Background(Color baseColor) {
		Active = CreateTexture(baseColor);
		Inactive = CreateTexture(baseColor * InactiveMod);
		Hover = CreateTexture(baseColor + HoverMod);
		Pressed = CreateTexture(baseColor + PressedMod);
	}

	public Background(float brightness) : this(new Color(brightness, brightness, brightness)) { }

	public Texture2D Active { get; }
	public Texture2D Inactive { get; }
	public Texture2D Hover { get; }
	public Texture2D Pressed { get; }

	public static Texture2D CreateTexture(Color color, Color borderColor = default, int borderSize = 0) {
		int textureSize = borderSize * 2 + 1;
		var texture = new Texture2D(textureSize, textureSize, TextureFormat.ARGB32, false);
		texture.filterMode = FilterMode.Point;
		if (borderSize > 0) {
			texture.SetPixels(Enumerable.Repeat(borderColor, textureSize * textureSize).ToArray());
		}
		texture.SetPixel(borderSize, borderSize, color);
		texture.Apply();
		TextureCache.Add(texture);
		return texture;
	}
}
