using System;

namespace ItemEditor;

internal static class Mpn {
	public static readonly MPN nose = Parse("nose");
	public static readonly MPN facegloss = Parse("facegloss");
	public static readonly MPN chikubi = Parse("chikubi");
	public static readonly MPN accha = Parse("accha");

	public static readonly MPN WearStart = (MPN)Enum.Parse(typeof(MPN_TYPE_RANGE), "WEAR_START");
	public static readonly MPN WearEnd = (MPN)Enum.Parse(typeof(MPN_TYPE_RANGE), "WEAR_END");

	public static MPN Parse(string name) => (MPN)Enum.Parse(typeof(MPN), name);
}
