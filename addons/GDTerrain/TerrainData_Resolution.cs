using Godot;

namespace GDTerrain {

	public partial class TerrainData : Resource {

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

		public int Resolution { get; private set; }

		[Signal]
		public delegate void ResolutionChangedEventHandler();

		/// <param name="newResolution">Must be power of 2 + 1</param>
		/// <param name="anchor">If <paramref name="stretch"/> is 'false', decides which side or corner to crop/expand the terrain from</param>
		public void Resize(int newResolution, bool stretch, Vector2i anchor) {
			newResolution = Util.Clamp(newResolution, MIN_RESOLUTION, MAX_RESOLUTION);
			newResolution = Util.NextPowerOfTwo(newResolution - 1) + 1;

			Logger.DebugLog($"[{nameof(TerrainData)}->{nameof(Resize)}] '{Resolution}->{newResolution}'");
			Resolution = newResolution;

			for (int tI = 0; tI < MAP_TYPE_COUNT; tI++) {
				MapTypeInfo mapTypeInfo = _mapTypes[tI];
				Map[] maps = _arrayOfMapArrays[tI];

				for (int i = 0; i < maps.Length; i++) {
					Map map = maps[i];
					Logger.DebugLog($"{nameof(TerrainData)}->{nameof(Resize)}->{mapTypeInfo.name}->{i}");

					if (map.image == null) {
						Logger.DebugLog($"[{nameof(TerrainData)}->{nameof(Resize)}] '{nameof(map.image)}' is null. Creating...");
						map.CreateDefaultImage(Resolution, mapTypeInfo, i);
					} else {
						if (stretch && !mapTypeInfo.authored) {
							map.CreateDefaultImage(Resolution, mapTypeInfo);
						} else {
							if (stretch) {
								map.image.Resize(Resolution, Resolution);
							} else {
								Color? fillColor = mapTypeInfo.GetDefaultFill(i);
								map.image = Util.GetCroppedImage(map.image, Resolution, Resolution, fillColor, anchor);
							}
						}
					}

					map.modified = true;
				}
			}

			UpdateAllVerticalBounds();
			_ = EmitSignal(SignalName.ResolutionChanged);
		}
	}
}
