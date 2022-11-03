#if TOOLS
#define DEBUG

using Godot;

namespace GDTerrain {

	public partial class Plugin : EditorPlugin {

		public const string BASE_PATH = "res://addons/GDTerrain/";

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

		private static Script LoadScript(string path) {
			return ResourceLoader.Load<Script>(BASE_PATH + path);
		}

		/// <summary>'Icons/icon_<paramref name="name"/>.svg'</summary>
		private static Texture2D LoadIcon(string name) {
			return ResourceLoader.Load<Texture2D>(BASE_PATH + "Icons/icon_" + name + ".svg");
		}

		private static Terrain GetTerrainFromObject(Variant o) {
			if (o.VariantType == Variant.Type.Object && o.Obj is Node3D node) {
				if (!node.IsInsideTree()) {
					return null;
				}
				if (node is Terrain result) {
					return result;
				}
			}
			return null;
		}

		private Vector2i? GetGridPos(Vector2 mousePos, Camera3D camera) {
			SubViewport viewport = (SubViewport)camera.GetViewport();
			SubViewport viewportContainer = camera.GetParent<SubViewport>();
			Vector2 screenPos = mousePos * viewport.Size / viewportContainer.Size;

			Vector3 origin = camera.ProjectRayOrigin(screenPos);
			Vector3 dir = camera.ProjectRayNormal(screenPos);

			float rayDistance = camera.Far * 1.2f;
			return _targetTerrain.CellRaycast(origin, dir, rayDistance);
		}
	}
}
#endif
