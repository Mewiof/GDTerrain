@tool
class_name TerrainResSaver
extends ResourceFormatSaver

const VALIDATE_METHOD_NAME = "IsTerrainData"
const EXTENSION = "terrain"
const SAVE_METHOD_NAME = "Save"

func validate(resource):
	return resource != null && resource.has_method(VALIDATE_METHOD_NAME)

func _get_recognized_extensions(resource):
	if validate(resource):
		return PackedStringArray([EXTENSION])
	return PackedStringArray()

func _recognize(resource):
	return validate(resource)

func _save(resource, path, flags):
	resource.call(SAVE_METHOD_NAME, path.get_base_dir())
