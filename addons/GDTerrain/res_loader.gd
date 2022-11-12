@tool
class_name TerrainResLoader
extends ResourceFormatLoader

const SAVER = preload("./res_saver.gd")
const TYPE = "Resource"
const SCRIPT = preload("./TerrainData.cs")
const LOAD_METHOD_NAME = "Load"

func _get_recognized_extensions():
	return PackedStringArray([SAVER.EXTENSION])

func _get_resource_type(path):
	var extension = path.get_extension().to_lower()
	if extension == SAVER.EXTENSION:
		return TYPE
	return ""

func _handles_type(type):
	return type == TYPE

func _load(path, original_path, use_sub_threads, cache_mode):
	var resource = SCRIPT.new()
	resource.call(LOAD_METHOD_NAME, path.get_base_dir())
	return resource
