using System;
using Godot;

namespace GDTerrain {

	public partial class TerrainData : Resource {

		public const int MAP_TYPE_COUNT = 8;
		/// <summary>Type index</summary>
		public const int
			MAP_HEIGHT = 0,
			MAP_NORMAL = 1,
			MAP_SPLAT = 2,
			MAP_COLOR = 3,
			MAP_DETAIL = 4,
			MAP_GLOBAL_ALBEDO = 5,
			MAP_SPLAT_IND = 6,
			MAP_SPLAT_WEI = 7;

		public sealed class MapTypeInfo {

			public string name;
			public Image.Format format;
			public Color[] defaultFillArr;
			public int defaultCount;
			public bool authored;

			public string GetShaderParamName(int index) {
				string result = "p_map_" + name;
				if (index != 0) {
					result += "_" + index;
				}
				return result;
			}

			public Color? GetDefaultFill(int index) {
				if (defaultFillArr == null || defaultFillArr.Length <= index) {
					return null;
				}
				return defaultFillArr[index];
			}
		}

		/// <summary>Contains information about different types of maps</summary>
		private static readonly MapTypeInfo[] _mapTypes = new MapTypeInfo[MAP_TYPE_COUNT] {
			new() {
				name = "height",
				format = Image.Format.Rh,
				defaultCount = 1,
				authored = true
			},
			new() {
				name = "normal",
				format = Image.Format.Rgb8,
				defaultFillArr = new Color[] { new(.5f, .5f, 1f) },
				defaultCount = 1,
				authored = false
			},
			new() {
				name = "splat",
				format = Image.Format.Rgba8,
				defaultFillArr = new Color[] { new(1f, 0f, 0f, 0f), new(0f, 0f, 0f, 0f) },
				defaultCount = 1,
				authored = true
			},
			new() {
				name = "color",
				format = Image.Format.Rgba8,
				defaultFillArr = new Color[] { new(1f, 1f, 1f) },
				defaultCount = 1,
				authored = true
			},
			new() {
				name = "detail",
				format = Image.Format.R8,
				defaultFillArr = new Color[] { new(0f, 0f, 0f) },
				defaultCount = 0,
				authored = true
			},
			new() {
				name = "global_albedo",
				format = Image.Format.Rgb8,
				defaultFillArr = null,
				defaultCount = 0,
				authored = false
			},
			new() {
				name = "splat_index",
				format = Image.Format.Rgb8,
				defaultFillArr = new Color[] { new(0f, 0f, 0f) },
				defaultCount = 0,
				authored = true
			},
			new() {
				name = "splat_weight",
				format = Image.Format.Rg8,
				defaultFillArr = new Color[] { new(1f, 0f, 0f) },
				defaultCount = 0,
				authored = true
			}
		};

		public sealed class Map {

			// For saving
			public int index;

			public ImageTexture texture;
			public Image image;
			public bool modified = true;

			public Map(int index) {
				this.index = index;
			}

			public Godot.Collections.Dictionary Serialize() {
				return new Godot.Collections.Dictionary() {
					{ "id", index }
				};
			}

			public static Map Deserialize(Godot.Collections.Dictionary value) {
				return new(value["id"].AsInt32());
			}

			public void CreateDefaultImage(int resolution, MapTypeInfo mapTypeInfo, int fillIndex = -1) {
				image = Image.Create(resolution, resolution, false, mapTypeInfo.format);

				if (fillIndex > -1) {
					Color? fillColor = mapTypeInfo.GetDefaultFill(fillIndex);
					if (fillColor.HasValue) {
						image.Fill(fillColor.Value);
					}
				}
			}
		}

		/// <summary>[typeIndex][index]</summary>
		private readonly Map[][] _arrayOfMapArrays = new Map[MAP_TYPE_COUNT][];

		/// <summary>Populates <paramref name="_arrayOfMapArrays"/> with default maps</summary>
		private void SetDefaultMaps() {
			Logger.DebugLog($"{nameof(TerrainData)}->{nameof(SetDefaultMaps)}");
			for (int tI = 0; tI < MAP_TYPE_COUNT; tI++) {
				Map[] maps = new Map[_mapTypes[tI].defaultCount];
				for (int i = 0; i < maps.Length; i++) {
					maps[i] = new(i);
				}
				_arrayOfMapArrays[tI] = maps;
			}
		}

		public int GetMapCount(int mapTypeIndex) {
			return _arrayOfMapArrays[mapTypeIndex].Length;
		}

		public Image GetMapImage(int mapTypeIndex, int index) {
			return _arrayOfMapArrays[mapTypeIndex][index].image;
		}

		public ImageTexture GetMapTexture(int mapTypeIndex, int index) {
			Map map = _arrayOfMapArrays[mapTypeIndex][index];

			if (map.image != null && map.texture == null) {
				UpdateMapTexture(mapTypeIndex, index);
			}

			return map.texture;
		}

		public float GetMapHeightAt(int x, int y) {
			Image image = GetMapImage(MAP_HEIGHT, 0);
			return Util.GetPixelClamped(image, x, y).r;
		}

		public static string GetMapShaderParamName(int mapTypeIndex, int index) {
			return _mapTypes[mapTypeIndex].GetShaderParamName(index);
		}

		[Signal]
		public delegate void MapChangedEventHandler(int typeIndex, int index);

		private void UpdateMapTextureRegion(int mapTypeIndex, int index, int minX, int minY, int sizeX, int sizeY) {
			Map map = _arrayOfMapArrays[mapTypeIndex][index];

			if (map.image == null) {
				throw new Exception("'image == null'");
			}

			if (map.texture == null) {
				map.texture = ImageTexture.CreateFromImage(map.image);
				_ = EmitSignal(SignalName.MapChanged, mapTypeIndex, index);
			} else if (map.texture.GetSize() != map.image.GetSize()) {
				map.texture = ImageTexture.CreateFromImage(map.image);
			} else {
				// TODO: partial texture update has not yet been implemented, so for now we are updating the full texture
				RenderingServer.Texture2dUpdate(map.texture.GetRid(), map.image, 0);
			}
		}

		private void UpdateMapTexture(int mapTypeIndex, int index) {
			UpdateMapTextureRegion(mapTypeIndex, index, 0, 0, Resolution, Resolution);
		}
	}
}
