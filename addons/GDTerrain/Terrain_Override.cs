using Godot;

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
					ForAllChunks(item => item.EnterWorld(world3D));
					break;
				case NotificationExitWorld:
					ForAllChunks(item => item.ExitWorld());
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
			Plugin.DebugLog($"{nameof(Terrain)}->{nameof(_EnterTree)}");

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

			_updatedChunks = 0;

			// for neighbours (seams)
			int prevCount = _pendingChunkUpdates.Count;
			PendingChunkUpdate u;
			for (int i = 0; i < prevCount; i++) {
				u = _pendingChunkUpdates[i];

				// in case chunk got split
				for (int d = 0; d < 4; d++) {
					int nCPosX = u.cPosX + _sDirs[d, 0];
					int nCPosY = u.cPosY + _sDirs[d, 1];

					Chunk nChunk = GetChunkAt(nCPosX, nCPosY, u.lOD);
					if (nChunk != null && nChunk.Active) {
						// this will append elements to the array we're iterating on,
						// but we iterate only on prev count, so it should be fine
						AddChunkUpdate(nChunk, nCPosX, nCPosY, u.lOD);
					}
				}

				// in case chunk got joined
				if (u.lOD > 0) {
					int cPosUpperX = u.cPosX * 2;
					int cPosUpperY = u.cPosY * 2;
					int nLOD = u.lOD - 1;

					for (int rD = 0; rD < 8; rD++) {
						int nCPosUpperX = cPosUpperX + _sRDirs[rD, 0];
						int nCPosUpperY = cPosUpperY + _sRDirs[rD, 1];

						Chunk nChunk = GetChunkAt(nCPosUpperX, nCPosUpperY, nLOD);
						if (nChunk != null && nChunk.Active) {
							// this will append elements to the array we're iterating on,
							// but we iterate only on prev count, so it should be fine
							AddChunkUpdate(nChunk, nCPosUpperX, nCPosUpperY, nLOD);
						}
					}
				}
			}

			// update chunks
			bool lVisible = IsVisibleInTree();
			for (int i = 0; i < _pendingChunkUpdates.Count; i++) {
				u = _pendingChunkUpdates[i];
				Chunk chunk = GetChunkAt(u.cPosX, u.cPosY, u.lOD);
				/*if (chunk == null) {
					throw new Exception("'chunk == null'");
				}*/
				UpdateChunk(chunk, u.lOD, lVisible);
				_updatedChunks++;
			}

			_pendingChunkUpdates.Clear();
		}

		private static readonly string[] _missingDataWarningArr = new string[1] {
			"Missing data.\nSelect the 'Data Directory' property to assign/create data."
		};

		public override string[] _GetConfigurationWarnings() {
			if (!HasData) {
				return _missingDataWarningArr;
			}
			return null;
		}
	}
}
