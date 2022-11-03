using System.Runtime.CompilerServices;
using Godot;
using GDTerrain.Native;

namespace GDTerrain {

	[Tool]
	public partial class Terrain : Node3D {

		//private const string _SHADER_DEFAULT = "Default";

		private readonly Mesher _mesher;

		#region QuadTreeLOD
		private readonly QuadTreeLOD _lodder;

		[Export(PropertyHint.Range, "2,5,1")]
		public float LODScale {
			get => _lodder.SplitScale;
			set => _lodder.SplitScale = value;
		}
		#endregion

		#region Data
		private TerrainData _data;

		private void OnDataRegionChanged(int arg1, int arg2, int arg3, int arg4, int arg5) {
		}

		private void OnDataMapChanged(int typeIndex, int index) {
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
					Plugin.DebugLog($"[{nameof(Terrain)}] Same '{nameof(_data)}'. Ignoring...");
					return;
				}

				Plugin.DebugLog($"[{nameof(Terrain)}] Setting new '{nameof(_data)}'");

				// old
				if (_data != null) {
					Plugin.DebugLog($"[{nameof(Terrain)}] Disconnecting old '{nameof(TerrainData)}'...");
					_data.resolutionChanged -= ResetGroundChunks;
					//_data.regionChanged -= OnDataRegionChanged;
					//_data.mapChanged -= OnDataMapChanged;
					_data.mapChanged -= OnDataMapAdded;
					//_data.mapRemoved -= OnDataMapRemoved;
				}

				_data = value;

				// the order of these two is important
				ClearAllChunks();

				// new
				if (_data != null) {
					Plugin.DebugLog($"[{nameof(Terrain)}] Connecting new '{nameof(TerrainData)}'...");

					_data.resolutionChanged += ResetGroundChunks;
					//_data.regionChanged += OnDataRegionChanged;
					_data.mapChanged += OnDataMapChanged;
					//_data.mapAdded += OnDataMapAdded;
					//_data.mapRemoved += OnDataMapRemoved;

					ResetGroundChunks();
				}

				UpdateConfigurationWarnings();

				Plugin.DebugLog($"[{nameof(Terrain)}] '{nameof(_data)}' has been set");
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
					Data = ResourceLoader.Load<TerrainData>(fPath, null, ResourceLoader.CacheMode.Replace);
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
			Plugin.DebugLog($"[{nameof(Terrain)}] Init");

			_mesher = new();
			_lodder = new();
			_lodder.SetCallbacks(MakeChunk, RecycleChunk, GetVerticalBounds);

			// TODO: should i call it in editor only?
			SetNotifyTransform(true);
		}
	}
}
