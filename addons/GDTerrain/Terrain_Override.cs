using System.Collections.Generic;
using Godot;
using Godot.Collections;

namespace GDTerrain {

	public partial class Terrain : Node3D {

		public override void _Notification(long what) {
			//DebugLog($"{nameof(Terrain)}->{nameof(_Notification)}->{what}");
			switch (what) {
				case NotificationPredelete:
					ClearAllChunks();
					break;
				case NotificationEnterWorld:
					World3D world3D = GetWorld3d();
					ForAllChunks(item => item.SetWorld(world3D));
					break;
				case NotificationExitWorld:
					ForAllChunks(item => item.SetWorld(null));
					break;
				case NotificationTransformChanged:
					OnTransformChanged();
					break;
				case NotificationVisibilityChanged:
					bool visibleInTree = IsVisibleInTree();
					ForAllChunks(item => item.Visible = visibleInTree && item.Active);
					break;
			}
		}

		public override void _EnterTree() {
			Logger.DebugLog($"{nameof(Terrain)}->{nameof(_EnterTree)}");

			SetPhysicsProcess(true);
		}

		public override void _PhysicsProcess(double delta) {
			if (!Engine.IsEditorHint()) {
				UpdateViewerPosition(null);
			}

			if (HasData) {
				if (_data.Resolution != 0) {
					Transform3D internalTransform = InternalTransform;
					// viewer position in heightmap,
					// where 1 unit == 1 pixel
					Vector3 viewerPosHeightmapLocal = internalTransform.AffineInverse() * _viewerPosWorld;
					_lodder.Update(viewerPosHeightmapLocal);
				}
			}

			PhysicsProcess_Chunks();
			PhysicsProcess_Material();
		}

		private static readonly string[] _missingDataWarningArr = new string[1] {
			"Missing data.\nUse the 'Data Directory' property to assign/create data."
		};

		public override string[] _GetConfigurationWarnings() {
			if (!HasData) {
				return _missingDataWarningArr;
			}
			return null;
		}

		private static readonly List<string> _aPIShaderParams = new() {
			"u_terrain_heightmap",
			"u_terrain_normalmap",
			"u_terrain_colormap",
			"u_terrain_splatmap",
			"u_terrain_splatmap_1",
			"u_terrain_splatmap_2",
			"u_terrain_splatmap_3",
			"u_terrain_splat_index_map",
			"u_terrain_splat_weight_map",
			"u_terrain_globalmap",

			"u_terrain_inverse_transform",
			"u_terrain_normal_basis",

			"u_ground_albedo_bump_0",
			"u_ground_albedo_bump_1",
			"u_ground_albedo_bump_2",
			"u_ground_albedo_bump_3",

			"u_ground_normal_roughness_0",
			"u_ground_normal_roughness_1",
			"u_ground_normal_roughness_2",
			"u_ground_normal_roughness_3",

			"u_ground_albedo_bump_array",
			"u_ground_normal_roughness_array"
		};

		public override Array<Dictionary> _GetPropertyList() {
			if (_material.Shader == null) {
				return null;
			}

			Array<Dictionary> result = new();
			Array<Dictionary> shaderParams = RenderingServer.GetShaderParameterList(_material.Shader.GetRid());
			for (int i = 0; i < shaderParams.Count; i++) {
				if (_aPIShaderParams.Contains(shaderParams[i]["name"].AsString())) {
					continue;
				}
				Dictionary dict = shaderParams[i];
				dict["name"] = "shader_params/" + shaderParams[i]["name"];
				result.Add(dict);
			}
			return result;
		}

		public override Variant _Get(StringName property) {
			string propertyStr = property.ToString();
			if (propertyStr.StartsWith("shader_params/")) {
				return _material.GetShaderParameter(propertyStr.Split("shader_params/")[1]);
			}

			return default;
		}

		public override bool _Set(StringName property, Variant value) {
			string propertyStr = property.ToString();
			if (propertyStr.StartsWith("shader_params/")) {
				_material.SetShaderParameter(propertyStr.Split("shader_params/")[1], value);
				return true;
			}

			return false;
		}
	}
}
