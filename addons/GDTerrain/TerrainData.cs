using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Godot;

namespace GDTerrain {

	[Tool]
	public partial class TerrainData : Resource {

		public const string DEFAULT_FILENAME = "data";
		public const string EXTENSION = "terrain";

		#region Misc
		private static string GetFilePath(string dirPath, string fileName, string extension) {
			return string.Concat(dirPath, '/', fileName, '.', extension);
		}

		private static string GetFilePath(string dirPath) {
			return GetFilePath(dirPath, DEFAULT_FILENAME, EXTENSION);
		}

		private static string GetMapPath(string dirPath, MapTypeInfo mapTypeInfo, int index) {
			return GetFilePath(dirPath, mapTypeInfo.name + index, "res");
		}
		#endregion

		public void SetDefault() {
			SetDefaultMaps();
			Resize(DEFAULT_RESOLUTION, true, -Vector2i.One);
		}

		public TerrainData() : base() {
			Logger.DebugLog($"[{nameof(TerrainData)}] Init...");
			SetDefault();
		}

		#region Save
		private string SerializeArrayOfMapArrays() {
			// types
			Godot.Collections.Array typeArr = new();
			_ = typeArr.Resize(MAP_TYPE_COUNT);
			for (int tI = 0; tI < typeArr.Count; tI++) {
				// maps
				Map[] maps = _arrayOfMapArrays[tI];
				Godot.Collections.Array mapArr = new();
				_ = mapArr.Resize(maps.Length);

				for (int i = 0; i < mapArr.Count; i++) {
					mapArr[i] = maps[i].Serialize();
				}
				typeArr[tI] = mapArr;
			}
			return JSON.Stringify(typeArr, "\t");
		}

		private void SaveArrayOfMapArrays(string fPath) {
			using FileAccess file = FileAccess.Open(fPath, FileAccess.ModeFlags.Write);
			file.StoreString(SerializeArrayOfMapArrays());
		}

		private void SaveMap(string dirPath, int mapTypeIndex, int index) {
			MapTypeInfo mapTypeInfo = _mapTypes[mapTypeIndex];
			Map map = _arrayOfMapArrays[mapTypeIndex][index];

			if (map.image == null) {
				if (map.texture != null) {
					Logger.DebugLog($"[{nameof(TerrainData)}->{nameof(SaveMap)}] Could not find '{nameof(map.image)}' for '{mapTypeInfo.name}'->{index}, loading from '{nameof(map.texture)}'...");
					map.image = map.texture.GetImage();
				} else {
					Logger.DebugLogError($"[{nameof(TerrainData)}->{nameof(SaveMap)}] Could not find '{nameof(map.image)}' for '{mapTypeInfo.name}'->{index}");
					return;
				}
			}

			string fPath = GetMapPath(dirPath, mapTypeInfo, index);

			Error errorCode = ResourceSaver.Save(map.image, fPath);
			if (errorCode != Error.Ok) {
				Logger.DebugLogError($"[{nameof(TerrainData)}->{nameof(SaveMap)}] '{errorCode}'");
			}
		}

		public long Save(string dirPath) {
			Logger.DebugLog($"{nameof(TerrainData)}->{nameof(Save)}->{dirPath}");

			SaveArrayOfMapArrays(GetFilePath(dirPath));

			for (int tI = 0; tI < MAP_TYPE_COUNT; tI++) {
				MapTypeInfo mapTypeInfo = _mapTypes[tI];
				Map[] maps = _arrayOfMapArrays[tI];

				for (int i = 0; i < maps.Length; i++) {
					Map map = maps[i];

					if (!map.modified) {
						Logger.DebugLog($"[{nameof(TerrainData)}->{nameof(Save)}] Skipping non-modified '{mapTypeInfo.name}'->{i}...");
						continue;
					}

					Logger.DebugLog($"[{nameof(TerrainData)}->{nameof(Save)}] Saving '{mapTypeInfo.name}'->{i}...");

					SaveMap(dirPath, tI, i);

					map.modified = false;
				}
			}

			return (long)Error.Ok;
		}
		#endregion

