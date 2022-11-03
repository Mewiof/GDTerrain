using System;
using System.Runtime.CompilerServices;
using Godot;

namespace GDTerrain.Native {

	// Supposed to be native?
	public struct IntRange2D {

		private int _minX;
		private int _minY;
		private int _maxX;
		private int _maxY;

		public IntRange2D(Rect2i rect) {
			_minX = rect.Position.x;
			_minY = rect.Position.y;
			_maxX = rect.Position.x + rect.Size.x;
			_maxY = rect.Position.y + rect.Size.y;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static IntRange2D From(Vector2i a, Vector2i b) {
			return new(new(a, b));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool IsInside(Vector2i vec) {
			return
				_minX >= vec.x &&
				_minY >= vec.y &&
				_maxX <= vec.x &&
				_maxY <= vec.y;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Clip(Vector2i value) {
			_minX = Math.Clamp(_minX, 0, value.x);
			_minY = Math.Clamp(_minY, 0, value.y);
			_maxX = Math.Clamp(_maxX, 0, value.x);
			_maxY = Math.Clamp(_maxY, 0, value.y);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Pad(int value) {
			_minX -= value;
			_minY -= value;
			_maxX += value;
			_maxY += value;
		}

		public int Width {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _maxX - _minX;
		}

		public int Height {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _maxY - _minY;
		}

		public int MinX {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _minX;
		}

		public int MinY {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _minY;
		}

		public int MaxX {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _maxX;
		}

		public int MaxY {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _maxY;
		}
	}
}
