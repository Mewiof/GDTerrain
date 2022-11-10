using System.Runtime.CompilerServices;
using Godot;
using GDTerrain.Native;
using System;

namespace GDTerrain {

	[Tool]
	public partial class Terrain : Node3D {

		private readonly Mesher _mesher = new();

		#region QuadTreeLOD
		private readonly QuadTreeLOD _lodder = new();

		[Export(PropertyHint.Range, "2,5,1")]
		public float LODScale { // TODO: float?
			get => _lodder.SplitScale;
			set => _lodder.SplitScale = value;
		}
		#endregion

		#region Data
		private TerrainData _data;

		private void OnDataRegionChanged(int x, int y, int w, int h, int mapTypeIndex) {
			if (mapTypeIndex == TerrainData.MAP_HEIGHT) {
				SetAreaDirty(x, y, w, h);
			}
		}

		private void OnDataMapChanged(int typeIndex, int index) {
			if (typeIndex == TerrainData.MAP_DETAIL ||
				typeIndex == TerrainData.MAP_HEIGHT ||
				typeIndex == TerrainData.MAP_NORMAL ||
				typeIndex == TerrainData.MAP_GLOBAL_ALBEDO) {
				// TODO
			}

			if (typeIndex != TerrainData.MAP_DETAIL) {
				_dirty = true;
			}
		}

		private void OnDataMapAdded(int arg1, int arg2) {
		}
		private void OnDataMapRemoved(int arg1, int arg2) {
		}

		public TerrainData Data {
			get {
				if (_data == null || string.IsNullOrEmpty(_data.ResourcePath)) {
					return null;
				}
				return _data;
			}
			set {
				if (_data == value) {
					Logger.DebugLog($"[{nameof(Terrain)}] Same '{nameof(_data)}'. Ignoring...");
					return;
				}

				Logger.DebugLog($"[{nameof(Terrain)}] Setting new '{nameof(_data)}'");

				// old
				if (_data != null) {
					Logger.DebugLog($"[{nameof(Terrain)}] Disconnecting old '{nameof(TerrainData)}'...");
					_data.ResolutionChanged -= ResetChunks;
					_data.RegionChanged -= OnDataRegionChanged;
					_data.MapChanged -= OnDataMapChanged;
					//_data.mapAdded -= OnDataMapAdded;
					//_data.mapRemoved -= OnDataMapRemoved;
				}

				_data = value;

				// the order of these two is important
				ClearAllChunks();

				// new
				if (_data != null) {
					Logger.DebugLog($"[{nameof(Terrain)}] Connecting new '{nameof(TerrainData)}'...");

					_data.ResolutionChanged += ResetChunks;
					_data.RegionChanged += OnDataRegionChanged;
					_data.MapChanged += OnDataMapChanged;
					//_data.mapAdded += OnDataMapAdded;
					//_data.mapRemoved += OnDataMapRemoved;

					ResetChunks();
				}

				_dirty = true;

				UpdateConfigurationWarnings();

				Logger.DebugLog($"[{nameof(Terrain)}] '{nameof(_data)}' has been set");
			}
		}

		public bool HasData {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _data != null;
		}

		[Export(PropertyHint.Dir)]
		public string DataDirectory {
			get {
				if (HasData) {
					return _data.ResourcePath.GetBaseDir();
				}
				return string.Empty;
			}
			set {
				// same?
				if (DataDirectory == value) {
					return;
				}

				// empty?
				if (string.IsNullOrWhiteSpace(value)) {
					Data = null;
					return;
				}

				string fPath = value + '/' + TerrainData.DEFAULT_FILENAME + '.' + TerrainData.EXTENSION;

				// exists?
				if (ResourceLoader.Exists(fPath)) {
					// load existing
					Data = ResourceLoader.Load<TerrainData>(fPath);
					return;
				}

				// create new
				TerrainData d = new() {
					ResourcePath = fPath
				};
				Data = d;
				_ = ResourceSaver.Save(Data, fPath);//?
			}
		}
		#endregion

		public Terrain() : base() {
			Logger.DebugLog($"[{nameof(Terrain)}] Init");

			_lodder.SetCallbacks(MakeChunk, RecycleChunk, GetVerticalBounds);

			// TODO: should i call it in editor only?
			SetNotifyTransform(true);

			Init_Material();
		}
	}
}
