using System;
using Godot;

namespace GDTerrain {

	public partial class Terrain : Node3D {

		private const float _MIN_MAP_SCALE = .01f;

		#region MapScale
		private Vector3 _mapScale = Vector3.One;
		[Export]
		public Vector3 MapScale {
			get => _mapScale;
			set {
				// same?
				if (_mapScale == value) {
					return;
				}
				value.x = MathF.Max(value.x, _MIN_MAP_SCALE);
				value.y = MathF.Max(value.y, _MIN_MAP_SCALE);
				value.z = MathF.Max(value.z, _MIN_MAP_SCALE);
				_mapScale = value;
				OnTransformChanged();
			}
		}
		#endregion

		#region Centered
		private bool _centered;
		[Export]
		public bool Centered {
			get => _centered;
			set {
				// same?
				if (_centered == value) {
					return;
				}
				_centered = value;
				OnTransformChanged();
			}
		}
		#endregion

		/// <summary>Heightmap</summary>
		public Transform3D InternalTransform {
			get {
				Transform3D gT = GlobalTransform;
				Transform3D result = new(gT.basis * Basis.Identity.Scaled(_mapScale), gT.origin);
				if (_centered && HasData) {
					float halfSize = .5f * (_data.Resolution - 1);
					result.origin += result.basis * -new Vector3(halfSize, 0f, halfSize);
				}
				return result;
			}
		}

		private Camera3D _cachedCamera;
		private Vector3 _viewerPosWorld;

		public void UpdateViewerPosition(Camera3D camera) {
			if (camera == null) {
				if (_cachedCamera == null) {
					Viewport viewport = GetViewport();
					if (viewport != null) {
						_cachedCamera = viewport.GetCamera3d();
					}
				}
				camera = _cachedCamera;
			}

			if (camera == null) {
				return;
			}

			_viewerPosWorld = camera.GlobalTransform.origin;
		}

		[Signal]
		public delegate void TransformChangedEventHandler(Transform3D value);

		private void OnTransformChanged() {
			// spawned?
			if (!IsInsideTree()) {
				return;
			}

			// send to all chunks
			Transform3D internalTransform = InternalTransform;
			ForAllChunks(item => item.OnParentTransformChanged(internalTransform));

			_ = EmitSignal(nameof(TransformChanged), internalTransform);
		}
	}
}
