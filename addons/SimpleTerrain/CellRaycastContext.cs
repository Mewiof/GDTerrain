using Godot;

namespace SimpleTerrain {

	public class CellRaycastContext {

		public Vector3 beginPos;
		private float _cellBeginPosY;
		private Vector2 _cellBeginPos2D;
		public Vector3 dir;
		public Vector2 dir2D;
		public Image verticalBounds;
		public Vector3 hit;
		public Image heightmap;
		public float broadParam2DTo3D;
		public float cellParam2DTo3D;

		public bool BroadFunc(int cX, int cZ, float enterParam, float exitParam) {
			if (cX < 0 || cZ < 0 || cZ >= verticalBounds.GetHeight() || cX >= verticalBounds.GetWidth()) {
				// may occasionally be called at boundary values
				return false;
			}

			Color vB = verticalBounds.GetPixel(cX, cZ);
			Vector3 begin = beginPos + (dir * (enterParam * broadParam2DTo3D));
			float exitY = beginPos.y + (dir.y * exitParam * broadParam2DTo3D);
			if (begin.y < vB.r || exitY > vB.g) {
				// not hitting this chunk
				return false;
			}

			// we may be hitting something in this chunk,
			// perform a narrow phase through terrain cells
			float distanceInChunk2D = (exitParam - enterParam) * TerrainData.VERTICAL_BOUNDS_CHUNK_SIZE;
			Vector2 cellRayOrigin2D = begin.XZ();
			_cellBeginPosY = begin.y;
			_cellBeginPos2D = cellRayOrigin2D;
			Util.GridRaytraceResult2D? rHit = Util.GridRaytrace2D(cellRayOrigin2D, dir2D, CellFunc, distanceInChunk2D);
			return rHit.HasValue;
		}

		private static Variant IntersectCell(Image heightmap, int cX, int cZ, Vector3 beginPos, Vector3 dir) {
			float h00 = Util.GetPixelClamped(heightmap, cX, cZ).r;
			float h10 = Util.GetPixelClamped(heightmap, cX + 1, cZ).r;
			float h01 = Util.GetPixelClamped(heightmap, cX, cZ + 1).r;
			float h11 = Util.GetPixelClamped(heightmap, cX + 1, cZ + 1).r;

			Vector3 p00 = new(cX, h00, cZ);
			Vector3 p10 = new(cX + 1, h10, cZ);
			Vector3 p01 = new(cX, h01, cZ + 1);
			Vector3 p11 = new(cX + 1, h11, cZ + 1);

			Variant t0 = Geometry3D.RayIntersectsTriangle(beginPos, dir, p00, p10, p11);
			Variant t1 = Geometry3D.RayIntersectsTriangle(beginPos, dir, p00, p11, p01);

			if (t0.VariantType != Variant.Type.Nil) {
				return t0;
			}
			return t1;
		}

		public bool CellFunc(int cX, int cZ, float enterParam, float exitParam) {
			Vector2 enterPos = _cellBeginPos2D + dir2D * enterParam;

			float enterY = _cellBeginPosY + dir.y * enterParam * cellParam2DTo3D;

			Variant hit = IntersectCell(heightmap, cX, cZ, new(enterPos.x, enterY, enterPos.y), dir);

			if (hit.VariantType != Variant.Type.Nil) {
				this.hit = hit.AsVector3();
				return true;
			}

			this.hit = Vector3.Zero;
			return false;
		}
	}
}
