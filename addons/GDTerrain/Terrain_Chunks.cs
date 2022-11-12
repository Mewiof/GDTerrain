using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Godot;

namespace GDTerrain {

	public struct PendingChunkUpdate {
		public int cPosX;
		public int cPosY;
		public int lOD;

		public PendingChunkUpdate(int cPosX, int cPosY, int lOD) {
			this.cPosX = cPosX;
			this.cPosY = cPosY;
			this.lOD = lOD;
		}
	}

	public partial class Terrain : Node3D {

		private const int _MIN_CHUNK_SIZE = 16;
		private const int _MAX_CHUNK_SIZE = 64;

		/// <summary>Directions to neighbour chunks</summary>
		private static readonly int[,] _sDirs = new int[4, 2] {
			{-1, 0},
			{1, 0},
			{0, -1},
			{0, 1}
		};

		/// <summary>Directions to neighbour chunks of higher LOD</summary>
		private static readonly int[,] _sRDirs = new int[8, 2] {
			{-1, 0},
			{-1, 1},
			{0, 2},
			{1, 2},
			{2, 1},
			{2, 0},
			{1, -1},
			{0, -1}
		};

		private readonly List<PendingChunkUpdate> _pendingChunkUpdates = new();
		/// <summary>By LOD</summary>
		private readonly Dictionary<int, Grid<Chunk>> _chunkGrids = new();

		// Stats & Debug
		private int _updatedChunks;

		#region ChunkSize
		private int _chunkSize = 16;
		[Export(PropertyHint.Enum, "16:16,32:32,64:64")] // name:value
		public int ChunkSize {
			get => _chunkSize;
			set {
				int cS = Math.Clamp(Util.NextPowerOfTwo(value), _MIN_CHUNK_SIZE, _MAX_CHUNK_SIZE);
				if (cS == _chunkSize) {
					return;
				}
				Logger.DebugLog($"Setting '{nameof(_chunkSize)}' to {cS}...");
				_chunkSize = cS;
				ResetChunks();
			}
		}
		#endregion

		#region ForAllChunks
		private delegate void ForAllChunksDelegate(Chunk chunk);

		private void ForAllChunks(ForAllChunksDelegate deleg) {
			for (int i = 0; i < _chunkGrids.Count; i++) {
				_chunkGrids[i].ForAll(item => deleg.Invoke(item));
			}
		}
		#endregion

		#region Grid
		private Chunk GetChunkAt(int cPosX, int cPosY, int lOD) {
			if (lOD >= _chunkGrids.Count) {
				return null;
			}

			// posY, posX
			return _chunkGrids[lOD].GetOrDefault(cPosY, cPosX, null);
		}

		private void SetChunkAt(int cPosX, int cPosY, int lOD, Chunk value) {
			// posY, posX
			_chunkGrids[lOD].Set(cPosY, cPosX, value);
		}
		#endregion

		private void QueueChunkUpdate(Chunk chunk, int cPosX, int cPosY, int lOD) {
			if (chunk.PendingUpdate) {
				return;
			}

			_pendingChunkUpdates.Add(new(cPosX, cPosY, lOD));
			chunk.PendingUpdate = true;
		}

		// Mesh, AABB, visibility
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void UpdateChunk(Chunk chunk, int lOD, bool visible) {
			if (!HasData) {
				throw new Exception("'!HasData'");
			}

			// check for own seams
			int seams = 0;
			int s = _chunkSize << lOD;
			int cPosX = chunk.CellOriginX / s;
			int cPosY = chunk.CellOriginY / s;
			int cPosLowerX = cPosX / 2;
			int cPosLowerY = cPosY / 2;

			// check for lower-LOD chunks around
			for (int d = 0; d < 4; d++) {
				int nCPosLowerX = (cPosX + _sDirs[d, 0]) / 2;
				int nCPosLowerY = (cPosY + _sDirs[d, 1]) / 2;
				if (nCPosLowerX != cPosLowerX || nCPosLowerY != cPosLowerY) {
					Chunk nChunk = GetChunkAt(nCPosLowerX, nCPosLowerY, lOD + 1);
					if (nChunk != null && nChunk.Active) {
						seams |= 1 << d;
					}
				}
			}

			Mesh mesh = _mesher.GetChunkMesh(lOD, seams);
			chunk.SetMesh(mesh);

			AABB aABB = _data.GetRegionAABB(chunk.CellOriginX, chunk.CellOriginY, s, s);
			aABB.Position = new(0f, aABB.Position.y, 0f);
			chunk.SetAABB(aABB);

			chunk.Visible = visible;
			chunk.PendingUpdate = false;
		}

		#region Func
		// Func
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private Chunk MakeChunk(int cPosX, int cPosY, int lOD) { // TODO
			Chunk chunk = GetChunkAt(cPosX, cPosY, lOD);

			if (chunk == null) {
				// first request at this LOD. Generate
				int lODFactor = QuadTreeLOD.GetLODFactor(lOD);//?
				int originInCelX = cPosX * _chunkSize * lODFactor;
				int originInCelY = cPosY * _chunkSize * lODFactor;

				chunk = new(this, originInCelX, originInCelY, _material);//?
				chunk.OnParentTransformChanged(InternalTransform);

				chunk.SetRenderLayerMask(_renderLayerMask);
				chunk.SetShadowCastingSetting(_shadowCastSetting);

				SetChunkAt(cPosX, cPosY, lOD, chunk);
			}

			QueueChunkUpdate(chunk, cPosX, cPosY, lOD);

			chunk.Active = true;
			return chunk;
		}

