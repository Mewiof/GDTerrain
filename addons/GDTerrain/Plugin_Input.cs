#if TOOLS

using Godot;

namespace GDTerrain {

	public partial class Plugin : EditorPlugin {

		public override long _Forward3dGuiInput(Camera3D camera, InputEvent @event) {
			long afterGUIInput = (long)AfterGUIInput.Pass;

			if (_targetTerrain == null || !_targetTerrain.HasData) {
				return afterGUIInput;
			}

			if (@event is InputEventMouseButton button && button.ButtonIndex == MouseButton.Left) {
				if (!button.Pressed) {
					_mousePressed = false;
				}

				if (!button.CtrlPressed && !button.AltPressed) {
					if (button.Pressed) {
						_mousePressed = true;
					}
					afterGUIInput = (long)AfterGUIInput.Stop;
					if (!_mousePressed) {
						// just finished painting
						_pendingPaintCommit = true;
						TerrainPainter.Brush.OnPaintEnd();
					}
				}
			} else if (@event is InputEventMouseMotion motion) {
				Vector2i? gridPos = GetGridPos(motion.Position, camera);
				if (gridPos.HasValue) {
					BrushDecal.SetPosition(new(gridPos.Value.x, 0, gridPos.Value.y));

					if (_mousePressed && Input.IsMouseButtonPressed(MouseButton.Left)) {//?
						afterGUIInput = (long)AfterGUIInput.Stop;
						_ = TerrainPainter.TryPaint((Vector2)gridPos, motion.Pressure);
					}
				}
			} else {
				_targetTerrain.UpdateViewerPosition(camera);
				_inspector.SetCameraTransform(camera.GlobalTransform);
			}

			return afterGUIInput;
		}
	}
}
#endif
