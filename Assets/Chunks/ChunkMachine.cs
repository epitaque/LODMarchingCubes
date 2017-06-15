using System.Collections.Generic;
using System.Collections;
using UnityEngine;

namespace Chunks {
	public class ChunkMachine {
		int LODs;
		int Threads;
		int MinCellSize;
		int Resolution;

		Hashtable LoadedChunks;

		int NumUnrenderedChunks;

		Vector3 LoadedChunksCenter;
		Vector3 UnloadedChunksCenter;

		ChunkManageInput UsedInput;
		ChunkJobQueuer JobQueuer;

		List<ChunkJob> MostRecentChunkJobList;

		GameObject MeshPrefab;

		public ChunkMachine(int LODs, int Threads, int MinCellSize, int Resolution, GameObject MeshPrefab) {
			this.LODs = LODs;
			this.Threads = Threads;
			this.MinCellSize = MinCellSize;
			this.Resolution = Resolution;
			this.MeshPrefab = MeshPrefab;

			LoadedChunks = new Hashtable();
			JobQueuer = new ChunkJobQueuer(Threads);

			UsedInput = new ChunkManageInput();
			UsedInput.LastUnrenderedChunkCenter = new Vector3(float.MinValue, float.MinValue, float.MinValue);
			UsedInput.LastRenderedChunkCenter = new Vector3(float.MinValue, float.MinValue, float.MinValue);
			UsedInput.LoadedChunks = LoadedChunks;

			UsedInput.MinSizeOfCell = MinCellSize;
			UsedInput.Resolution = Resolution;
		}

		public void Update(Vector3 PlayerLocation) {
			UsedInput.PlayerLocation = PlayerLocation;
			ChunkManageResult mResult = ChunkManager.ManageChunks(UsedInput);

			if(mResult.NewState == ChunkWorkState.CancelLastJob || mResult.NewState == ChunkWorkState.DoNewJobAndCancelLastJob) {
				JobQueuer.CancelAllJobs();
				UnloadedChunksCenter = LoadedChunksCenter;
				NumUnrenderedChunks = 0;
			}
			if(mResult.NewState == ChunkWorkState.DoNewJobAndCancelLastJob) {
				UnloadedChunksCenter = mResult.NewCenter;
				NumUnrenderedChunks = mResult.Jobs.Count;
				MostRecentChunkJobList = mResult.AllChunkJobs;
				for(int i = 0; i < mResult.Jobs.Count; i++) {
					JobQueuer.QueueChunk(mResult.Jobs[i]);
				}
			}

			if(JobQueuer.LoadedChunks.Count == NumUnrenderedChunks && NumUnrenderedChunks != 0) {
				Hashtable NewChunks = new Hashtable();

				foreach(Chunk c in LoadedChunks) {
					bool shouldDestroy = true;
					foreach(ChunkJob c2 in MostRecentChunkJobList) {
						if(c.Key == c2.Key) {
							shouldDestroy = false;
							NewChunks.Add(c.Key, c);
							break;
						}
					}

					if(shouldDestroy) {
						UnityEngine.Object.Destroy(c.Object);
					}
				}

				foreach(ChunkJobResult JobRes in JobQueuer.LoadedChunks) {
					Chunk c = ChunkManager.RealizeChunk(JobRes, MeshPrefab);
					NewChunks.Add(c.Key, c);
				}

				LoadedChunks = NewChunks;
			}
		}

	}
}