		#region Load
		private void DeserializeArrayOfMapArrays(string jSON) {
			// types
			Godot.Collections.Array typeArr = JSON.ParseString(jSON).AsGodotArray();
			for (int tI = 0; tI < typeArr.Count; tI++) {
				// maps
				Godot.Collections.Array mapArr = typeArr[tI].AsGodotArray();
				Map[] maps = new Map[mapArr.Count];

				for (int i = 0; i < maps.Length; i++) {
					maps[i] = Map.Deserialize(mapArr[i]);
				}
				_arrayOfMapArrays[tI] = maps;
			}
		}

		private void LoadArrayOfMapArrays(string fPath) {
			using FileAccess file = FileAccess.Open(fPath, FileAccess.ModeFlags.Read);
			DeserializeArrayOfMapArrays(file.GetAsText());
		}

		private void LoadMap(string dirPath, int mapTypeIndex, int index) {
			MapTypeInfo mapTypeInfo = _mapTypes[mapTypeIndex];
			Map map = _arrayOfMapArrays[mapTypeIndex][index];
			string fPath = GetMapPath(dirPath, mapTypeInfo, index);

			Image image = ResourceLoader.Load<Image>(fPath, null, ResourceLoader.CacheMode.Replace);
			if (image == null) {
				Logger.DebugLogError($"[{nameof(TerrainData)}->{nameof(LoadMap)}] Could not load '{fPath}'");
				return;
			}

			if (mapTypeInfo.name == "height") {
				Resolution = image.GetWidth();
			}

			map.image = image;
			UpdateTexture(mapTypeIndex, index);
		}

		private void Load(string dirPath) {
			Logger.DebugLog($"{nameof(TerrainData)}->{nameof(Load)}->{dirPath}");

			LoadArrayOfMapArrays(GetFilePath(dirPath));

			for (int tI = 0; tI < MAP_TYPE_COUNT; tI++) {
				MapTypeInfo mapTypeInfo = _mapTypes[tI];
				Map[] maps = _arrayOfMapArrays[tI];

				for (int i = 0; i < maps.Length; i++) {
					Logger.DebugLog($"[{nameof(TerrainData)}->{nameof(Load)}] Loading '{mapTypeInfo.name}'->{i}...");
					LoadMap(dirPath, tI, i);
					// a map that has just been loaded is not considered modified
					_arrayOfMapArrays[tI][i].modified = false;
				}
			}

			UpdateAllVerticalBounds();
			EmitSignal(SignalName.ResolutionChanged);
		}
		#endregion

		#region GDScript
#pragma warning disable CA1822 // Mark members as static
		/// <summary>For GDScript</summary>
		public void IsTerrainData() { }

		/// <summary>For GDScript</summary>
		public string GetExtension() {
			return EXTENSION;
		}
#pragma warning restore CA1822
		#endregion

		[Signal]
		public delegate void MapChangedEventHandler(int typeIndex, int index);

		private void UpdateTextureRegion(int mapTypeIndex, int index, int minX, int minY, int sizeX, int sizeY) {
			Map map = _arrayOfMapArrays[mapTypeIndex][index];

			if (map.image == null) {
				throw new Exception("'image == null'");
			}

			if (map.texture == null) {
				map.texture = ImageTexture.CreateFromImage(map.image);
				EmitSignal(SignalName.MapChanged, mapTypeIndex, index);
			} else if (map.texture.GetSize() != map.image.GetSize()) {
				map.texture = ImageTexture.CreateFromImage(map.image);
			} else {
				// TODO: partial texture update has not yet been implemented, so for now we are updating the full texture
				RenderingServer.Texture2dUpdate(map.texture.GetRid(), map.image, 0);
			}
		}

		private void UpdateTexture(int mapTypeIndex, int index) {
			UpdateTextureRegion(mapTypeIndex, index, 0, 0, Resolution, Resolution);
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
				UpdateTextureRegion(mapTypeIndex, mapIndex, min.x, min.y, size.x, size.y);
			}

			_arrayOfMapArrays[mapTypeIndex][mapIndex].modified = true;

			EmitSignal(SignalName.RegionChanged, min.x, min.y, size.x, size.y, mapTypeIndex);
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
			Image heightmap = GetMapImage(MAP_HEIGHT);
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
