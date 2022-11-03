using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Godot;

namespace GDTerrain.Native {

	// Supposed to be native?
	public sealed class QuadTreeLOD {

		public const int NO_CHILDREN = -1;
		public const int ROOT = -1;

		private readonly Quad _root = new();
		private readonly List<Quad> _nodePool = new();
		private readonly Stack<int> _freeIndices = new();

		private int _maxDepth;
		private int _baseSize = 16;
		private float _splitScale = 2f;

		/// <summary>x, y, LOD</summary>
		private Func<int, int, int, Chunk> _makeChunk;
		/// <summary>TerrainChunk, x, y, LOD</summary>
		private Action<Chunk, int, int, int> _recycleChunk;
		/// <summary>x, y, LOD</summary>
		private Func<int, int, int, Vector2> _getVertBounds;

		public void SetCallbacks(Func<int, int, int, Chunk> makeChunk, Action<Chunk, int, int, int> recycleChunk, Func<int, int, int, Vector2> getVertBounds) {
			_makeChunk = makeChunk;
			_recycleChunk = recycleChunk;
			_getVertBounds = getVertBounds;
		}

		public int LODCount {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _maxDepth + 1;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int GetLODFactor(int value) {
			return 1 << value;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int ComputeLODCount(int baseSize, int fullSize) {
			int result = 0;
			while (fullSize > baseSize) {
				fullSize >>= 1;
				result += 1;
			}
			return result;
		}

		public float SplitScale {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _splitScale;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set {
				const float MIN = 2f;//?
				const float MAX = 5f;//?

				if (value > MAX) {
					value = MAX;
				}
				if (value < MIN) {
					value = MIN;
				}

				_splitScale = value;
			}
		}

		public void Clear() {
			JoinAllRecursively(ROOT, _maxDepth);
			_maxDepth = 0;
			_baseSize = 0;
		}

		public void Set(int baseSize, int fullSize) {
			Clear();
			_baseSize = baseSize;
			_maxDepth = ComputeLODCount(baseSize, fullSize);

			int nodeCount = (((int)Mathf.Pow(4, _maxDepth + 1) - 1) / (4 - 1)) - 1;
			_nodePool.Clear();
			for (int i = 0; i < nodeCount; i++) {
				_nodePool.Add(new());
			}

			_freeIndices.Clear();
			for (int i = 0; i < nodeCount / 4; i++) {
				_freeIndices.Push(4 * i);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Update(Vector3 viewPos) {
			Update(ROOT, _maxDepth, viewPos);

			// makes sure we keep seeing the lowest LOD
			// if tree is cleared, while we are far away
			Quad root = Root;
			if (!root.HasChildren && root.IsNull) {
				root.Data = MakeChunk(_maxDepth, 0, 0);
			}
		}

		public void DebugDrawTree(CanvasItem canvasItem) {
			if (canvasItem == null) {//?
				return;
			}
			DebugDrawTreeRecursive(canvasItem, ROOT, _maxDepth, 0);
		}

		public Quad Root {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _root;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Quad GetNode(int index) {
			if (index == ROOT) {
				return _root;
			}
			return _nodePool[index];
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void ClearChildren(int index) {
			Quad quad = GetNode(index);
			if (quad.HasChildren) {
				RecycleChildren(quad.firstChild);
				quad.firstChild = NO_CHILDREN;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private int AllocateChildren() {
			if (_freeIndices.Count == 0) {
				return NO_CHILDREN;
			}

			return _freeIndices.Pop();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void RecycleChildren(int i0) {
			if (i0 % 4 != 0) {
				throw new ArgumentException("'i0 % 4 != 0'");
			}

			for (int i = 0; i < 4; i++) {
				_nodePool[i0 + i].Reset();
			}

			_freeIndices.Push(i0);
		}

		private Chunk MakeChunk(int lOD, int originX, int originY) {
			return _makeChunk.Invoke(originX, originY, lOD);
		}

		private void RecycleChunk(int quadIndex, int lOD) {
			Quad quad = GetNode(quadIndex);
			_recycleChunk.Invoke(quad.Data, quad.originX, quad.originY, lOD);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void JoinAllRecursively(int quadIndex, int lOD) {
			Quad quad = GetNode(quadIndex);

			if (quad.HasChildren) {
				for (int i = 0; i < 4; i++) {
					JoinAllRecursively(quad.firstChild + i, lOD - 1);
				}
				ClearChildren(quadIndex);
			} else if (!quad.IsNull) {
				RecycleChunk(quadIndex, lOD);
				quad.ClearData();
			}
		}

		private static readonly Vector3 _worldCenterOffsetTemp = new(.5f, 0f, .5f);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void Update(int quadIndex, int lOD, Vector3 viewPos) {
			Quad quad = GetNode(quadIndex);
			int lODFactor = GetLODFactor(lOD);
			int chunkSize = _baseSize * lODFactor;
			Vector3 worldCenter = chunkSize * (new Vector3(quad.originX, 0f, quad.originY) + _worldCenterOffsetTemp);

			Vector2 vBounds = _getVertBounds.Invoke(quad.originX, quad.originY, lOD);
			worldCenter.y = (vBounds.x + vBounds.y) / 2f;

			int splitDistance = _baseSize * lODFactor * (int)_splitScale;

			if (!quad.HasChildren) {
				if (lOD > 0 && worldCenter.DistanceTo(viewPos) < splitDistance) {
					// split
					int newIndex = AllocateChildren();
					if (newIndex == NO_CHILDREN) {
						throw new Exception("'newIndex == NO_CHILDREN'");
					}
					quad.firstChild = newIndex;

					for (int i = 0; i < 4; i++) {
						Quad child = GetNode(quad.firstChild + i);
						child.originX = quad.originX * 2 + (i & 1);
						child.originY = quad.originY * 2 + ((i & 2) >> 1);
						child.Data = MakeChunk(lOD - 1, child.originX, child.originY);
					}

					if (!quad.IsNull) {
						RecycleChunk(quadIndex, lOD);
						quad.ClearData();
					}
				}
			} else {
				bool noSplitChild = true;

				for (int i = 0; i < 4; i++) {
					Update(quad.firstChild + i, lOD - 1, viewPos);

					if (GetNode(quad.firstChild + i).HasChildren) {
						noSplitChild = false;
					}
				}

				if (noSplitChild && worldCenter.DistanceTo(viewPos) > splitDistance) {
					// join
					for (int i = 0; i < 4; i++) {
						RecycleChunk(quad.firstChild + i, lOD - 1);
					}
					ClearChildren(quadIndex);
					quad.Data = MakeChunk(lOD, quad.originX, quad.originY);
				}
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void DebugDrawTreeRecursive(CanvasItem canvasItem, int quadIndex, int lODIndex, int childIndex) {
			Quad quad = GetNode(quadIndex);

			if (quad.HasChildren) {
				int chIndex = quad.firstChild;
				for (int i = 0; i < 4; i++) {
					DebugDrawTreeRecursive(canvasItem, chIndex + i, lODIndex - 1, i);
				}
			} else {
				float size = GetLODFactor(lODIndex);
				int checker = 0;
				if (childIndex == 1 || childIndex == 2) {
					checker = 1;
				}

				int chunkIndicator = 0;
				if (!quad.IsNull) {
					chunkIndicator = 1;
				}

				Rect2 rect = new(new Vector2(quad.originX, quad.originY) * size, new(size, size));
				Color color = new(1f - lODIndex * .2f, .2f * checker, chunkIndicator, 1f);

				canvasItem.DrawRect(rect, color);
			}
		}
	}
}
