using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Godot;

namespace GDTerrain {

	public partial class TerrainData : Resource {

		public const string DEFAULT_FILENAME = "data";
		public const string EXTENSION = "terrain";

		#region Misc
		private static string GetPath(string dirPath, string fileName, string extension) {
			return string.Concat(dirPath, '/', fileName, '.', extension);
		}

		private static string GetFilePath(string dirPath) {
			return GetPath(dirPath, DEFAULT_FILENAME, EXTENSION);
		}

		private static string GetMapPath(string dirPath, MapTypeInfo mapTypeInfo, int index) {
			return GetPath(dirPath, mapTypeInfo.name + index, "res");
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
		#endregion

		#region Maps
		public const int MAP_TYPE_COUNT = 8;
		/// <summary>Type index</summary>
		public const int
			MAP_HEIGHT = 0,
			MAP_NORMAL = 1,
			MAP_SPLAT = 2,
			MAP_COLOR = 3,
			MAP_DETAIL = 4,
			MAP_GLOBAL_ALBEDO = 5,
			MAP_SPLAT_INDEX = 6,
			MAP_SPLAT_WEIGHT = 7;

		#region Map Type Info
		public class MapTypeInfo {

			public string name;
			public string[] shaderParamName;
			public Image.Format format;
			public Color[] defaultFill;
			public int defaultCount;
			public bool authored;
		}

		/// <summary>Contains information about different types of maps</summary>
		private static readonly MapTypeInfo[] _mapTypes = new MapTypeInfo[MAP_TYPE_COUNT] {
			new() {
				name = "height",
				shaderParamName = new string[] { "u_terrain_heightmap" },
				format = Image.Format.Rh,
				defaultFill = null,
				defaultCount = 1,
				authored = true
			},
			new() {
				name = "normal",
				shaderParamName = new string[] { "u_terrain_normalmap" },
				format = Image.Format.Rgb8,
				defaultFill = new Color[] { new(.5f, .5f, 1f) },
				defaultCount = 1,
				authored = false
			},
			new() {
				name = "splat",
				shaderParamName = new string[] { "u_terrain_splatmap", "u_terrain_splatmap_1", "u_terrain_splatmap_2", "u_terrain_splatmap_3" },
				format = Image.Format.Rgba8,
				defaultFill = new Color[] { new(1f, 0f, 0f, 0f), new(0f, 0f, 0f, 0f) },
				defaultCount = 1,
				authored = true
			},
			new() {
				name = "color",
				shaderParamName = new string[] { "u_terrain_colormap" },
				format = Image.Format.Rgba8,
				defaultFill = new Color[] { new(1f, 1f, 1f, 1f) },
				defaultCount = 1,
				authored = true
			},
			new() {
				name = "detail",
				shaderParamName = new string[] { "u_terrain_detailmap" },
				format = Image.Format.R8,
				defaultFill = new Color[] { new(0f, 0f, 0f) },
				defaultCount = 0,
				authored = true
			},
			new() {
				name = "global_albedo",
				shaderParamName = new string[] { "u_terrain_globalmap" },
				format = Image.Format.Rgb8,
				defaultFill = null,
				defaultCount = 0,
				authored = false
			},
			new() {
				name = "splat_index",
				shaderParamName = new string[] { "u_terrain_splat_index_map" },
				format = Image.Format.Rgb8,
				defaultFill = new Color[] { new(0f, 0f, 0f) },
				defaultCount = 0,
				authored = true
			},
			new() {
				name = "splat_weight",
				shaderParamName = new string[] { "u_terrain_splat_weight_map" },
				format = Image.Format.Rg8,
				defaultFill = new Color[] { new(1f, 0f, 0f) },
				defaultCount = 0,
				authored = true
			}
		};
		#endregion

		/// <summary>[typeIndex][index]</summary>
		private readonly Map[][] _arrayOfMapArrays = new Map[MAP_TYPE_COUNT][];

		#region Misc
		/// <summary>Populates <paramref name="_arrayOfMapArrays"/> with default maps</summary>
		private void SetDefaultMaps() {
			Plugin.DebugLog($"{nameof(TerrainData)}->{nameof(SetDefaultMaps)}");
			for (int tI = 0; tI < MAP_TYPE_COUNT; tI++) {
				Map[] maps = new Map[_mapTypes[tI].defaultCount];
				for (int i = 0; i < maps.Length; i++) {
					maps[i] = new(i);
				}
				_arrayOfMapArrays[tI] = maps;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Image GetMapImage(int mapTypeIndex, int index = 0) {
			return _arrayOfMapArrays[mapTypeIndex][index].image;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ImageTexture GetMapTexture(int mapTypeIndex, int index = 0) {
			return _arrayOfMapArrays[mapTypeIndex][index].texture;
		}
		#endregion
		#endregion

		#region Resolution
		// Power of 2 + 1
		private const int MAX_RESOLUTION = 8193;
		private const int MIN_RESOLUTION = 65; // Must not be smaller than max chunk size
		private const int DEFAULT_RESOLUTION = 513;
		public static readonly int[] supportedResolutions = new int[] {
			65,
			129,
			257,
			513,
			1025,
			2049,
			4097,
			8193 // :skull:
		};

		public int Resolution {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			private set;
		}

		public Action resolutionChanged;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static Color? GetMapDefaultFill(Color[] arr, int mapIndex) {//?
			if (arr == null || arr.Length < 1) {
				return null;
			}
			if (arr.Length == 1 || mapIndex == 0) {
				return arr[0];
			}
			return arr[mapIndex];
		}

		/// <param name="newResolution">Must be power of 2 + 1</param>
		/// <param name="anchor">If <paramref name="stretch"/> is 'false', decides which side or corner to crop/expand the terrain from</param>
		public void Resize(int newResolution, bool stretch, Vector2i anchor) {
			newResolution = Util.Clamp(newResolution, MIN_RESOLUTION, MAX_RESOLUTION);
			newResolution = Util.NextPowerOfTwo(newResolution - 1) + 1;

			Plugin.DebugLog($"[{nameof(TerrainData)}->{nameof(Resize)}] {Resolution}->{newResolution}");

			Resolution = newResolution;

			MapTypeInfo mapTypeInfo;
			Map[] maps;
			Map map;

			for (int tI = 0; tI < MAP_TYPE_COUNT; tI++) {
				mapTypeInfo = _mapTypes[tI];
				maps = _arrayOfMapArrays[tI];

				for (int i = 0; i < maps.Length; i++) {
					map = maps[i];

					Plugin.DebugLog($"{nameof(TerrainData)}->{nameof(Resize)}->{mapTypeInfo.name}->{i}");

					if (map.image == null) {
						Plugin.DebugLog($"[{nameof(TerrainData)}->{nameof(Resize)}] '{nameof(map.image)}' is null. Creating...");
						map.image = Image.Create(Resolution, Resolution, false, mapTypeInfo.format);

						Color? fillColor = GetMapDefaultFill(mapTypeInfo.defaultFill, i);
						if (fillColor.HasValue) {
							map.image.Fill(fillColor.Value);
						}
					} else {
						if (stretch && !mapTypeInfo.authored) {
							map.image = Image.Create(Resolution, Resolution, false, mapTypeInfo.format);
						} else {
							if (stretch) {
								map.image.Resize(Resolution, Resolution);
							} else {
								Color? fillColor = GetMapDefaultFill(mapTypeInfo.defaultFill, i);
								map.image = Util.GetCroppedImage(map.image, Resolution, Resolution, fillColor, anchor);
							}
						}
					}

					map.modified = true;
				}
			}

			UpdateAllVerticalBounds();
			resolutionChanged?.Invoke();
		}
		#endregion

		#region VerticalBounds
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

		/// <summary>Sets <paramref name="_chunkedVerticalBounds"/> using <paramref name="_resolution"/></summary>
		private void UpdateAllVerticalBounds() {
			int cSizeX = Resolution / VERTICAL_BOUNDS_CHUNK_SIZE;
			int cSizeY = cSizeX;
			Plugin.DebugLog($"[{nameof(TerrainData)}->{nameof(UpdateAllVerticalBounds)}] {cSizeX}x{cSizeY} chunks");
			_chunkedVerticalBounds = Image.Create(cSizeX, cSizeY, false, Image.Format.Rgf);
			UpdateVerticalBounds(0, 0, Resolution - 1, Resolution - 1);
		}
		#endregion

		public void SetDefault() {
			SetDefaultMaps();
			Resize(DEFAULT_RESOLUTION, true, -Vector2i.One);
		}

		public TerrainData() : base() {
			Plugin.DebugLog($"[{nameof(TerrainData)}] Init...");
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
					Plugin.DebugLog($"[{nameof(TerrainData)}->{nameof(SaveMap)}] Could not find '{nameof(map.image)}' for '{mapTypeInfo.name}'->{index}, loading from '{nameof(map.texture)}'...");
					map.image = map.texture.GetImage();
				} else {
					Plugin.DebugLogError($"[{nameof(TerrainData)}->{nameof(SaveMap)}] Could not find '{nameof(map.image)}' for '{mapTypeInfo.name}'->{index}");
					return;
				}
			}

			string fPath = GetMapPath(dirPath, mapTypeInfo, index);

			Error errorCode = ResourceSaver.Save(map.image, fPath);
			if (errorCode != Error.Ok) {
				Plugin.DebugLogError($"[{nameof(TerrainData)}->{nameof(SaveMap)}] '{errorCode}'");
			}
		}

		public long Save(string dirPath) {
			Plugin.DebugLog($"{nameof(TerrainData)}->{nameof(Save)}->{dirPath}");

			SaveArrayOfMapArrays(GetFilePath(dirPath));

			for (int tI = 0; tI < MAP_TYPE_COUNT; tI++) {
				MapTypeInfo mapTypeInfo = _mapTypes[tI];
				Map[] maps = _arrayOfMapArrays[tI];

				for (int i = 0; i < maps.Length; i++) {
					Map map = maps[i];

					if (!map.modified) {
						Plugin.DebugLog($"[{nameof(TerrainData)}->{nameof(Save)}] Skipping non-modified '{mapTypeInfo.name}'->{i}...");
						continue;
					}

					Plugin.DebugLog($"[{nameof(TerrainData)}->{nameof(Save)}] Saving '{mapTypeInfo.name}'->{i}...");

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
				Plugin.DebugLogError($"[{nameof(TerrainData)}->{nameof(LoadMap)}] Could not load '{fPath}'");
				return;
			}

			Resolution = Math.Max(Resolution, image.GetWidth());//?

			map.image = image;
			UpdateTexture(mapTypeIndex, index);//?
		}

		private void Load(string dirPath) {
			Plugin.DebugLog($"{nameof(TerrainData)}->{nameof(Load)}->{dirPath}");

			LoadArrayOfMapArrays(GetFilePath(dirPath));

			for (int tI = 0; tI < MAP_TYPE_COUNT; tI++) {
				MapTypeInfo mapTypeInfo = _mapTypes[tI];
				Map[] maps = _arrayOfMapArrays[tI];

				for (int i = 0; i < maps.Length; i++) {
					Plugin.DebugLog($"[{nameof(TerrainData)}->{nameof(Load)}] Loading '{mapTypeInfo.name}'->{i}...");
					LoadMap(dirPath, tI, i);
					_arrayOfMapArrays[tI][i].modified = false;//?
				}
			}

			UpdateAllVerticalBounds();
			resolutionChanged?.Invoke();
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
#pragma warning restore CA1822 // Mark members as static
		#endregion

		/// <summary>typeIndex, index</summary>
		public Action<int, int> mapChanged;

		private void UpdateTexture(int mapTypeIndex, int index) {//?
			Map map = _arrayOfMapArrays[mapTypeIndex][index];

			if (map.image == null) {
				throw new Exception("'image == null'");
			}

			if (map.texture == null) {
				map.texture = ImageTexture.CreateFromImage(map.image);
				mapChanged?.Invoke(mapTypeIndex, index);
			} else if (map.texture.GetSize() != map.image.GetSize()) {
				map.texture = ImageTexture.CreateFromImage(map.image);
			} else {
				// TODO: partial texture update has not yet been implemented, so for now we are updating the full texture
				RenderingServer.Texture2dUpdate(map.texture.GetRid(), map.image, 0);
			}
		}

		/// <summary>x, y, w, h, mapTypeIndex</summary>
		public Action<int, int, int, int, int> regionChanged;

		public void NotifyRegionChange(Rect2i rect, int mapTypeIndex, int mapIndex, bool uploadToTexture, bool updateVerticalBounds) {
			Vector2i min = rect.Position;
			Vector2i size = rect.Size;

			if (mapTypeIndex == MAP_HEIGHT && updateVerticalBounds) {
				UpdateVerticalBounds(min.x, min.y, size.x, size.y);
			}

			if (uploadToTexture) {
				UpdateTexture(mapTypeIndex, mapIndex);
			}

			_arrayOfMapArrays[mapTypeIndex][mapIndex].modified = true;

			regionChanged?.Invoke(min.x, min.y, size.x, size.y, mapTypeIndex);//?
			EmitChanged();
		}

		/// <summary>Mathematical, does not use collisions</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Vector2i? CellRaycast(Vector3 origin, Vector3 direction, float maxDistance) {
			Image heightmap = GetMapImage(MAP_HEIGHT);
			if (heightmap == null) {
				return null;
			}

			Rect2 terrainRect = new(default, new(Resolution, Resolution));
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
			context.broadParam2DTo3D = context.cellParam2DTo3D * VERTICAL_BOUNDS_CHUNK_SIZE;

			Vector2 broadRayOrigin = clippedSegment2D[0] / VERTICAL_BOUNDS_CHUNK_SIZE;
			float broadMaxDistance = clippedSegment2D[0].DistanceTo(clippedSegment2D[1]) / VERTICAL_BOUNDS_CHUNK_SIZE;
			Util.GridRaytraceResult2D? hit = Util.GridRaytrace2D(broadRayOrigin, rayDirection2D, context.BroadFunc, broadMaxDistance);

			if (hit == null) {
				return null;
			}

			return new((int)context.hit.x, (int)context.hit.z);
		}
	}
}
