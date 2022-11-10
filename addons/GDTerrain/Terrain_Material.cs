using System.Collections.Generic;
using Godot;

namespace GDTerrain {

	public partial class Terrain : Node3D {

		public enum ShaderType {
			Classic,
			Custom
		}

		public readonly Dictionary<ShaderType, string> _builtInShaders = new() {
			{
				ShaderType.Classic,
				Plugin.BASE_PATH + "Shaders/classic.gdshader"
			}
		};

		private ShaderType _shaderType = ShaderType.Classic;

		private ShaderMaterial _material = new();
		/// <summary>Material</summary>
		private bool _dirty;

		#region Render Layer Mask
		private uint _renderLayerMask = 1;
		[Export(PropertyHint.Layers3dRender)]
		public uint RenderLayerMask {
			get => _renderLayerMask;
			set {
				_renderLayerMask = value;
				ForAllChunks(item => item.SetRenderLayerMask(_renderLayerMask));
			}
		}
		#endregion

		#region Shadow Cast Setting
		private RenderingServer.ShadowCastingSetting _shadowCastSetting = RenderingServer.ShadowCastingSetting.On;
		[Export]
		public RenderingServer.ShadowCastingSetting ShadowCastSetting {
			get => _shadowCastSetting;
			set {
				_shadowCastSetting = value;
				ForAllChunks(item => item.SetShadowCastingSetting(_shadowCastSetting));
			}
		}
		#endregion

		private void Init_Material() {
			_material.SetShaderParameter("u_ground_uv_scale", 20);
			_material.SetShaderParameter("u_ground_uv_scale_vec4", new Color(20f, 20f, 20f, 20f));
			_material.SetShaderParameter("u_depth_blending", true);

			_material.Shader = ResourceLoader.Load<Shader>(_builtInShaders[_shaderType]);

			TransformChanged += _ => _dirty = true;
		}

		private void UpdateMaterialParams() {
			if (_material == null) {
				throw new System.Exception("'_material == null'");
			}

			// TODO: cache
			Dictionary<string, ImageTexture> terrainTextures = new();

			if (HasData) {
				for (int tI = 0; tI < TerrainData.MAP_TYPE_COUNT; tI++) {
					int mapCount = _data.GetMapCount(tI);
					for (int i = 0; i < mapCount; i++) {
						string paramName = TerrainData.GetMapShaderParamName(tI, i);
						terrainTextures[paramName] = _data.GetMapTexture(tI, i);
					}
				}
			}

			if (IsInsideTree()) {
				Transform3D gT = InternalTransform;
				Transform3D t = gT.AffineInverse();
				_material.SetShaderParameter("u_terrain_inverse_transform", t);

				Basis normalBasis = gT.basis.Inverse().Transposed();
				_material.SetShaderParameter("u_terrain_normal_basis", normalBasis);
			}

			foreach (KeyValuePair<string, ImageTexture> keyValuePair in terrainTextures) {
				_material.SetShaderParameter(keyValuePair.Key, keyValuePair.Value);
			}
		}

		private void PhysicsProcess_Material() {
			if (_dirty) {
				UpdateMaterialParams();
				_dirty = false;
			}
		}
	}
}
