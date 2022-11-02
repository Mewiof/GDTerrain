#if TOOLS
#define DEBUG

using Godot;

namespace SimpleTerrain {

	[Tool]
	public partial class Plugin : EditorPlugin {

		private Terrain _targetTerrain;

		#region Misc
		public const string BASE_PATH = "res://addons/SimpleTerrain/";

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
			return GD.Load<Script>(BASE_PATH + path);
		}

		/// <summary>
		/// 'Icons/icon_<paramref name="name"/>.svg'
		/// </summary>
		private static Texture2D LoadIcon(string name) {
			return GD.Load<Texture2D>(BASE_PATH + "Icons/icon_" + name + ".svg");
		}
		#endregion

		#region GUI
		private HBoxContainer _toolbar;
		private MenuButton _menuButton;
		private PopupMenu _debugMenu;
		#endregion

		#region Brush Decal
		private BrushDecal _brushDecal = new();
		#endregion

		#region Toolbar Menu Ids
		public const int
			MENU_RESIZE = 0,
			MENU_BAKE = 1,
			MENU_UPDATE_EDITOR_COLLIDER = 2,
			MENU_DEBUG = 3;
		#endregion

		public Plugin() : base() {
			_brushDecal.SetSize(20);
		}

		public override void _EnterTree() {
			DebugLog($"{nameof(SimpleTerrain)}->{nameof(_EnterTree)}");

			// custom types
			AddCustomType(nameof(Terrain), nameof(Node3D), LoadScript("Terrain.cs"), LoadIcon("heightmap_node"));
			AddCustomType(nameof(TerrainData), nameof(Resource), LoadScript("TerrainData.cs"), LoadIcon("heightmap_data"));

			// components
			_toolbar = new();
			AddControlToContainer(CustomControlContainer.SpatialEditorMenu, _toolbar);
			_toolbar.Hide();

			// menu
			MenuButton menu = new() {
				Text = "Terrain"
			};
			PopupMenu menuPopup = menu.GetPopup();
			menuPopup.AddItem("Resize", MENU_RESIZE);
			menuPopup.AddItem("Bake", MENU_BAKE);
			menuPopup.AddSeparator();
			menuPopup.AddItem("Update Editor Collider", MENU_UPDATE_EDITOR_COLLIDER);
			menuPopup.AddSeparator();
			_debugMenu = new() {
				Name = "Debug Menu"
			};
			_debugMenu.AboutToPopup += OnDebugMenuAboutToPopup;
			_debugMenu.IdPressed += OnDebugMenuIdPressed;
			menuPopup.AddChild(_debugMenu);
			menuPopup.AddSubmenuItem("Debug", _debugMenu.Name, MENU_DEBUG);
			menuPopup.IdPressed += OnMenuIdPressed;
			_toolbar.AddChild(menu);
			_menuButton = menu;

			_toolbar.AddChild(new VSeparator());
		}

		public override void _ExitTree() {
			DebugLog($"{nameof(SimpleTerrain)}->{nameof(_ExitTree)}");

			CustomEdit(null);

			// components
			RemoveControlFromContainer(CustomControlContainer.SpatialEditorMenu, _toolbar);
			_toolbar.QueueFree();
			_toolbar = null;

			// custom types
			RemoveCustomType(nameof(Terrain));
			RemoveCustomType(nameof(TerrainData));
		}

		/// <summary>Prev frame</summary>
		private bool _terrainHadData;

		private void UpdateToolbarMenuAvailability() {
			bool available = _targetTerrain != null && _targetTerrain.HasData;
			PopupMenu menuPopup = _menuButton.GetPopup();
			for (int i = 0; i < menuPopup.ItemCount; i++) {
				if (available) {
					menuPopup.SetItemDisabled(i, false);
					menuPopup.SetItemTooltip(i, string.Empty);
				} else {
					menuPopup.SetItemDisabled(i, true);
					menuPopup.SetItemTooltip(i, "Terrain has no data");
				}
			}
		}

		public override bool _Handles(Variant @object) {
			return GetTerrainFromObject(@object) != null;
		}

		// Update viewer position
		public override long _Forward3dGuiInput(Camera3D viewportCamera, InputEvent @event) {
			if (_targetTerrain == null || !_targetTerrain.HasData) {
				return (long)AfterGUIInput.Pass;
			}

			if (@event is InputEventMouseMotion inputMotion) {
				Vector2i? gridPos = GetGridPos(inputMotion.Position, viewportCamera);
				if (gridPos.HasValue) {
					_brushDecal.SetPosition(new(gridPos.Value.x, 0, gridPos.Value.y));
				}

				// This is needed in case the data or textures change during the user's editing of the terrain
				_brushDecal.UpdateVisibility();
			} else {
				_targetTerrain.UpdateViewerPosition(viewportCamera);
			}

			return (long)AfterGUIInput.Pass;
		}

		public override void _Process(double delta) {
			if (_targetTerrain == null) {
				return;
			}

			bool hasData = _targetTerrain.HasData;

			if (hasData != _terrainHadData) {
				_terrainHadData = hasData;
				UpdateToolbarMenuAvailability();
			}
		}

		#region GUI Callbacks
		private void OnDebugMenuAboutToPopup() {
		}

		private void OnDebugMenuIdPressed(long id) {
		}

		private void OnMenuIdPressed(long id) {
		}
		#endregion

		private static Terrain GetTerrainFromObject(Variant o) {
			if (o.VariantType == Variant.Type.Object && o.AsGodotObject() is Node3D node) {
				if (!node.IsInsideTree()) {
					return null;
				}
				if (node is Terrain result) {
					return result;
				}
			}
			return null;
		}

		private void OnTerrainExitedScene() {
			DebugLog($"{nameof(SimpleTerrain)}->{nameof(OnTerrainExitedScene)}");
			CustomEdit(null);
		}

		private void CustomEdit(Terrain newTargetTerrain) {
			// old
			if (_targetTerrain != null) {
				_targetTerrain.TreeExited -= OnTerrainExitedScene;
			}

			_targetTerrain = newTargetTerrain;

			// new
			if (_targetTerrain != null) {
				_targetTerrain.TreeExited += OnTerrainExitedScene;
			}

			// brush decal
			_brushDecal.SetTargetTerrain(newTargetTerrain);

			UpdateToolbarMenuAvailability();
		}

		public override void _Edit(Variant @object) {
			CustomEdit(GetTerrainFromObject(@object));
		}

		public override void _MakeVisible(bool visible) {
			_toolbar.Visible = visible;
			// brush decal
			_brushDecal.UpdateVisibility();
			if (!visible) {
				CustomEdit(null);
			}
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
