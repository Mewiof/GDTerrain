using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Godot;

namespace GDTerrain {

	[Tool]
	public partial class TerrainData : Resource {

		public TerrainData() : base() {
			Logger.DebugLog($"[{nameof(TerrainData)}] Init...");
			SetDefaultMaps();
			Resize(DEFAULT_RESOLUTION, true, -Vector2i.One);
		}

		[Signal]
		public delegate void RegionChangedEventHandler(int x, int y, int w, int h, int mapTypeIndex);

		public void NotifyRegionChange(Rect2i rect, int mapTypeIndex, int mapIndex, bool uploadToTexture, bool updateVerticalBounds) {
			Vector2i min = rect.Position;
			Vector2i size = rect.Size;

			if (mapTypeIndex == MAP_HEIGHT && updateVerticalBounds) {
				UpdateVerticalBounds(min.x, min.y, size.x, size.y);
			}

			if (uploadToTexture) {
				UpdateMapTextureRegion(mapTypeIndex, mapIndex, min.x, min.y, size.x, size.y);
			}

			_arrayOfMapArrays[mapTypeIndex][mapIndex].modified = true;

			_ = EmitSignal(SignalName.RegionChanged, min.x, min.y, size.x, size.y, mapTypeIndex);
			EmitChanged();
		}

		public void NotifyFullChange() {//?
			for (int tI = 0; tI < MAP_TYPE_COUNT; tI++) {
				if (tI == MAP_NORMAL) {
					// ignore normals, because they get updated along with heights
					continue;
				}
				Map[] maps = _arrayOfMapArrays[tI];
				for (int i = 0; i < maps.Length; i++) {
					NotifyRegionChange(new(0, 0, Resolution, Resolution), tI, i, true, true);
				}
			}
		}

		/// <summary>Mathematical, does not use collisions</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Vector2i? CellRaycast(Vector3 origin, Vector3 direction, float maxDistance) {
			Image heightmap = GetMapImage(MAP_HEIGHT, 0);
			if (heightmap == null) {
				return null;
			}

			Rect2 terrainRect = new(Vector2.Zero, new(Resolution, Resolution));
			Vector2 rayOrigin2D = origin.XZ();
			Vector2 rayEnd2D = (origin + (direction * maxDistance)).XZ();
			List<Vector2> clippedSegment2D = Util.GetSegmentClippedByRect(terrainRect, rayOrigin2D, rayEnd2D);

			if (clippedSegment2D.Count == 0) {
				// not hitting the terrain
				return null;
			}

			float maxDistance2D = rayOrigin2D.DistanceTo(rayEnd2D);
			if (maxDistance2D < .001f) {
				// not hitting the terrain
				return null;
			}

			float beginClipParam = rayOrigin2D.DistanceTo(clippedSegment2D[0]) / maxDistance2D;
			Vector2 rayDirection2D = direction.XZ().Normalized();
			float cellParam2DTo3D = maxDistance / maxDistance2D;

			CellRaycastContext context = CellRaycastContext.instance;
			context.Set(
				origin + (direction * (beginClipParam * maxDistance)),
				direction,
				rayDirection2D,
				_chunkedVerticalBounds,
				heightmap,
				cellParam2DTo3D * VERTICAL_BOUNDS_CHUNK_SIZE,
				cellParam2DTo3D
				);

			Vector2 broadRayOrigin = clippedSegment2D[0] / VERTICAL_BOUNDS_CHUNK_SIZE;
			float broadMaxDistance = clippedSegment2D[0].DistanceTo(clippedSegment2D[1]) / VERTICAL_BOUNDS_CHUNK_SIZE;
			Util.GridRaytraceResult2D? hit = Util.GridRaytrace2D(broadRayOrigin, rayDirection2D, context.BroadFunc, broadMaxDistance);

			if (hit == null) {
				return null;
			}

			return new((int)context.hit.x, (int)context.hit.z);//?
		}
	}
}
