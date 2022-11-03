# Currently, "ResourceFormatSaver" / "ResourceFormatLoader" work through a** in C#, so we use GDScript

@tool
class_name TerrainResSaver
extends ResourceFormatSaver

func validate(resource):
	return resource.has_method("IsTerrainData")

func _get_recognized_extensions(resource):
	if resource != null and validate(resource):
		return PackedStringArray([resource.call("GetExtension")])
	return PackedStringArray()

func _recognize(resource):
	return validate(resource)

func _save(resource, path, flags):
	resource.call("Save", path.get_base_dir())
