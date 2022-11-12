#if TOOLS

using System.Collections.Generic;
using Godot;

namespace GDTerrain {

	public partial class Plugin : EditorPlugin {

		// Parent
		// Children

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

		private void OnModeSelected(TerrainPainter.Mode mode) {
			TerrainPainter.mode = mode;
			_inspector.SetBrushEditorPaintMode(mode);
		}
		#endregion

		private readonly List<Button> _modeButtons = new();

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

			ButtonGroup modeButtonGroup = new();

			static Texture2D GetModeButtonIcon(TerrainPainter.Mode mode) {
				return LoadIcon(mode.ToString().ToLower());
			}

			foreach (TerrainPainter.Mode mode in (TerrainPainter.Mode[])System.Enum.GetValues(typeof(TerrainPainter.Mode))) {
				Button button = new() {
					Icon = GetModeButtonIcon(mode),
					TooltipText = mode.ToString(),
					ToggleMode = true,
					ButtonGroup = modeButtonGroup,
					CustomMinimumSize = new(28f, 28f),
					IconAlignment = HorizontalAlignment.Center,
					ExpandIcon = true,
					Flat = true
				};

				if (mode == TerrainPainter.mode) {
					button.ButtonPressed = true;
				}

				button.Pressed += () => OnModeSelected(mode);
				_toolbar.AddChild(button);
				_modeButtons.Add(button);
			}
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
			_inspector.Visible = false;
			AddControlToContainer(CustomControlContainer.SpatialEditorBottom, _inspector);

			_inspector.SetTerrainPainter(TerrainPainter);

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
	}
}
#endif
