using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Godot;
using SimpleTerrain.Native;

namespace SimpleTerrain {

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

		// Directions to neighbour chunks
		private static readonly int[,] _sDirs = new int[4, 2] {
			{-1, 0},
			{1, 0},
			{0, -1},
			{0, 1}
		};

		// Directions to neighbour chunks of higher LOD
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
				Plugin.DebugLog($"Setting '{nameof(_chunkSize)}' to {cS}...");
				_chunkSize = cS;
				ResetGroundChunks();
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

		// Add to update queue
		private void AddChunkUpdate(Chunk chunk, int cPosX, int cPosY, int lOD) {
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

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private Chunk MakeChunk(int cPosX, int cPosY, int lOD) {
			Chunk chunk = GetChunkAt(cPosX, cPosY, lOD);

			if (chunk == null) {
				// first request at this LOD. Generate
				int lODFactor = QuadTreeLOD.GetLODFactor(lOD);//?
				int originInCelX = cPosX * _chunkSize * lODFactor;
				int originInCelY = cPosY * _chunkSize * lODFactor;

				chunk = new(this, originInCelX, originInCelY, null);//?
				chunk.OnParentTransformChanged(InternalTransform);

				//chunk.SetRenderLayerMask(_renderLayerMask);
				//chunk.SetCastShadowSetting(_castShadowSetting);

				SetChunkAt(cPosX, cPosY, lOD, chunk);
			}

			AddChunkUpdate(chunk, cPosX, cPosY, lOD);

			chunk.Active = true;
			return chunk;
		}

		private static void RecycleChunk(Chunk chunk, int cPosX, int cPosY, int lOD) {//?
			chunk.Visible = false;
			chunk.Active = false;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private Vector2 GetVerticalBounds(int cPosX, int cPosY, int lOD) {
			int chunkSize = _chunkSize * QuadTreeLOD.GetLODFactor(lOD);//?
			int originInCelX = cPosX * chunkSize;
			int originInCelY = cPosY * chunkSize;
			return _data.GetPointAABB(originInCelX + (chunkSize / 2), originInCelY + (chunkSize / 2));
		}

		// References
		private void ClearAllChunks() {
			_lodder.Clear();

			for (int i = 0; i < _chunkGrids.Count; i++) {
				_chunkGrids[i] = null;
			}
		}

		private void ResetGroundChunks() {
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
				Plugin.DebugLog($"Creating grid for {lOD} ({cRes}x{cRes})...");
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
	}
}
