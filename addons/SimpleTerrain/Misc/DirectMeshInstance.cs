using System.Runtime.CompilerServices;
using Godot;

namespace SimpleTerrain {

	/// <summary>
	/// An alternative to MeshInstance3D that does not use scene tree
	/// </summary>
	public class DirectMeshInstance {

		private RID _meshInstance;
		// Keep a reference
		private Mesh _mesh;

		private void ThrowIfNullRID() {
			if (_meshInstance.Equals(default)) {
				throw new System.Exception($"[{nameof(DirectMeshInstance)}] Null RID");
			}
		}

		#region Visible
		private bool _visible;
		public bool Visible {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _visible;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set {
				ThrowIfNullRID();
				RenderingServer.InstanceSetVisible(_meshInstance, value);
				_visible = value;
			}
		}
		#endregion

		public DirectMeshInstance() {
			_meshInstance = RenderingServer.InstanceCreate();
			Visible = true;
		}

		public void TryFreeRID() {
			if (_meshInstance.Equals(default)) {
				return;
			}

			RenderingServer.FreeRid(_meshInstance);
			_meshInstance = default;
		}

		~DirectMeshInstance() {
			TryFreeRID();
		}

		private void EnterWorld(World3D value) {
			ThrowIfNullRID();
			RenderingServer.InstanceSetScenario(_meshInstance, value.Scenario);
		}

		private void ExitWorld() {
			ThrowIfNullRID();
			RenderingServer.InstanceSetScenario(_meshInstance, default);
		}

		public void SetWorld(World3D value) {
			if (value != null) {
				EnterWorld(value);
				return;
			}
			ExitWorld();
		}

		public void SetTransform(Transform3D value) {
			ThrowIfNullRID();
			RenderingServer.InstanceSetTransform(_meshInstance, value);
		}

		public void SetMesh(Mesh value) {
			ThrowIfNullRID();
			RenderingServer.InstanceSetBase(_meshInstance, value != null ? value.GetRid() : default);
			_mesh = value;
		}

		public void SetMaterial(Material value) {
			ThrowIfNullRID();
			RenderingServer.InstanceGeometrySetMaterialOverride(_meshInstance, value != null ? value.GetRid() : default);
		}

		public void SetAABB(AABB value) {
			ThrowIfNullRID();
			RenderingServer.InstanceSetCustomAabb(_meshInstance, value);
		}
	}
}
