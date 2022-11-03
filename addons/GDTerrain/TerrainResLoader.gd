# Currently, "ResourceFormatSaver" / "ResourceFormatLoader" work through a** in C#, so we use GDScript

@tool
class_name TerrainResLoader
extends ResourceFormatLoader

const EXTENSION = "terrain" # ?
const TYPE_STR = "Resource"

func _get_recognized_extensions():
	return PackedStringArray([EXTENSION])

func _get_resource_type(path):
	var extension = path.get_extension().to_lower() # ?
	if extension == EXTENSION:
		return TYPE_STR
	return ""

func _handles_type(type):
	return type == TYPE_STR

func _load(path, original_path, use_sub_threads, cache_mode):
	var resource = load("res://addons/GDTerrain/TerrainData.cs").new()
	resource.call("Load", path.get_base_dir())
	return resource
