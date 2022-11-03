#if TOOLS

using Godot;

namespace GDTerrain {

	[Tool]
	public partial class Plugin : EditorPlugin {

		private Terrain _targetTerrain;

		#region GUI
		private HBoxContainer _toolbar;
		private MenuButton _menuButton;
		private PopupMenu _menuPopup;
		private PopupMenu _debugMenu;

		#region Callbacks
		private void OnDebugMenuAboutToPopup() {
		}

		private void OnDebugMenuIdPressed(long id) {
		}

		private void OnMenuIdPressed(long id) {
		}
		#endregion

		private void InitToolbar() {
			_toolbar = new();
			AddControlToContainer(CustomControlContainer.SpatialEditorMenu, _toolbar);
			_toolbar.Visible = false;

			// menu
			_menuButton = new() {
				Text = "Terrain"
			};
			_menuPopup = _menuButton.GetPopup();
			_menuPopup.AddItem("Resize", MENU_RESIZE);
			_menuPopup.AddItem("Bake", MENU_BAKE);
			_menuPopup.AddSeparator();
			_menuPopup.AddItem("Update Editor Collider", MENU_UPDATE_EDITOR_COLLIDER);
			_menuPopup.AddSeparator();
			_debugMenu = new() {
				Name = "Debug Menu"
			};
			_debugMenu.AboutToPopup += OnDebugMenuAboutToPopup;
			_debugMenu.IdPressed += OnDebugMenuIdPressed;
			_menuPopup.AddChild(_debugMenu);
			_menuPopup.AddSubmenuItem("Debug", _debugMenu.Name, MENU_DEBUG);
			_menuPopup.IdPressed += OnMenuIdPressed;
			_toolbar.AddChild(_menuButton);

			_toolbar.AddChild(new VSeparator());
		}

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

		private void InitGUI() {
			InitToolbar();
		}

		private void DisposeToolbar() {
			RemoveControlFromContainer(CustomControlContainer.SpatialEditorMenu, _toolbar);
			_toolbar.QueueFree();
			_toolbar = null;
		}

		private void DisposeGUI() {
			DisposeToolbar();
		}
		#endregion

		private readonly BrushDecal _brushDecal = new();

		#region Toolbar Menu Ids
		public const int
			MENU_RESIZE = 0,
			MENU_BAKE = 1,
			MENU_UPDATE_EDITOR_COLLIDER = 2,
			MENU_DEBUG = 3;
		#endregion

		public override void _EnterTree() {
			DebugLog($"{nameof(GDTerrain)}->{nameof(_EnterTree)}");

			// custom types
			AddCustomType(nameof(Terrain), nameof(Node3D), LoadScript("Terrain.cs"), LoadIcon("heightmap_node"));
			AddCustomType(nameof(TerrainData), nameof(Resource), LoadScript("TerrainData.cs"), LoadIcon("heightmap_data"));

			// GUI
			InitGUI();

			_brushDecal.SetSize(20);
		}

		public override void _ExitTree() {
			DebugLog($"{nameof(GDTerrain)}->{nameof(_ExitTree)}");

			SetTargetTerrain(null);

			// GUI
			DisposeGUI();

			// custom types
			RemoveCustomType(nameof(TerrainData));
			RemoveCustomType(nameof(Terrain));
		}

		public override bool _Handles(Variant @object) {
			return GetTerrainFromObject(@object) != null;
		}

		public override long _Forward3dGuiInput(Camera3D camera, InputEvent @event) {
			if (_targetTerrain == null || !_targetTerrain.HasData) {
				return (long)AfterGUIInput.Pass;
			}

			if (@event is InputEventMouseMotion mouse) {
				Vector2i? gridPos = GetGridPos(mouse.Position, camera);
				if (gridPos.HasValue) {
					_brushDecal.SetPosition(new(gridPos.Value.x, 0, gridPos.Value.y));
				}
			} else {
				_targetTerrain.UpdateViewerPosition(camera);
			}

			return (long)AfterGUIInput.Pass;
		}

		/// <summary>Prev frame</summary>
		private bool _terrainHadData;

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

		#region Target Terrain
		private void OnTerrainExitedScene() {
			DebugLog($"{nameof(OnTerrainExitedScene)}");
			SetTargetTerrain(null);
		}

		public void SetTargetTerrain(Terrain value) {
			DebugLog($"{nameof(SetTargetTerrain)}");

			// old
			if (_targetTerrain != null) {
				_targetTerrain.TreeExited -= OnTerrainExitedScene;
			}

			_targetTerrain = value;

			// new
			if (_targetTerrain != null) {
				_targetTerrain.TreeExited += OnTerrainExitedScene;
			}

			// brush decal
			_brushDecal.TargetTerrain = _targetTerrain;

			UpdateToolbarMenuAvailability();
		}
		#endregion

		public override void _Edit(Variant @object) {
			SetTargetTerrain(GetTerrainFromObject(@object));
		}

		public override void _MakeVisible(bool value) {
			_toolbar.Visible = value;

			// brush decal
			_brushDecal.UpdateVisibility();

			if (!value) {
				SetTargetTerrain(null);
			}
		}
	}
}
#endif
