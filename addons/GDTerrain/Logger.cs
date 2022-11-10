#define DEBUG

using Godot;

namespace GDTerrain {

	public static class Logger {

		public static void DebugLog(string text) {
#if DEBUG
			GD.Print(text);
#endif
		}

		public static void DebugLogError(string text) {
#if DEBUG
			GD.PrintErr(text);
#endif
		}
	}
}
