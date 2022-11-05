#if TOOLS

using System;
using System.Collections.Generic;
using Godot;

namespace GDTerrain {

	public sealed class Brush {

		public const string SHAPES_DIR = Plugin.BASE_PATH + "Tools/Brush/Shapes";
		public const string DEFAULT_SHAPE_FILE_PATH = SHAPES_DIR + "/default.exr";

		public const int MAX_SIZE_FOR_SLIDERS = 500;
		public const int MAX_SIZE = 4000;//?

		/// <summary>New size</summary>
		public Action<int> sizeChanged;
		public Action shapesChanged;

		#region Size
		private int _size = 32;
		public int Size {
			get => _size;
			set {
				if (_size == value) {
					return;
				}

				_size = value;
				sizeChanged?.Invoke(_size);//?
			}
		}
		#endregion
		#region Opacity
		private float _opacity = 1f;
		public float Opacity {
			get => _opacity;
			set => _opacity = Mathf.Clamp(value, 0f, 1f);
		}
		#endregion
		public bool randomRotation;
		public bool usePressure;
		#region Pressure Factor
		private float _pressureFactor = .5f;
		public float PressureFactor {
			get => _pressureFactor;
			set => _pressureFactor = Mathf.Clamp(value, 0f, 1f);
		}
		#endregion
		#region Pressure Opacity Factor
		private float _pressureOpacityFactor = .5f;
		public float PressureOpacityFactor {
			get => _pressureOpacityFactor;
			set => _pressureOpacityFactor = Mathf.Clamp(value, 0f, 1f);
		}
		#endregion
		#region Frequency Distance
		private float _frequencyDistance;
		public float FrequencyDistance {
			get => _frequencyDistance;
			set => _frequencyDistance = MathF.Max(value, 0f);
		}
		#endregion
		#region Delay
		/// <summary>Milliseconds</summary>
		private float _delay;
		/// <summary>Milliseconds</summary>
		public float Delay {
			get => _delay;
			set => _delay = MathF.Max(value, 0f);
		}
		#endregion
		private int _shapeIndex;
		#region Shapes
		private ImageTexture[] _shapes;
		public ImageTexture[] Shapes {
			get {
				// TODO: why 'copy'?
				// copy
				return _shapes.Duplicate();
			}
			set {
				if (value == null || value.Length < 1) {
					throw new ArgumentException("'value == null || value.Length < 1'");
				}
				for (int i = 0; i < value.Length; i++) {
					if (value[i] == null) {
						throw new ArgumentException("'value[i] == null'");
					}
				}
				// TODO: why 'copy'?
				// copy
				_shapes = value.Duplicate();
				if (_shapeIndex >= _shapes.Length) {
					_shapeIndex = _shapes.Length - 1;
				}
				shapesChanged?.Invoke();//?
			}
		}
		#endregion
		private Vector2 _prevPosition;
		private ulong _prevTime;

		// TODO: move?
		public static ImageTexture LoadShapeFromFile(string fPath) {
			Image image = ResourceLoader.Load<Image>(fPath, null, ResourceLoader.CacheMode.Replace);
			if (image == null) {
				return null;
			}
			return ImageTexture.CreateFromImage(image);
		}

		/// <summary>Also configures <paramref name="painters"/></summary>
		public bool CanPaint(List<Painter> painters, Vector2 position, float pressure) {
			if (_shapes == null || _shapes.Length < 1) {
				throw new Exception("'_shapes == null || _shapes.Length < 1'");
			}

			// distance
			if (position.DistanceTo(_prevPosition) < _frequencyDistance) {
				return false;
			}
			_prevPosition = position;

			// delay
			ulong now = Time.GetTicksMsec();
			if (now - _prevTime < _delay) {
				return false;
			}
			_prevTime = now;

			for (int i = 0; i < painters.Count; i++) {
				Painter painter = painters[i];
				if (randomRotation) {
					// TODO: is there any float random? :eyes:
					painter.BrushRotation = (float)GD.RandRange(-Mathf.Pi, Mathf.Pi);
				} else {
					painter.BrushRotation = 0f;
				}

				painter.SetBrushTexture(_shapes[_shapeIndex]);
				painter.brushSize = _size;

				if (usePressure) {
					painter.BrushScale = Mathf.Lerp(1f, pressure, _pressureFactor);
					painter.BrushOpacity = _opacity * Mathf.Lerp(1f, pressure, _pressureOpacityFactor);
				} else {
					painter.BrushScale = 1f;
					painter.BrushOpacity = _opacity;
				}
			}

			_shapeIndex++;
			if (_shapeIndex >= _shapes.Length) {
				_shapeIndex = 0;
			}

			return true;
		}

		public void OnPaintEnd() {//?
			_prevPosition = -Vector2.One * 9999f;
			_prevTime = 0U;
		}
	}
}
#endif
