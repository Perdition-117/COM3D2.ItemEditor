using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using I2.Loc;
using ItemEditor.Gui;
using UnityEngine;

namespace ItemEditor;

[BepInPlugin("dev.meido.com3d2.itemeditor", MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class ItemEditor : BaseUnityPlugin {
	private const int MessageBoxIconError = 0x0010;

	private enum ViewMode {
		None,
		Category,
		MaskSlots,
		UndressSlots,
	}

	private static readonly Dictionary<string, string> BodySlots = new(StringComparer.OrdinalIgnoreCase);
	private static MPN[] _itemSlots;

	private readonly GuiWindow _mainWindow;
	private readonly GuiWindow _itemWindow;
	private readonly Dictionary<ViewMode, ItemView> _itemViews = new();

	private ViewMode _currentView;
	private Vector2 _mainScrollPosition;
	private Vector2 _itemScrollPosition;

	private MenuItem _currentItem;

	private readonly ConfigEntry<string> _configExportPath;
	private readonly ConfigEntry<bool> _configShowBodySlots;

	private ItemEditor() {
		_configExportPath = Config.Bind("General", "ExportPath", "exports", "Path relative to the game directory where modified items will be exported");
		_configShowBodySlots = Config.Bind("General", "ShowBodySlots", false, "Include body slots in lists instead of only clothes");

		foreach (var item in Enum.GetNames(typeof(TBody.SlotID))) {
			BodySlots.Add(item, item);
		}

		AddView(ViewMode.Category, "Category", DrawItemWindowSlot);
		AddView(ViewMode.MaskSlots, "Masked slots", DrawItemWindowMask);
		AddView(ViewMode.UndressSlots, "Undressed slots", DrawItemWindowUndress);

		_mainWindow = new(MyPluginInfo.PLUGIN_NAME.GetHashCode(), DrawMainWindow, 600, 450, MyPluginInfo.PLUGIN_NAME);
		_itemWindow = new(MyPluginInfo.PLUGIN_NAME.GetHashCode() + 1, DrawItemWindow, 300, 400, MyPluginInfo.PLUGIN_NAME);
	}

	private void Update() {
		if (GameMain.Instance.GetNowSceneName() == "SceneEdit" && IsHotKeyDown()) {
			_mainWindow.Visible = !_mainWindow.Visible;

			_itemSlots ??= SceneEditInfo.m_dicPartsTypePair
				.Where(e => e.Value.m_eType == SceneEditInfo.CCateNameType.EType.Item)
				.OrderBy(e => e.Value.m_eMenuCate)
				.ThenBy(e => e.Value.m_nIdx)
				.Select(e => e.Key)
				.ToArray();
		}
	}

	private bool IsHotKeyDown() =>
		Input.GetKey(KeyCode.LeftControl) &&
		Input.GetKey(KeyCode.LeftShift) &&
		Input.GetKeyDown(KeyCode.E);

	private void OnGUI() {
		_mainWindow.Render();
		_itemWindow.Render();
	}

	private void ExportItem(string filename) {
		if (!Directory.Exists(_configExportPath.Value)) {
			Directory.CreateDirectory(_configExportPath.Value);
		}
		Logger.LogDebug($"Exporting {filename}...");
		_currentItem.Write(Path.Combine(_configExportPath.Value, filename));
	}

	private static string GetMaidPropLabel(MPN maidProp) {
		if (SceneEditInfo.m_dicPartsTypePair.TryGetValue(maidProp, out var categoryName)) {
			return LocalizationManager.GetTranslation("SceneEdit/カテゴリー/サブ/" + categoryName.m_strBtnPartsTypeName);
		} else {
			return null;
		}
	}

	private bool IsShownSlot(MPN mpn) {
		return _configShowBodySlots.Value || IsWearSlot(mpn);
	}

	private static bool IsWearSlot(MPN mpn) {
		return Mpn.WearStart <= mpn && mpn <= Mpn.WearEnd && mpn != Mpn.accha;
	}

	private void AddView(ViewMode viewMode, string title, Action renderFunction) {
		_itemViews.Add(viewMode, new() {
			Title = title,
			DrawContent = renderFunction,
		});
	}

	private void DrawMainWindow(int id) {
		GUILayout.Label("Equipped items", GuiSkin.BoxHeadingStyle);
		_mainScrollPosition = GUILayout.BeginScrollView(_mainScrollPosition, GuiSkin.HeadingBoxStyle);
		{
			var maid = SceneEdit.Instance.maid;
			if (maid?.body0) {
				foreach (var mpn in _itemSlots.Where(IsShownSlot)) {
					var prop = maid.GetProp(mpn);
					if (prop.strFileName.IsNullOrWhiteSpace() || MenuItem.IsDefaultItem(mpn, prop.strFileName)) {
						continue;
					}

					GUILayout.BeginHorizontal();
					{
						GUILayout.Label(GetMaidPropLabel(mpn), GUILayout.Width(130));
						GUILayout.Label(prop.strFileName);
						if (GUILayout.Button("Edit", GUILayout.Width(60))) {
							_currentItem = MenuItem.Deserialize(prop.strFileName);
							_currentView = ViewMode.None;
							_itemWindow.Visible = true;
						}
						GUI.enabled = true;
					}
					GUILayout.EndHorizontal();
				}
			}
			GUILayout.FlexibleSpace();
		}
		GUILayout.EndScrollView();

		GUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		if (GUILayout.Button("Close", GUILayout.Width(80))) {
			_mainWindow.Visible = false;
			_itemWindow.Visible = false;
		}
		GUILayout.EndHorizontal();
	}

	private void DrawItemWindow(int id) {
		GUILayout.Label(_currentItem.FileName, GuiSkin.HeadingStyle);

		if (_currentView == ViewMode.None) {
			DrawItemWindowMain();
		} else {
			var view = _itemViews[_currentView];
			GUILayout.Label(view.Title, GuiSkin.BoxHeadingStyle);
			_itemScrollPosition = GUILayout.BeginScrollView(_itemScrollPosition, GuiSkin.HeadingBoxStyle);
			view.DrawContent();
			GUILayout.FlexibleSpace();
			GUILayout.EndScrollView();

			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			if (GUILayout.Button("Back", GUILayout.Width(80))) {
				_currentView = ViewMode.None;
				_itemScrollPosition = Vector2.zero;
			}
			GUILayout.EndHorizontal();
		}
	}

	private void DrawItemWindowMain() {
		GUILayout.BeginVertical(GUI.skin.box);
		{
			GUILayout.BeginHorizontal();
			GUILayout.Label("Name", GUILayout.Width(80));
			_currentItem.Name = GUILayout.TextField(_currentItem.Name);
			GUILayout.EndHorizontal();

			var slotView = _itemViews[ViewMode.Category];
			GUILayout.BeginHorizontal();
			GUILayout.Label(slotView.Title, GUILayout.Width(80));
			if (GUILayout.Button(GetMaidPropLabel(_currentItem.Category), new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleLeft })) {
				_currentView = ViewMode.Category;
			}
			GUILayout.EndHorizontal();
		}
		GUILayout.EndVertical();

		DrawViewButton(ViewMode.MaskSlots);
		DrawViewButton(ViewMode.UndressSlots);

		void DrawViewButton(ViewMode viewMode) {
			var view = _itemViews[viewMode];
			if (GUILayout.Button(view.Title)) {
				_currentView = viewMode;
			}
		}

		GUILayout.FlexibleSpace();

		GUILayout.BeginHorizontal();
		{
			GUILayout.FlexibleSpace();

			if (GUILayout.Button("Export", GUILayout.Width(80))) {
				try {
					ExportItem(_currentItem.FileName);
				} catch (Exception e) {
					NUty.WinMessageBox(NUty.GetWindowHandle(), e.Message, MyPluginInfo.PLUGIN_NAME, MessageBoxIconError);
				}
			}

			if (GUILayout.Button("Close", GUILayout.Width(80))) {
				_itemWindow.Visible = false;
				_currentItem = null;
				_mainWindow.Focus();
			}
		}
		GUILayout.EndHorizontal();
	}

	private void DrawItemWindowSlot() {
		foreach (var mpn in _itemSlots.Where(IsShownSlot)) {
			if (GUILayout.Button(GetMaidPropLabel(mpn), new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleLeft })) {
				_currentItem.Category = mpn;
				_currentView = ViewMode.None;
				_itemScrollPosition = Vector2.zero;
				break;
			}
		}
	}

	private void DrawItemWindowMask() {
		foreach (var mpn in _itemSlots) {
			if (BodySlots.TryGetValue(mpn.ToString(), out var bodySlot)) {
				_currentItem.MaskedSlots.TryGetValue(bodySlot, out var isSelected);
				_currentItem.MaskedSlots[bodySlot] = GUILayout.Toggle(isSelected, GetMaidPropLabel(mpn));
			}
		}
	}

	private void DrawItemWindowUndress() {
		foreach (var mpn in _itemSlots.Where(IsShownSlot)) {
			_currentItem.UndressedSlots.TryGetValue(mpn, out var isSelected);
			_currentItem.UndressedSlots[mpn] = GUILayout.Toggle(isSelected, GetMaidPropLabel(mpn));
		}
	}

	class ItemView {
		public string Title { get; internal set; }
		public Action DrawContent { get; internal set; }
	}
}
