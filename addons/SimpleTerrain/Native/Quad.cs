using System.Runtime.CompilerServices;

namespace SimpleTerrain.Native {

	// Supposed to be native?
	public sealed class Quad {

		public int firstChild = QuadTreeLOD.NO_CHILDREN;
		public int originX;
		public int originY;

		private Chunk _data;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void ClearData() {
			_data = null;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Reset() {
			firstChild = QuadTreeLOD.NO_CHILDREN;
			originX = 0;
			originY = 0;
			// same as ClearData()
			_data = null;
		}

		public bool HasChildren {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => firstChild != QuadTreeLOD.NO_CHILDREN;
		}

		public bool IsNull {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _data == null;
		}

		public Chunk Data {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _data;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set => _data = value;
		}
	}
}
