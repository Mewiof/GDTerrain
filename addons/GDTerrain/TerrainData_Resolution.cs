using System.Runtime.CompilerServices;
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

		public int Resolution {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			private set;
		}

		[Signal]
		public delegate void ResolutionChangedEventHandler();

		/// <param name="newResolution">Must be power of 2 + 1</param>
		/// <param name="anchor">If <paramref name="stretch"/> is 'false', decides which side or corner to crop/expand the terrain from</param>
		public void Resize(int newResolution, bool stretch, Vector2i anchor) {
			newResolution = Util.Clamp(newResolution, MIN_RESOLUTION, MAX_RESOLUTION);
			newResolution = Util.NextPowerOfTwo(newResolution - 1) + 1;

			Logger.DebugLog($"[{nameof(TerrainData)}->{nameof(Resize)}] {Resolution}->{newResolution}");

			Resolution = newResolution;

			MapTypeInfo mapTypeInfo;
			Map[] maps;
			Map map;

			for (int tI = 0; tI < MAP_TYPE_COUNT; tI++) {
				mapTypeInfo = _mapTypes[tI];
				maps = _arrayOfMapArrays[tI];

				for (int i = 0; i < maps.Length; i++) {
					map = maps[i];

					Logger.DebugLog($"{nameof(TerrainData)}->{nameof(Resize)}->{mapTypeInfo.name}->{i}");

					if (map.image == null) {
						Logger.DebugLog($"[{nameof(TerrainData)}->{nameof(Resize)}] '{nameof(map.image)}' is null. Creating...");
						map.image = Image.Create(Resolution, Resolution, false, mapTypeInfo.format);

						Color? fillColor = GetMapDefaultFill(tI, i);
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
								Color? fillColor = GetMapDefaultFill(tI, i);
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
