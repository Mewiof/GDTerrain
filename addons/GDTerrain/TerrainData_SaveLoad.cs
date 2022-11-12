using Godot;

namespace GDTerrain {

	public partial class TerrainData : Resource {

		/// <summary>Don't forget to set the value in 'res_saver.gd' as well</summary>
		public const string EXTENSION = "terrain";
		public const string DEFAULT_FILENAME = "data";

		#region GDScript
#pragma warning disable CA1822 // Mark members as static
		public void IsTerrainData() { }
#pragma warning restore CA1822
		#endregion

		private static string GetFilePath(string dirPath, string filename, string extension) {
			return string.Concat(dirPath, '/', filename, '.', extension);
		}

		private static string GetDataFilePath(string dirPath) {
			return GetFilePath(dirPath, DEFAULT_FILENAME, EXTENSION);
		}

		private static string GetMapFilePath(string dirPath, MapTypeInfo mapTypeInfo, int index) {
			string filename = mapTypeInfo.name;
			if (index > 0) {
				filename += index;
			}
			return GetFilePath(dirPath, filename, "res");
		}

		private string SerializeArrayOfMapArrays() {
			// types
			Godot.Collections.Array typeArr = new();
			_ = typeArr.Resize(MAP_TYPE_COUNT);
			for (int tI = 0; tI < MAP_TYPE_COUNT; tI++) {
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
					Logger.DebugLog($"[{nameof(TerrainData)}->{nameof(SaveMap)}] Could not find '{nameof(map.image)}' for '{mapTypeInfo.name}->{index}'. Loading from '{nameof(map.texture)}'...");
					map.image = map.texture.GetImage();
				} else {
					Logger.DebugLogError($"[{nameof(TerrainData)}->{nameof(SaveMap)}] Could not find '{nameof(map.image)}' for '{mapTypeInfo.name}->{index}'");
					return;
				}
			}

			string fPath = GetMapFilePath(dirPath, mapTypeInfo, index);

			Error errorCode = ResourceSaver.Save(map.image, fPath);
			if (errorCode != Error.Ok) {
				Logger.DebugLogError($"[{nameof(TerrainData)}->{nameof(SaveMap)}] '{errorCode}'");
			}
		}

		public void Save(string dirPath) {
			Logger.DebugLog($"{nameof(TerrainData)}->{nameof(Save)}->'{dirPath}'");

			SaveArrayOfMapArrays(GetDataFilePath(dirPath));

			// maps
			for (int tI = 0; tI < MAP_TYPE_COUNT; tI++) {
				MapTypeInfo mapTypeInfo = _mapTypes[tI];
				Map[] maps = _arrayOfMapArrays[tI];

				for (int i = 0; i < maps.Length; i++) {
					Map map = maps[i];
					if (!map.modified) {
						Logger.DebugLog($"[{nameof(TerrainData)}->{nameof(Save)}] Skipping non-modified '{mapTypeInfo.name}->{i}'...");
						continue;
					}

					Logger.DebugLog($"[{nameof(TerrainData)}->{nameof(Save)}] Saving '{mapTypeInfo.name}->{i}'...");
					SaveMap(dirPath, tI, i);
					map.modified = false;
				}
			}
		}

		private void DeserializeArrayOfMapArrays(string jSON) {
			// types
			Godot.Collections.Array typeArr = JSON.ParseString(jSON).AsGodotArray();
			for (int tI = 0; tI < typeArr.Count; tI++) {
				// maps
				Godot.Collections.Array mapArr = typeArr[tI].AsGodotArray();
				Map[] maps = new Map[mapArr.Count];

				for (int i = 0; i < maps.Length; i++) {
					maps[i] = Map.Deserialize(mapArr[i].AsGodotDictionary());
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
			string fPath = GetMapFilePath(dirPath, mapTypeInfo, index);

			Image image = ResourceLoader.Load<Image>(fPath);
			if (image == null) {
				Logger.DebugLogError($"[{nameof(TerrainData)}->{nameof(LoadMap)}] Could not load '{fPath}'");
				return;
			}

			// heightmap? -> set resolution
			if (mapTypeInfo.name == "height") {
				Resolution = image.GetWidth();
			}

			map.image = image;
			UpdateMapTexture(mapTypeIndex, index);
		}

		private void Load(string dirPath) {
			Logger.DebugLog($"{nameof(TerrainData)}->{nameof(Load)}->'{dirPath}'");

			LoadArrayOfMapArrays(GetDataFilePath(dirPath));

			// maps
			for (int tI = 0; tI < MAP_TYPE_COUNT; tI++) {
				MapTypeInfo mapTypeInfo = _mapTypes[tI];
				Map[] maps = _arrayOfMapArrays[tI];
				for (int i = 0; i < maps.Length; i++) {
					Logger.DebugLog($"[{nameof(TerrainData)}->{nameof(Load)}] Loading '{mapTypeInfo.name}->{i}'...");
					LoadMap(dirPath, tI, i);
					_arrayOfMapArrays[tI][i].modified = false;
				}
			}

			UpdateAllVerticalBounds();
			_ = EmitSignal(SignalName.ResolutionChanged);
		}
	}
}
