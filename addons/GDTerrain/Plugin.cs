#if TOOLS

using Godot;

namespace GDTerrain {

	[Tool]
	public partial class Plugin : EditorPlugin {

		private Terrain _targetTerrain;
		private Terrain TargetTerrain {
			set {
				if (_targetTerrain == value) {
					DebugLog($"{nameof(TargetTerrain)}->set->same");
					return;
				}

				DebugLog($"{nameof(TargetTerrain)}->set");

				// old
				if (_targetTerrain != null) {
					_targetTerrain.TreeExited -= OnTerrainExitedScene;
				}

				_targetTerrain = value;

				// new
				if (_targetTerrain != null) {
					_targetTerrain.TreeExited += OnTerrainExitedScene;
				}

				_inspector.SetTargetTerrain(_targetTerrain);
				_terrainPainter.SetTargetTerrain(_targetTerrain);
				// brush decal
				_brushDecal.TargetTerrain = _targetTerrain;

				UpdateToolbarMenuAvailability();
			}
		}

		#region GUI
		private Inspector _inspector;
		private HBoxContainer _toolbar;
		private MenuButton _menuButton;
		private PopupMenu _menuPopup;
		private PopupMenu _debugMenu;

		#region Toolbar Menu Ids
		public const int
			MENU_RESIZE = 0,
			MENU_BAKE = 1,
			MENU_UPDATE_EDITOR_COLLIDER = 2,
			MENU_DEBUG = 3;
		#endregion

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
			for (int i = 0; i < _menuPopup.ItemCount; i++) {
				if (available) {
					_menuPopup.SetItemDisabled(i, false);
					_menuPopup.SetItemTooltip(i, string.Empty);
				} else {
					_menuPopup.SetItemDisabled(i, true);
					_menuPopup.SetItemTooltip(i, "Terrain has no data");
				}
			}
		}

		private void InitGUI() {
			_inspector = ResourceLoader.Load<PackedScene>(BASE_PATH + "Tools/inspector.tscn").Instantiate<Inspector>();
			//Util.ApplyDPIScale(_inspector, dPIScale);
			_inspector.Visible = false;
			AddControlToContainer(CustomControlContainer.SpatialEditorBottom, _inspector);
			_inspector.SetTerrainPainter(_terrainPainter);
			//_inspector.CallDeferred("SetTerrainPainter", _terrainPainter);
			//_inspector.CallDeferred("SetupDialogs", baseControl);
			//_inspector.SetUndoRedo(GetUndoRedo());
			//_inspector.SetImageCache(_imageCache);

			InitToolbar();
		}

		private void DisposeToolbar() {
			RemoveControlFromContainer(CustomControlContainer.SpatialEditorMenu, _toolbar);
			_toolbar.QueueFree();
			_toolbar = null;
		}

		private void DisposeGUI() {
			DisposeToolbar();

			RemoveControlFromContainer(CustomControlContainer.SpatialEditorBottom, _inspector);
			_inspector.QueueFree();
			_inspector = null;
		}
		#endregion

		private TerrainPainter _terrainPainter;
		private BrushDecal _brushDecal;

		public override void _EnterTree() {
			DebugLog($"{nameof(GDTerrain)}->{nameof(_EnterTree)}");

			// custom types
			AddCustomType(nameof(Terrain), nameof(Node3D), LoadScript("Terrain.cs"), LoadIcon("heightmap_node"));
			AddCustomType(nameof(TerrainData), nameof(Resource), LoadScript("TerrainData.cs"), LoadIcon("heightmap_data"));

			_terrainPainter = new() {
				BrushSize = 20
			};
			_terrainPainter.Brush.sizeChanged += value => _brushDecal.Size = value;
			AddChild(_terrainPainter);

			_brushDecal = new() {
				Size = _terrainPainter.BrushSize
			};

			// GUI
			InitGUI();
		}

		public override void _ExitTree() {
			DebugLog($"{nameof(GDTerrain)}->{nameof(_ExitTree)}");

			TargetTerrain = null;

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
			long afterGUIInput = (long)AfterGUIInput.Pass;

			if (_targetTerrain == null || !_targetTerrain.HasData) {
				return afterGUIInput;
			}

			if (@event is InputEventMouseMotion mouse) {
				Vector2i? gridPos = GetGridPos(mouse.Position, camera);
				if (gridPos.HasValue) {
					_brushDecal.SetPosition(new(gridPos.Value.x, 0, gridPos.Value.y));

					if (Input.IsMouseButtonPressed(MouseButton.Left)) {
						_ = _terrainPainter.TryPaint((Vector2)gridPos, mouse.Pressure);
						afterGUIInput = (long)AfterGUIInput.Stop;
					}
				}
				_brushDecal.UpdateVisibility();//?
			} else {
				_targetTerrain.UpdateViewerPosition(camera);
				//_inspector.SetCameraTransform(camera.GlobalTransform);
			}

			return afterGUIInput;
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
			TargetTerrain = null;
		}
		#endregion

		public override void _Edit(Variant @object) {
			TargetTerrain = GetTerrainFromObject(@object);
		}

		public override void _MakeVisible(bool value) {
			_inspector.Visible = value;
			_toolbar.Visible = value;

			// brush decal
			_brushDecal.UpdateVisibility();

			if (!value) {
				TargetTerrain = null;
			}
		}
	}
}
#endif
