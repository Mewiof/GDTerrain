#if TOOLS

using System.Collections.Generic;
using Godot;

namespace GDTerrain {

	[Tool]
	public partial class TerrainPainter : Node3D {

		private static readonly Shader _raiseShader = Plugin.LoadShader("Tools/Brush/Shaders/raise.gdshader");

		public enum Mode {
			Raise,
			Lower,
			Smooth,
			Level,
			Flatten,
			Erode,
			Splat,
			Color,
			Detail,
			Mask
		}

		public Mode mode = Mode.Raise;

		private readonly List<Painter> _painters = new();
		private readonly Brush _brush = new();
		public Color color = new(1f, 0f, 0f, 1f);
		public bool maskFlag;
		#region Modified Maps
		public struct ModifiedMap {

			public int mapTypeIndex;
			public int mapIndex;
			public int painterIndex;

			public ModifiedMap(int mapTypeIndex, int mapIndex, int painterIndex) {
				this.mapTypeIndex = mapTypeIndex;
				this.mapIndex = mapIndex;
				this.painterIndex = painterIndex;
			}
		}

		private readonly List<ModifiedMap> _modifiedMaps = new();
		#endregion
		private Terrain _targetTerrain;

		public Brush Brush => _brush;
		public int BrushSize {
			get => _brush.Size;
			set => _brush.Size = value;
		}

		public void SetBrushTexture(ImageTexture texture) {
			// TODO: cache?
			_brush.Shapes = new ImageTexture[] { texture };
		}

		public float Opacity {
			get => _brush.Opacity;
			set => _brush.Opacity = value;
		}

		/// <summary>All painters</summary>
		public bool PendingOperation {
			get {
				for (int i = 0; i < _painters.Count; i++) {
					if (_painters[i].PendingOperation) {
						return true;
					}
				}
				return false;
			}
		}

		/// <summary>All painters</summary>
		public bool HasModifiedChunks {
			get {
				for (int i = 0; i < _painters.Count; i++) {
					if (_painters[i].HasModifiedChunks) {
						return true;
					}
				}
				return false;
			}
		}

		public void SetTargetTerrain(Terrain value) {
			if (_targetTerrain == value) {
				return;
			}
			_targetTerrain = value;
			// it is important to release resources here
			foreach (Painter painter in _painters) {
				painter.SetImage(null, null);
				painter.ClearModifiedBrushShaderParams();
			}
		}

		public TerrainPainter() : base() {
			for (int i = 0; i < 4; i++) {
				Painter painter = new() {
					// name for debugging
					Name = nameof(Painter) + i
				};
				painter.TextureRegionChanged += rect => OnPainterTextureRegionChanged(rect, i);//?
				AddChild(painter);
				_painters.Add(painter);
			}
		}

		public void Commit() {
			TerrainData terrainData = _targetTerrain.Data;

			for (int i = 0; i < _modifiedMaps.Count; i++) {
				ModifiedMap modifiedMap = _modifiedMaps[i];

				Painter painter = _painters[modifiedMap.painterIndex];
				(List<Vector2i> cPositions, List<Image> cInitData, List<Image> cFinalData) info = painter.Commit();

				// TODO: append changes

				foreach (Vector2i pos in info.cPositions) {
					Rect2i rect = new(pos * Painter.UNDO_CHUNK_SIZE, new(Painter.UNDO_CHUNK_SIZE, Painter.UNDO_CHUNK_SIZE));
					terrainData.NotifyRegionChange(rect, modifiedMap.mapTypeIndex, modifiedMap.mapIndex, false, true);
				}
			}

			if (HasModifiedChunks) {
				throw new System.Exception("'HasModifiedChunks'");
			}
		}

		#region Paint
		public void PaintHeight(TerrainData terrainData, Vector2 position, float factor) {
			Image heightmapImage = terrainData.GetMapImage(TerrainData.MAP_HEIGHT, 0);
			ImageTexture heightmapTexture = terrainData.GetMapTexture(TerrainData.MAP_HEIGHT, 0);

			ModifiedMap mM = new(TerrainData.MAP_HEIGHT, 0, 0);
			_modifiedMaps.Add(mM);

			// when using sculpting tools, make it dependent on brush size
			float raiseStrength = 10f + _brush.Size;
			float delta = factor * (2f / 60f) * raiseStrength;

			Painter painter = _painters[0];

			painter.SetBrushShader(_raiseShader);
			painter.SetBrushShaderParam("p_factor", delta);
			painter.SetImage(heightmapImage, heightmapTexture);
			painter.PaintInput(new((int)position.x, (int)position.y));
		}
		#endregion

		public bool TryPaint(Vector2 position, float pressure) {
			TerrainData terrainData = _targetTerrain.Data;

			if (!_brush.CanPaint(_painters, position, pressure)) {
				return false;
			}

			_modifiedMaps.Clear();

			switch (mode) {
				case Mode.Raise:
					PaintHeight(terrainData, position, 1f);
					break;
			}

			return true;
		}

		private void OnPainterTextureRegionChanged(Rect2i rect, int painterIndex) {
			TerrainData terrainData = _targetTerrain.Data;
			if (terrainData == null) {
				return;
			}
			foreach (ModifiedMap mM in _modifiedMaps) {
				if (mM.painterIndex == painterIndex) {
					terrainData.NotifyRegionChange(rect, mM.mapTypeIndex, mM.mapIndex, false, false);
					break;
				}
			}
		}
	}
}
#endif
