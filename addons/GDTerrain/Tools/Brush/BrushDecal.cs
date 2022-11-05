#if TOOLS

using System.Runtime.CompilerServices;
using Godot;

namespace GDTerrain {

	[Tool]
	public partial class BrushDecal : Object {

		private readonly DirectMeshInstance _directMeshInstance;
		private readonly PlaneMesh _mesh;
		private readonly ShaderMaterial _material;

		private Terrain _targetTerrain;
		public Terrain TargetTerrain {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _targetTerrain;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set {
				// old
				if (_targetTerrain != null) {
					_targetTerrain.transformChanged -= OnTargetTerrainTransformChanged;
					_directMeshInstance.SetWorld(null);
				}

				_targetTerrain = value;

				// new
				if (_targetTerrain != null) {
					_targetTerrain.transformChanged += OnTargetTerrainTransformChanged;
					OnTargetTerrainTransformChanged(_targetTerrain.InternalTransform);
					_directMeshInstance.SetWorld(_targetTerrain.GetWorld3d());
				}

				UpdateVisibility();
			}
		}

		public BrushDecal() : base() {
			_directMeshInstance = new();
			_mesh = new();
			_directMeshInstance.SetMesh(_mesh);
			_material = new() {
				Shader = ResourceLoader.Load<Shader>("res://addons/GDTerrain/Tools/Brush/Shaders/decal.gdshader")
			};
			_directMeshInstance.SetMaterial(_material);
		}

		public int Size {
			get => (int)System.MathF.Ceiling(System.MathF.Max(_mesh.Size.x, _mesh.Size.y));//?
			set {
				_mesh.Size = new(value, value);
				int sS = value - 1;
				// do not subdivide too much
				while (sS > 25) {
					sS /= 2;
				}
				_mesh.SubdivideWidth = sS;
				_mesh.SubdivideDepth = sS;
			}
		}

		public void OnTargetTerrainTransformChanged(Transform3D value) {
			Transform3D inv = value.AffineInverse();
			_material.SetShaderParameter("u_terrain_inverse_transform", inv);
			Basis normalBasis = value.basis.Inverse().Transposed();
			_material.SetShaderParameter("u_terrain_normal_basis", normalBasis);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void SetPosition(Vector3i value) {
			TerrainData terrainData = _targetTerrain.Data;
			if (terrainData != null) {
				Vector2 r = _mesh.Size / 2f;
				AABB aABB = terrainData.GetRegionAABB(
					(int)(value.x - r.x),
					(int)(value.z - r.y),
					(int)(2f * r.x),
					(int)(2f * r.y));
				aABB.Position = new(-r.x, aABB.Position.y, -r.y);
				_mesh.CustomAabb = aABB;
			}

			Transform3D transform = new(Basis.Identity, value);
			Transform3D tTerrGT = _targetTerrain.InternalTransform;
			transform *= tTerrGT;
			_directMeshInstance.SetTransform(transform);
		}

		private ImageTexture GetHeightmapTexture() {
			if (_targetTerrain == null) {
				return null;
			}
			TerrainData terrainData = _targetTerrain.Data;
			if (terrainData == null) {
				return null;
			}
			return terrainData.GetMapTexture(TerrainData.MAP_HEIGHT);
		}

		public void UpdateVisibility() {
			ImageTexture heightmapTexture = GetHeightmapTexture();
			if (heightmapTexture == null) {
				//Plugin.DebugLog($"{nameof(BrushDecal)}->{nameof(UpdateVisibility)}->'heightmapTexture == null'");
				_material.SetShaderParameter("u_terrain_heightmap", default);
				_directMeshInstance.Visible = false;
				return;
			}
			_material.SetShaderParameter("u_terrain_heightmap", heightmapTexture);
			_directMeshInstance.Visible = true;
		}
	}
}
#endif
