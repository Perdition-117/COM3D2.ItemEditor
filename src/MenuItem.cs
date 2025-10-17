using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ItemEditor;

internal class MenuItem {
	private const string FileHeader = "CM3D2_MENU";

	private static readonly Dictionary<MPN, string> DefaultItems = new() {
		[Mpn.nose] = "nose_del_i_.menu",
		[Mpn.facegloss] = "facegloss_del_i_.menu",
	};

	private int _version;
	private string _path;

	public string FileName { get; set; }
	public string Name { get; set; }
	private string CategoryName { get; set; }
	public MPN Category { get; set; }
	public string Description { get; set; }

	private List<KeyValuePair<string, List<string>>> Properties { get; } = new();
	public Dictionary<string, bool> MaskedSlots { get; } = new();
	public Dictionary<MPN, bool> UndressedSlots { get; } = new();

	public static MenuItem Deserialize(string fileName) {
		using var file = GameUty.FileOpen(fileName);
		using var stream = new MemoryStream(file.ReadAll());
		using var reader = new BinaryReader(stream);

		var menuItem = new MenuItem();
		menuItem.FileName = fileName;
		menuItem.Deserialize(reader);

		foreach (var property in menuItem.Properties) {
			var values = property.Value;
			if (values.Count > 0) {
				var value = values[0];
				switch (property.Key) {
					case PropertyNames.Name:
						menuItem.Name = value;
						break;
					case PropertyNames.Description:
						menuItem.Description = value;
						break;
					case PropertyNames.Category:
						menuItem.CategoryName = value.ToLower();
						menuItem.Category = Mpn.Parse(menuItem.CategoryName);
						break;
					case PropertyNames.MaskItem:
						menuItem.MaskedSlots.Add(value, true);
						break;
					case PropertyNames.Item:
						if (TryGetDefaultItem(value, out var mpn)) {
							menuItem.UndressedSlots.Add(mpn, true);
						}
						break;
				}
			}
		}

		return menuItem;
	}

	private void Deserialize(BinaryReader reader) {
		var header = reader.ReadString();
		if (header != FileHeader) {
			throw new Exception($"Invalid header \"{header}\" for {nameof(MenuItem)}. Expected \"{FileHeader}\".");
		}

		_version = reader.ReadInt32();

		_path = reader.ReadString();
		Name = reader.ReadString();
		CategoryName = reader.ReadString();
		Description = reader.ReadString();
		reader.ReadInt32();

		while (true) {
			var numStrings = reader.ReadByte();
			if (numStrings == 0) {
				break;
			}
			var values = new List<string>();
			for (var i = 0; i < numStrings; i++) {
				values.Add(reader.ReadString());
			}
			var key = values[0];
			if (key == "end") {
				break;
			}
			values.RemoveAt(0);
			Properties.Add(new(key, values));
		}
	}

