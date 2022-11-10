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
			MAP_SPLAT_INDEX = 6,
			MAP_SPLAT_WEIGHT = 7;

		public sealed class MapTypeInfo {

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

		public Image GetMapImage(int mapTypeIndex, int index = 0) {
			return _arrayOfMapArrays[mapTypeIndex][index].image;
		}

		public ImageTexture GetMapTexture(int mapTypeIndex, int index = 0) {
			return _arrayOfMapArrays[mapTypeIndex][index].texture;
		}

		public float GetMapHeightAt(int x, int y) {
			Image image = GetMapImage(MAP_HEIGHT);
			return Util.GetPixelClamped(image, x, y).r;
		}

		public static string GetMapShaderParamName(int mapTypeIndex, int index = 0) {
			return _mapTypes[mapTypeIndex].shaderParamName[index];
		}

		public static Color? GetMapDefaultFill(int mapTypeIndex, int index = 0) {
			Color[] value = _mapTypes[mapTypeIndex].defaultFill;
			if (value == null) {
				return null;
			}
			return value[index];
		}
	}
}
