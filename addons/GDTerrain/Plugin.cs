#if TOOLS

using Godot;

namespace GDTerrain {

	[Tool]
	public partial class Plugin : EditorPlugin {

		private Terrain _targetTerrain;
		private Terrain TargetTerrain {
			set {
				// same?
				if (_targetTerrain == value) {
					Logger.DebugLog($"{nameof(TargetTerrain)}->set->same");
					return;
				}

				Logger.DebugLog($"{nameof(TargetTerrain)}->set");

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
				TerrainPainter.SetTargetTerrain(_targetTerrain);
				BrushDecal.TargetTerrain = _targetTerrain;

				UpdateToolbarMenuAvailability();
			}
		}

		private TerrainPainter TerrainPainter { get; set; }
		private BrushDecal BrushDecal { get; set; }
		private bool _mousePressed;
		private bool _pendingPaintCommit;

		public override void _EnterTree() {
			Logger.DebugLog($"{nameof(GDTerrain)}->{nameof(_EnterTree)}");

			// custom types
			AddCustomType(nameof(Terrain), nameof(Node3D), LoadScript("Terrain.cs"), LoadIcon("heightmap_node"));
			AddCustomType(nameof(TerrainData), nameof(Resource), LoadScript("TerrainData.cs"), LoadIcon("heightmap_data"));

			TerrainPainter = new() {
				BrushSize = 5
			};
			TerrainPainter.Brush.SizeChanged += value => BrushDecal.Size = value;
			AddChild(TerrainPainter);

			BrushDecal = new() {
				Size = TerrainPainter.BrushSize
			};

			// GUI
			InitGUI();
		}

		public override void _ExitTree() {
			Logger.DebugLog($"{nameof(GDTerrain)}->{nameof(_ExitTree)}");

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

		/// <summary>Prev frame</summary>
		private bool _terrainHadData;

		public override void _Process(double delta) {
			if (_targetTerrain == null) {
				return;
			}

			bool hasData = _targetTerrain.HasData;

			if (_pendingPaintCommit) {
				_pendingPaintCommit = false;
				if (hasData && !TerrainPainter.PendingOperation && TerrainPainter.HasModifiedChunks) {
					TerrainPainter.Commit();
				}
			}

			if (hasData != _terrainHadData) {
				_terrainHadData = hasData;
				UpdateToolbarMenuAvailability();
			}
		}

		#region Target Terrain
		private void OnTerrainExitedScene() {
			Logger.DebugLog($"{nameof(OnTerrainExitedScene)}");
			TargetTerrain = null;
		}
		#endregion

		public override void _Edit(Variant @object) {
			TargetTerrain = GetTerrainFromObject(@object);
		}

		public override void _MakeVisible(bool value) {
			_inspector.Visible = value;
			_toolbar.Visible = value;
			BrushDecal.UpdateVisibility();

			if (!value) {
				TargetTerrain = null;
			}
		}
	}
}
#endif