	private void Serialize(BinaryWriter writer) {
		writer.Write(FileHeader);
		writer.Write(_version);

		var newCategory = Category.ToString();

		writer.Write(_path);
		writer.Write(Name);
		writer.Write(newCategory);
		writer.Write(Description);

		using var memoryStream = new MemoryStream();
		using var binaryWriter = new BinaryWriter(memoryStream);

		int FindLastIndex(string key) => Properties.FindLastIndex(e => e.Key == key) + 1;

		var lastIndexMaskItem = FindLastIndex(PropertyNames.MaskItem);

		foreach (var item in MaskedSlots.Where(e => e.Value && !HasProperty(PropertyNames.MaskItem, e.Key))) {
			if (item.Key.Equals(Mpn.chikubi.ToString(), StringComparison.OrdinalIgnoreCase)) {
				AddProperties(lastIndexMaskItem, PropertyNames.MaskItem, item.Key, "accNipL", "accNipR");
			} else {
				AddProperty(lastIndexMaskItem, PropertyNames.MaskItem, item.Key);
			}
		}

		var lastIndexRemoveItem = FindLastIndex(PropertyNames.Item);

		foreach (var item in UndressedSlots.Where(e => e.Value)) {
			if (TryGetDefaultItem(item.Key, out var defaultItem) && !HasProperty(PropertyNames.Item, defaultItem)) {
				AddProperty(lastIndexRemoveItem, PropertyNames.Item, defaultItem);
			}
		}

		foreach (var property in Properties) {
			var values = property.Value;
			if (values.Count > 0) {
				var value = values[0];
				switch (property.Key) {
					case PropertyNames.Name:
						values[0] = Name.Replace(' ', '\u2008');
						break;
					case PropertyNames.Description:
						values[0] = Description;
						break;
					case PropertyNames.Category:
						values[0] = newCategory;
						break;
					case PropertyNames.MaskItem:
						if (!IsMaskedSlot(value)) {
							continue;
						}
						if (value is "accNipL" or "accNipR" && !IsMaskedSlot(Mpn.chikubi.ToString())) {
							continue;
						}
						break;
					case PropertyNames.Item:
						if (TryGetDefaultItem(value, out var mpn) && !IsUndressedSlot(mpn)) {
							continue;
						}
						// if item slot was changed, make sure the new slot is not undressed
						if (TryGetDefaultItem(Category, out var defaultItem) && value.Equals(defaultItem, StringComparison.OrdinalIgnoreCase)) {
							continue;
						}
						break;
					default:
						for (var i = 0; i < values.Count; i++) {
							if (values[i].Equals(CategoryName, StringComparison.OrdinalIgnoreCase)) {
								values[i] = newCategory;
							}
						}
						break;
				}
			}

			binaryWriter.Write((byte)(property.Value.Count + 1));
			binaryWriter.Write(property.Key);
			foreach (var propertyValue in property.Value) {
				binaryWriter.Write(propertyValue);
			}
		}

		if (!Properties.Exists(e => e.Key == "end")) {
			binaryWriter.Write((byte)0);
		}

		writer.Write((int)memoryStream.Length);
		memoryStream.WriteTo(writer.BaseStream);

		CategoryName = newCategory;
	}

	public void Write(string path) {
		using var fileStream = new FileStream(path, FileMode.Create);
		using var binaryWriter = new BinaryWriter(fileStream);
		Serialize(binaryWriter);
	}

	private void AddProperty(int index, string key, string value) {
		var property = new KeyValuePair<string, List<string>>(key, new() { value });
		if (index > 0) {
			Properties.Insert(index, property);
		} else {
			Properties.Add(property);
		}
	}

	private void AddProperties(int index, string key, params string[] values) {
		var properties = values.Select(e => new KeyValuePair<string, List<string>>(key, new() { e }));
		if (index > 0) {
			Properties.InsertRange(index, properties);
		} else {
			Properties.AddRange(properties);
		}
	}

	private bool HasProperty(string key, string value) {
		return Properties.Any(e => e.Key == key && e.Value[0] == value);
	}

	private bool IsMaskedSlot(string value) {
		return MaskedSlots.TryGetValue(value, out var isMasked) && isMasked;
	}

	private bool IsUndressedSlot(MPN value) {
		return UndressedSlots.TryGetValue(value, out var isUndressed) && isUndressed;
	}

	public static bool IsDefaultItem(MPN mpn, string menuFileName) {
		return TryGetDefaultItem(mpn, out var defaultItem) && menuFileName.Equals(defaultItem, StringComparison.OrdinalIgnoreCase);
	}

	private static bool TryGetDefaultItem(MPN mpn, out string defaultFileName) {
		return CM3.dicDelItem.TryGetValue(mpn, out defaultFileName) || DefaultItems.TryGetValue(mpn, out defaultFileName);
	}

	private static bool TryGetDefaultItem(string defaultItem, out MPN mpn) {
		var item = CM3.dicDelItem.FirstOrDefault(e => e.Value.Equals(defaultItem, StringComparison.OrdinalIgnoreCase));
		if (item.Key == default) {
			item = DefaultItems.FirstOrDefault(e => e.Value.Equals(defaultItem, StringComparison.OrdinalIgnoreCase));
		}
		mpn = item.Key;
		return item.Key != default;
	}

	private static class PropertyNames {
		public const string Name = "name";
		public const string Description = "setumei";
		public const string Category = "category";
		public const string MaskItem = "maskitem";
		public const string Item = "アイテム";
	}
}
