using System;
using System.Runtime.CompilerServices;
using Godot;

namespace GDTerrain {

	public partial class TerrainData : Resource {

		public const int VERTICAL_BOUNDS_CHUNK_SIZE = 16;

		/// <summary>RGF image where 'r' is min height and 'g' is max height</summary>
		private Image _chunkedVerticalBounds = new();

		private Vector2 ComputeVerticalBoundsAt(int originX, int originY, int sizeX, int sizeY) {
			Image heightmap = GetMapImage(MAP_HEIGHT);
			return Util.GetRedRange(heightmap, new(originX, originY, sizeX, sizeY));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void UpdateVerticalBounds(int originInCelX, int originInCelY,
			int sizeInCelX, int sizeInCelY) {

			int cMinX = originInCelX / VERTICAL_BOUNDS_CHUNK_SIZE;
			int cMinY = originInCelY / VERTICAL_BOUNDS_CHUNK_SIZE;

			int cMaxX = (originInCelX + sizeInCelX - 1) / VERTICAL_BOUNDS_CHUNK_SIZE + 1;
			int cMaxY = (originInCelY + sizeInCelY - 1) / VERTICAL_BOUNDS_CHUNK_SIZE + 1;

			int cVertBWidth = _chunkedVerticalBounds.GetWidth();
			int cVertBHeight = _chunkedVerticalBounds.GetHeight();

			cMinX = Util.Clamp(cMinX, 0, cVertBWidth - 1);
			cMinY = Util.Clamp(cMinY, 0, cVertBHeight - 1);
			cMaxX = Util.Clamp(cMaxX, 0, cVertBWidth);
			cMaxY = Util.Clamp(cMaxY, 0, cVertBHeight);

			int chunkSizeX = VERTICAL_BOUNDS_CHUNK_SIZE + 1;
			int chunkSizeY = chunkSizeX;

			for (int y = cMinY; y < cMaxY; y++) {
				int pMinY = y * VERTICAL_BOUNDS_CHUNK_SIZE;
				for (int x = cMinX; x < cMaxX; x++) {
					int pMinX = x * VERTICAL_BOUNDS_CHUNK_SIZE;
					Vector2 b = ComputeVerticalBoundsAt(pMinX, pMinY, chunkSizeX, chunkSizeY);
					_chunkedVerticalBounds.SetPixel(x, y, new(b.x, b.y, 0f));
				}
			}
		}

		/// <summary>Sets <paramref name="_chunkedVerticalBounds"/> using <paramref name="Resolution"/></summary>
		private void UpdateAllVerticalBounds() {
			int cSizeX = Resolution / VERTICAL_BOUNDS_CHUNK_SIZE;
			int cSizeY = cSizeX;
			Logger.DebugLog($"[{nameof(TerrainData)}->{nameof(UpdateAllVerticalBounds)}] {cSizeX}x{cSizeY} chunks");
			_chunkedVerticalBounds = Image.Create(cSizeX, cSizeY, false, Image.Format.Rgf);
			UpdateVerticalBounds(0, 0, Resolution - 1, Resolution - 1);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Vector2 GetPointAABB(int cellX, int cellY) {

			int cX = cellX / VERTICAL_BOUNDS_CHUNK_SIZE;
			int cY = cellY / VERTICAL_BOUNDS_CHUNK_SIZE;

			if (cX < 0) {
				cX = 0;
			}
			if (cY < 0) {
				cY = 0;
			}

			int chVertBWidth = _chunkedVerticalBounds.GetWidth();
			int chVertBHeight = _chunkedVerticalBounds.GetHeight();

			if (cX >= chVertBWidth) {
				cX = chVertBWidth - 1;
			}
			if (cY >= chVertBHeight) {
				cY = chVertBHeight - 1;
			}

			Color b = _chunkedVerticalBounds.GetPixel(cX, cY);
			return new(b.r, b.g);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public AABB GetRegionAABB(int originInCelX, int originInCelY,
			int sizeInCelX, int sizeInCelY) {

			int cMinX = originInCelX / VERTICAL_BOUNDS_CHUNK_SIZE;
			int cMinY = originInCelY / VERTICAL_BOUNDS_CHUNK_SIZE;

			int cMaxX = ((originInCelX + sizeInCelX - 1) / VERTICAL_BOUNDS_CHUNK_SIZE) + 1;
			int cMaxY = ((originInCelY + sizeInCelY - 1) / VERTICAL_BOUNDS_CHUNK_SIZE) + 1;

			int cVBWidth = _chunkedVerticalBounds.GetWidth();
			int cVBHeight = _chunkedVerticalBounds.GetHeight();

			cMinX = Util.Clamp(cMinX, 0, cVBWidth - 1);
			cMinY = Util.Clamp(cMinY, 0, cVBHeight - 1);
			cMaxX = Util.Clamp(cMaxX, 0, cVBWidth);
			cMaxY = Util.Clamp(cMaxY, 0, cVBHeight);

			float minHeight = _chunkedVerticalBounds.GetPixel(cMinX, cMinY).r;
			float maxHeight = minHeight;

			for (int y = cMinY; y < cMaxY; y++) {
				for (int x = cMinX; x < cMaxX; x++) {
					Color b = _chunkedVerticalBounds.GetPixel(x, y);
					minHeight = MathF.Min(b.r, minHeight);
					maxHeight = MathF.Max(b.g, maxHeight);
				}
			}

			AABB result = new(new(originInCelX, minHeight, originInCelY), new(sizeInCelX, maxHeight - minHeight, sizeInCelY));

			return result;
		}
	}
}
