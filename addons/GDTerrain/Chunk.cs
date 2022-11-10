using System.Runtime.CompilerServices;
using Godot;

namespace GDTerrain {

	public sealed class Chunk : DirectMeshInstance {

		private Transform3D _localTransform;

		public int CellOriginX {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get;
		}

		public int CellOriginY {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get;
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

		public Chunk(Node3D parent, int cellX, int cellY, Material material) : base() {
			_localTransform = new(Basis.Identity, new(cellX, 0f, cellY));

			CellOriginX = cellX;
			CellOriginY = cellY;

			SetWorld(parent.GetWorld3d());
			SetMaterial(material);

			Active = true;
			PendingUpdate = false;
		}

		~Chunk() {
			TryFreeRID();
		}

		public void OnParentTransformChanged(Transform3D value) {
			Transform3D worldTransform = value * _localTransform;
			SetTransform(worldTransform);
		}

		public void SetRenderLayerMask(uint value) {
			ThrowIfNullRID();
			RenderingServer.InstanceSetLayerMask(_meshInstance, value);
		}

		public void SetShadowCastingSetting(RenderingServer.ShadowCastingSetting value) {
			ThrowIfNullRID();
			RenderingServer.InstanceGeometrySetCastShadowsSetting(_meshInstance, value);
		}
	}
}
