using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Godot;

namespace SimpleTerrain {

	public class Mesher {

		public const int
			SEAM_LEFT = 1,
			SEAM_RIGHT = 2,
			SEAM_BOTTOM = 4,
			SEAM_TOP = 8,
			SEAM_CONFIG_COUNT = 16;

		private readonly Mesh[][] _meshCache = new Mesh[SEAM_CONFIG_COUNT][];
		private int _chunkSizeX; // Prev
		private int _chunkSizeY; // Prev
		private int _lODCount; // Prev
		private readonly List<int> _makeIndicesResultCache = new();

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private int[] MakeIndices(int chunkSizeX, int chunkSizeY, int seams) {
			_makeIndicesResultCache.Clear();

			/*if (seams != 0 && (chunkSizeX % 2 != 0 || chunkSizeY % 2 != 0)) {
				throw new ArgumentException("'chunkSizeX % 2 != 0 || chunkSizeY % 2 != 0'");
			}*/

			int regOriginX = 0;
			int regOriginY = 0;
			int regSizeX = chunkSizeX;
			int regSizeY = chunkSizeY;
			int regHStride = 1;

			if ((seams & SEAM_LEFT) > 0) {
				regOriginX++;
				regSizeX--;
				regHStride++;
			}

			if ((seams & SEAM_BOTTOM) > 0) {
				regOriginY++;
				regSizeY--;
			}

			if ((seams & SEAM_RIGHT) > 0) {
				regSizeX--;
				regHStride++;
			}

			if ((seams & SEAM_TOP) > 0) {
				regSizeY--;
			}

			// regular triangles
			int iI = regOriginX + (regOriginY * (chunkSizeX + 1));

			for (int y = 0; y < regSizeY; y++) {
				for (int x = 0; x < regSizeX; x++) {
					int i00 = iI,
						i10 = iI + 1,
						i01 = iI + chunkSizeX + 1,
						i11 = i01 + 1;

					/* 01---11
					 *  |  /|
					 *  | / |
					 *  |/  |
					 * 00---10
					 */

					// flips the pattern to make the geometry orientation-free
					// not sure if it helps in any way though
					bool flip = (x + regOriginX + ((y + regOriginY) % 2)) % 2 != 0;

					if (flip) {
						_makeIndicesResultCache.Add(i00);
						_makeIndicesResultCache.Add(i10);
						_makeIndicesResultCache.Add(i01);

						_makeIndicesResultCache.Add(i10);
						_makeIndicesResultCache.Add(i11);
						_makeIndicesResultCache.Add(i01);
					} else {
						_makeIndicesResultCache.Add(i00);
						_makeIndicesResultCache.Add(i11);
						_makeIndicesResultCache.Add(i01);

						_makeIndicesResultCache.Add(i00);
						_makeIndicesResultCache.Add(i10);
						_makeIndicesResultCache.Add(i11);
					}

					iI++;
				}
				iI += regHStride;
			}

			// left seam
			if ((seams & SEAM_LEFT) > 0) {
				/*    4 . 5
				 *    |\  .
				 *    | \ .
				 *    |  \.
				 * (2)|   3
				 *    |  /.
				 *    | / .
				 *    |/  .
				 *    0 . 1
				 */

				int i = 0;
				int n = chunkSizeY / 2;

				for (int j = 0; j < n; j++) {
					var i0 = i;
					var i1 = i + 1;
					var i3 = i + chunkSizeX + 2;
					var i4 = i + 2 * (chunkSizeX + 1);
					var i5 = i4 + 1;

					_makeIndicesResultCache.Add(i0);
					_makeIndicesResultCache.Add(i3);
					_makeIndicesResultCache.Add(i4);

					if (j != 0 || (seams & SEAM_BOTTOM) == 0) {
						_makeIndicesResultCache.Add(i0);
						_makeIndicesResultCache.Add(i1);
						_makeIndicesResultCache.Add(i3);
					}

					if (j != n - 1 || (seams & SEAM_TOP) == 0) {
						_makeIndicesResultCache.Add(i3);
						_makeIndicesResultCache.Add(i5);
						_makeIndicesResultCache.Add(i4);
					}

					i = i4;
				}
			}

			// right seam
			if ((seams & SEAM_RIGHT) > 0) {
				/*    4 . 5
				 *    .  /|
				 *    . / |
				 *    ./  |
				 *    2   |(3)
				 *    .\  |
				 *    . \ |
				 *    .  \|
				 *    0 . 1
				 */

				int i = chunkSizeX - 1;
				int n = chunkSizeY / 2;

				for (int j = 0; j < n; j++) {
					var i0 = i;
					var i1 = i + 1;
					var i2 = i + chunkSizeX + 1;
					var i4 = i + 2 * (chunkSizeX + 1);
					var i5 = i4 + 1;

					_makeIndicesResultCache.Add(i1);
					_makeIndicesResultCache.Add(i5);
					_makeIndicesResultCache.Add(i2);

					if (j != 0 || (seams & SEAM_BOTTOM) == 0) {
						_makeIndicesResultCache.Add(i0);
						_makeIndicesResultCache.Add(i1);
						_makeIndicesResultCache.Add(i2);
					}

					if (j != n - 1 || (seams & SEAM_TOP) == 0) {
						_makeIndicesResultCache.Add(i2);
						_makeIndicesResultCache.Add(i5);
						_makeIndicesResultCache.Add(i4);
					}

					i = i4;
				}
			}

			// bottom seam
			if ((seams & SEAM_BOTTOM) > 0) {
				/* 3 . 4 . 5
				 * .  / \  .
				 * . /   \ .
				 * ./     \.
				 * 0-------2
				 *    (1)
				 */

				int i = 0;
				int n = chunkSizeX / 2;

				for (int j = 0; j < n; j++) {
					var i0 = i;
					var i2 = i + 2;
					var i3 = i + chunkSizeX + 1;
					var i4 = i3 + 1;
					var i5 = i4 + 1;

					_makeIndicesResultCache.Add(i0);
					_makeIndicesResultCache.Add(i2);
					_makeIndicesResultCache.Add(i4);

					if (j != 0 || (seams & SEAM_LEFT) == 0) {
						_makeIndicesResultCache.Add(i0);
						_makeIndicesResultCache.Add(i4);
						_makeIndicesResultCache.Add(i3);
					}

					if (j != n - 1 || (seams & SEAM_RIGHT) == 0) {
						_makeIndicesResultCache.Add(i2);
						_makeIndicesResultCache.Add(i5);
						_makeIndicesResultCache.Add(i4);
					}

					i = i2;
				}
			}

			// top seam
			if ((seams & SEAM_TOP) > 0) {
				/*    (4)
				 * 3-------5
				 * .\     /.
				 * . \   / .
				 * .  \ /  .
				 * 0 . 1 . 2
				 */

				int i = (chunkSizeY - 1) * (chunkSizeX + 1);
				int n = chunkSizeX / 2;

				for (int j = 0; j < n; j++) {
					var i0 = i;
					var i1 = i + 1;
					var i2 = i + 2;
					var i3 = i + chunkSizeX + 1;
					var i5 = i3 + 2;

					_makeIndicesResultCache.Add(i3);
					_makeIndicesResultCache.Add(i1);
					_makeIndicesResultCache.Add(i5);

					if (j != 0 || (seams & SEAM_LEFT) == 0) {
						_makeIndicesResultCache.Add(i0);
						_makeIndicesResultCache.Add(i1);
						_makeIndicesResultCache.Add(i3);
					}

					if (j != n - 1 || (seams & SEAM_RIGHT) == 0) {
						_makeIndicesResultCache.Add(i1);
						_makeIndicesResultCache.Add(i2);
						_makeIndicesResultCache.Add(i5);
					}

					i = i2;
				}
			}

			return _makeIndicesResultCache.ToArray();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private Mesh MakeFlatChunk(int quadCountX, int quadCountY, int stride, int seams) {
			Vector3[] positions = new Vector3[(quadCountX + 1) * (quadCountY + 1)];

			int i = 0;
			for (int y = 0; y < quadCountY + 1; y++) {
				for (int x = 0; x < quadCountX + 1; x++) {
					positions[i] = new(x * stride, 0f, y * stride);
					i++;
				}
			}

			Span<int> indices = MakeIndices(quadCountX, quadCountY, seams).AsSpan();

			Godot.Collections.Array arr = new();
			arr.Resize((int)Mesh.ArrayType.Max);
			arr[(int)Mesh.ArrayType.Vertex] = positions.AsSpan();
			arr[(int)Mesh.ArrayType.Index] = indices;

			ArrayMesh arrayMesh = new();
			arrayMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arr);

			return arrayMesh;
		}

		public void Configure(int chunkSizeX, int chunkSizeY, int lODCount) {
			// same?
			if (chunkSizeX == _chunkSizeX &&
				chunkSizeY == _chunkSizeY &&
				lODCount == _lODCount) {
				return;
			}

			// update history
			_chunkSizeX = chunkSizeX;
			_chunkSizeY = chunkSizeY;
			_lODCount = lODCount;

			for (int s = 0; s < SEAM_CONFIG_COUNT; s++) {
				Mesh[] arr = new Mesh[_lODCount];
				for (int lOD = 0; lOD < _lODCount; lOD++) {
					arr[lOD] = MakeFlatChunk(_chunkSizeX, _chunkSizeY, 1 << lOD, s);
				}
				_meshCache[s] = arr;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Mesh GetChunkMesh(int lOD, int seams) {
			return _meshCache[seams][lOD];
		}
	}
}
