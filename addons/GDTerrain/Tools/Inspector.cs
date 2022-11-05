#if TOOLS

using System;
using Godot;

namespace GDTerrain {

	[Tool]
	public partial class Inspector : Control {

		private BrushEditor _brushEditor;

		public override void _Ready() {
			_brushEditor = GetNode<BrushEditor>("HSplitContainer/BrushEditor");
		}

		public void SetupDialogs(Control baseControl) {
			_brushEditor.SetupDialogs(baseControl);
		}

		public void SetTargetTerrain(Terrain value) {
		}

		public void SetTerrainPainter(TerrainPainter value) {
			_brushEditor.SetTerrainPainter(value);
		}

		public void SetBrushEditorPaintMode(TerrainPainter.Mode mode) {
			_brushEditor.SetPaintMode(mode);
		}
	}
}
#endif
