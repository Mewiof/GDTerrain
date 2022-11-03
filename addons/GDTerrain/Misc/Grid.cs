using System.Runtime.CompilerServices;

namespace GDTerrain {

	public class Grid<T> {

		public delegate void ForAllDelegate(T value);

		private int _width;
		private int _height;
		private T[,] _array;

		public void Set(int w, int h) {
			_width = w;
			_height = h;
			_array = new T[_width, _height];
		}

		public Grid() { }

		public Grid(int w, int h) {
			Set(w, h);
		}

		public int Width {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _width;
		}

		public int Height {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _height;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Set(int x, int y, T value) {
			_array[x, y] = value;
		}


		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Set(T value) {
			for (int x = 0; x < _width; x++) {
				for (int y = 0; y < _height; y++) {
					_array[x, y] = value;
				}
			}
		}

		public int ElemCount {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _array.Length;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public T Get(int x, int y) {
			return _array[x, y];
		}

		/// <summary>
		/// Safe variant of 'Get'
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public T GetOrDefault(int x, int y, T defVal) {
			if (y >= 0 && y < _width &&
				x >= 0 && x < _height) {
				return _array[x, y];
			}
			return defVal;
		}

		public void ForAll(ForAllDelegate deleg) {
			for (int x = 0; x < _width; x++) {
				for (int y = 0; y < _height; y++) {
					T t = _array[x, y];
					if (t != null) {
						deleg.Invoke(t);
					}
				}
			}
		}
	}
}
