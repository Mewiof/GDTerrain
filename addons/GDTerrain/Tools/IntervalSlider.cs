#if TOOLS

using System;
using Godot;

namespace GDTerrain {

	[Tool]
	public partial class IntervalSlider : Control {

		public enum ValueIndex : int {
			A,
			B
		}

		public const float MARGIN = 2f;

		[Signal]
		public delegate void ChangedEventHandler();

		#region Min & Max
		private float _min;
		[Export]
		public float Min {
			get => _min;
			set => _min = MathF.Min(value, _max);
		}

		private float _max;
		[Export]
		public float Max {
			get => _max;
			set => _max = MathF.Max(value, _min);
		}

		public Vector2 Range {//?
			get => new(Min, Max);
			set {
				Min = value.x;
				Max = value.y;
			}
		}
		#endregion

		#region Values
		private readonly float[] _values = new float[2];

		public float GetValue(ValueIndex valueIndex) {
			return _values[(int)valueIndex];
		}

		public void SetValue(ValueIndex valueIndex, float value, bool notifyChange) {
			float min = _min;
			float max = _max;

			switch (valueIndex) {
				case ValueIndex.A:
					max = ValueB;
					break;
				case ValueIndex.B:
					min = ValueA;
					break;
			}

			value = Mathf.Clamp(value, min, max);
			if (value == _values[(int)valueIndex]) {
				return;
			}

			_values[(int)valueIndex] = value;
			if (notifyChange) {
				_ = EmitSignal(nameof(Changed));
			}
		}

		public float ValueA {
			get => GetValue(ValueIndex.A);
			set => SetValue(ValueIndex.A, value, false);
		}
		public float ValueB {
			get => GetValue(ValueIndex.B);
			set => SetValue(ValueIndex.B, value, false);
		}
		#endregion

		private bool _grabbing;

		#region Ratio
		private float ValueToRatio(float value) {
			if (MathF.Abs(_max - _min) < .001f) {
				return 0f;
			}
			return (value - _min) / (_max - _min);
		}

		private float RatioToValue(float ratio) {
			return (ratio * (_max - _min)) + _min;
		}

		private float RatioA => ValueToRatio(ValueA);
		private float RatioB => ValueToRatio(ValueB);
		#endregion

		#region Misc
		private ValueIndex GetClosest(float ratio) {
			float distanceA = MathF.Abs(ratio - RatioA);
			float distanceB = MathF.Abs(ratio - RatioB);
			if (distanceA < distanceB) {
				return ValueIndex.A;
			}
			return ValueIndex.B;
		}

		private void SetFromPixel(float pixelX) {
			float r = (pixelX - MARGIN) / (Size.x - (MARGIN * 2f));
			ValueIndex vI = GetClosest(r);
			float v = RatioToValue(r);
			SetValue(vI, v, true);
		}
		#endregion

		public override void _GuiInput(InputEvent @event) {
			if (@event is InputEventMouseButton mouseButton) {
				if (mouseButton.ButtonIndex == MouseButton.Left) {
					if (mouseButton.Pressed) {
						_grabbing = true;
						SetFromPixel(mouseButton.Position.x);
						return;
					}
					_grabbing = false;
				}
				return;
			}
			if (_grabbing && @event is InputEventMouseMotion mouseMotion) {
				SetFromPixel(mouseMotion.Position.x);
			}
		}

		public override void _Draw() {
			int grabberWidth = 4;
			float backgroundVMargin = 0f;
			Color grabberColor = new(.8f, .8f, .8f);
			Color intervalColor = new(.4f, .4f, .4f);
			Color backgroundColor = new(.1f, .1f, .1f);

			Rect2 controlRect = new(new(), Size);

			Rect2 backgroundRect = new(
				controlRect.Position.x,
				controlRect.Position.y + backgroundVMargin,
				controlRect.Size.x,
				controlRect.Size.y - (2 * backgroundVMargin));
			DrawRect(backgroundRect, backgroundColor);

			Rect2 foregroundRect = controlRect.Grow(-MARGIN);

			float ratioA = RatioA;
			float ratioB = RatioB;

			float xA = foregroundRect.Position.x + (ratioA * foregroundRect.Size.x);
			float xB = foregroundRect.Position.x + (ratioB * foregroundRect.Size.x);

			Rect2 intervalRect = new(xA, foregroundRect.Position.y, xB - xA, foregroundRect.Size.y);
			DrawRect(intervalRect, intervalColor);

			xA = foregroundRect.Position.x + (ratioA * (foregroundRect.Size.x - grabberWidth));
			xB = foregroundRect.Position.x + (ratioB * (foregroundRect.Size.x - grabberWidth));

			void DrawGrabber(float posX) {
				Rect2 grabberRect = new(posX, foregroundRect.Position.y, grabberWidth, foregroundRect.Size.y);
				DrawRect(grabberRect, grabberColor);
			}

			DrawGrabber(xA);
			DrawGrabber(xB);
		}
	}
}
#endif