		// Func
		private static void RecycleChunk(Chunk chunk, int cPosX, int cPosY, int lOD) {//?
			chunk.Visible = false;
			chunk.Active = false;
		}

		// Func
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private Vector2 GetVerticalBounds(int cPosX, int cPosY, int lOD) {
			int chunkSize = _chunkSize * QuadTreeLOD.GetLODFactor(lOD);//?
			int originInCelX = cPosX * chunkSize;
			int originInCelY = cPosY * chunkSize;
			return _data.GetPointAABB(originInCelX + (chunkSize / 2), originInCelY + (chunkSize / 2));
		}
		#endregion

		// References
		private void ClearAllChunks() {
			_lodder.Clear();

			for (int i = 0; i < _chunkGrids.Count; i++) {
				_chunkGrids[i] = null;
			}
		}

		private void ResetChunks() {
			ClearAllChunks();
			_pendingChunkUpdates.Clear();
			_chunkGrids.Clear();

			if (!HasData) {
				return;
			}

			_lodder.Set(_chunkSize, _data.Resolution);

			int lODCount = _lodder.LODCount;
			int cRes = _data.Resolution / _chunkSize;

			// create grids
			for (int lOD = 0; lOD < lODCount; lOD++) {
				Logger.DebugLog($"Creating grid for {lOD} ({cRes}x{cRes})...");
				_chunkGrids[lOD] = new(cRes, cRes);
				cRes /= 2;
			}

			_mesher.Configure(_chunkSize, _chunkSize, lODCount);
		}

		/// <summary>Mathematical, does not use collisions</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Vector2i? CellRaycast(Vector3 worldOrigin, Vector3 worldDirection, float maxDistance) {
			if (!HasData) {
				return null;
			}
			Transform3D toLocal = InternalTransform.AffineInverse();
			Vector3 localOrigin = toLocal * worldOrigin;
			Vector3 localDir = toLocal.basis * worldDirection;
			return _data.CellRaycast(localOrigin, localDir, maxDistance);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void SetAreaDirty(int originInCelX, int originInCelY, int sizeInCelX, int sizeInCelY) {
			int cPos0X = originInCelX / ChunkSize;
			int cPos0Y = originInCelY / ChunkSize;
			int cSizeX = ((sizeInCelX - 1) / ChunkSize) + 1;
			int cSizeY = ((sizeInCelY - 1) / ChunkSize) + 1;

			for (int lOD = 0; lOD < _lodder.LODCount; lOD++) {
				Grid<Chunk> grid = _chunkGrids[lOD];
				int s = QuadTreeLOD.GetLODFactor(lOD);

				int minX = cPos0X / s;
				int minY = cPos0Y / s;
				int maxX = (cPos0X + cSizeX - 1) / s + 1;
				int maxY = (cPos0Y + cSizeY - 1) / s + 1;

				for (int cY = minY; cY < maxY; cY++) {
					for (int cX = minX; cX < maxX; cX++) {
						// cY, cX
						Chunk chunk = grid.GetOrDefault(cY, cX, null);
						if (chunk != null && chunk.Active) {
							QueueChunkUpdate(chunk, cX, cY, lOD);
						}
					}
				}
			}
		}

		private void PhysicsProcess_Chunks() {
			_updatedChunks = 0;

			// for neighbours (seams)
			int prevCount = _pendingChunkUpdates.Count;
			PendingChunkUpdate u;
			for (int i = 0; i < prevCount; i++) {
				u = _pendingChunkUpdates[i];

				// in case chunk got split
				for (int d = 0; d < 4; d++) {
					int nCPosX = u.cPosX + _sDirs[d, 0];
					int nCPosY = u.cPosY + _sDirs[d, 1];

					Chunk nChunk = GetChunkAt(nCPosX, nCPosY, u.lOD);
					if (nChunk != null && nChunk.Active) {
						// this will add elements to the array we are iterating on,
						// but we only iterate on prev count, so it should be fine
						QueueChunkUpdate(nChunk, nCPosX, nCPosY, u.lOD);
					}
				}

				// in case chunk got joined
				if (u.lOD > 0) {
					int cPosUpperX = u.cPosX * 2;
					int cPosUpperY = u.cPosY * 2;
					int nLOD = u.lOD - 1;

					for (int rD = 0; rD < 8; rD++) {
						int nCPosUpperX = cPosUpperX + _sRDirs[rD, 0];
						int nCPosUpperY = cPosUpperY + _sRDirs[rD, 1];

						Chunk nChunk = GetChunkAt(nCPosUpperX, nCPosUpperY, nLOD);
						if (nChunk != null && nChunk.Active) {
							// this will add elements to the array we are iterating on,
							// but we only iterate on prev count, so it should be fine
							QueueChunkUpdate(nChunk, nCPosUpperX, nCPosUpperY, nLOD);
						}
					}
				}
			}

			// update chunks
			bool lVisible = IsVisibleInTree();
			for (int i = 0; i < _pendingChunkUpdates.Count; i++) {
				u = _pendingChunkUpdates[i];
				Chunk chunk = GetChunkAt(u.cPosX, u.cPosY, u.lOD);
				/*if (chunk == null) {
					throw new Exception("'chunk == null'");
				}*/
				UpdateChunk(chunk, u.lOD, lVisible);
				_updatedChunks++;
			}

			_pendingChunkUpdates.Clear();
		}
	}
}
