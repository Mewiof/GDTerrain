#if TOOLS

using System.Runtime.CompilerServices;
using Godot;

namespace GDTerrain {

	/// <summary>Shows a cursor over terrain to preview where the brush will paint</summary>
	public sealed class BrushDecal {

		private readonly DirectMeshInstance _directMeshInstance = new();
		private readonly PlaneMesh _mesh = new();
		private readonly ShaderMaterial _material = new();

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

		public BrushDecal() {
			_material.Shader = ResourceLoader.Load<Shader>("res://addons/GDTerrain/Tools/Brushes/Shaders/decal.gdshader", null, ResourceLoader.CacheMode.Replace);
			_directMeshInstance.SetMaterial(_material);
			_directMeshInstance.SetMesh(_mesh);
		}

		public void SetSize(int value) {
			_mesh.Size = new(value, value);
			int sS = value - 1;
			// do not subdivide too much
			while (sS > 50) {
				sS /= 2;
			}
			_mesh.SubdivideWidth = sS;
			_mesh.SubdivideDepth = sS;
		}

		public void OnTargetTerrainTransformChanged(Transform3D value) {
			Transform3D inv = value.AffineInverse();
			_material.SetShaderParameter("u_terrain_inverse_transform", inv);
			Basis normalBasis = value.basis.Inverse().Transposed();
			_material.SetShaderParameter("u_terrain_normal_basis", normalBasis);
		}

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
