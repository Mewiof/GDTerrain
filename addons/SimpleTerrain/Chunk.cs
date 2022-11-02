using System.Runtime.CompilerServices;
using Godot;

namespace SimpleTerrain {

	public class Chunk {

		private readonly int _cellOriginX;
		private readonly int _cellOriginY;

		private RID _meshInstance;
		private Transform3D _localTransform;

		public int CellOriginX => _cellOriginX;
		public int CellOriginY => _cellOriginY;

		private bool _visible;
		public bool Visible {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _visible;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set {
				if (_meshInstance.Equals(default)) {
					throw new System.Exception("'_meshInstance.Equals(default)'");
				}
				RenderingServer.InstanceSetVisible(_meshInstance, value);
				_visible = value;
			}
		}

		public bool Active {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set;
		}
		public bool PendingUpdate {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set;
		}

		public Chunk(Node3D parent, int cellX, int cellY, Material material) {
			_cellOriginX = cellX;
			_cellOriginY = cellY;

			_meshInstance = RenderingServer.InstanceCreate();
			_localTransform = new(Basis.Identity, new(_cellOriginX, 0f, _cellOriginY));

			if (material != null) {
				RenderingServer.InstanceGeometrySetMaterialOverride(_meshInstance, material.GetRid());
			}

			World3D world = parent.GetWorld3d();
			if (world != null) {
				RenderingServer.InstanceSetScenario(_meshInstance, world.Scenario);
			}

			Visible = true;

			Active = true;
			PendingUpdate = false;
		}

		public void TryFreeRID() {
			if (_meshInstance.Equals(default)) {
				return;
			}

			RenderingServer.FreeRid(_meshInstance);
			_meshInstance = default;
		}

		~Chunk() {
			TryFreeRID();
		}

		public void EnterWorld(World3D world) {
			if (_meshInstance.Equals(default)) {
				throw new System.Exception("'_meshInstance.Equals(default)'");
			}

			RenderingServer.InstanceSetScenario(_meshInstance, world.Scenario);
		}

		public void ExitWorld() {
			if (_meshInstance.Equals(default)) {
				throw new System.Exception("'_meshInstance.Equals(default)'");
			}

			RenderingServer.InstanceSetScenario(_meshInstance, default);
		}

		public void OnParentTransformChanged(Transform3D parentTransform) {
			if (_meshInstance.Equals(default)) {
				throw new System.Exception("'_meshInstance.Equals(default)'");
			}

			Transform3D worldTransform = parentTransform * _localTransform;
			RenderingServer.InstanceSetTransform(_meshInstance, worldTransform);
		}

		public void SetMesh(Mesh value) {
			if (_meshInstance.Equals(default)) {
				throw new System.Exception("'_meshInstance.Equals(default)'");
			}

			RenderingServer.InstanceSetBase(_meshInstance, value != null ? value.GetRid() : default);
		}

		public void SetMaterial(Material value) {
			if (_meshInstance.Equals(default)) {
				throw new System.Exception("'_meshInstance.Equals(default)'");
			}

			RenderingServer.InstanceGeometrySetMaterialOverride(
				_meshInstance, value != null ? value.GetRid() : default);
		}

		public void SetAABB(AABB value) {
			if (_meshInstance.Equals(default)) {
				throw new System.Exception("'_meshInstance.Equals(default)'");
			}

			RenderingServer.InstanceSetCustomAabb(_meshInstance, value);
		}

		public void SetRenderLayerMask(uint value) {
			if (_meshInstance.Equals(default)) {
				throw new System.Exception("'_meshInstance.Equals(default)'");
			}

			RenderingServer.InstanceSetLayerMask(_meshInstance, value);
		}

		public void SetCastShadowsSetting(RenderingServer.ShadowCastingSetting value) {
			if (_meshInstance.Equals(default)) {
				throw new System.Exception("'_meshInstance.Equals(default)'");
			}

			RenderingServer.InstanceGeometrySetCastShadowsSetting(_meshInstance, value);
		}
	}
}
