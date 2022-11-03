using Godot;

namespace GDTerrain {

	public sealed class Map {

		public ImageTexture texture;
		public Image image;
		// For saving
		public int id = -1;
		/// <summary>Dirty</summary>
		public bool modified = true;

		public Map(int id) {
			this.id = id;
		}

		public Variant Serialize() {
			return new Godot.Collections.Dictionary() {
				{ "id", id }
			};
		}

		public static Map Deserialize(Variant value) {
			return new(value.AsGodotDictionary()["id"].AsInt32());
		}
	}
}
