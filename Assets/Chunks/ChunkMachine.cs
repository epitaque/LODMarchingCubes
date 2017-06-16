using System.Collections.Generic;
using System.Collections;
using UnityEngine;

namespace Chunks {
	public class ChunkMachine {
		int LODs;
		int Threads;
		float MinCellSize;
		int Resolution;

		Hashtable LoadedChunks;

		int NumUnrenderedChunks;

		Vector3 LoadedChunksCenter;
		Vector3 UnloadedChunksCenter;

		ChunkManageInput UsedInput;
		ChunkJobQueuer JobQueuer;

		List<ChunkJob> MostRecentChunkJobList;

		GameObject MeshPrefab;

		public ChunkMachine(int LODs, int Threads, float MinCellSize, int Resolution, GameObject MeshPrefab) {
			this.LODs = LODs;
			this.Threads = Threads;
			this.MinCellSize = MinCellSize;
			this.Resolution = Resolution;
			this.MeshPrefab = MeshPrefab;

			LoadedChunks = new Hashtable();
			JobQueuer = new ChunkJobQueuer(Threads);

			UsedInput = new ChunkManageInput();
			LoadedChunksCenter = new Vector3(float.MinValue, float.MinValue, float.MinValue);
			UnloadedChunksCenter = new Vector3(float.MinValue, float.MinValue, float.MinValue);
			UsedInput.LoadedChunks = LoadedChunks;
			UsedInput.LODs = LODs;

			UsedInput.MinSizeOfCell = MinCellSize;
			UsedInput.Resolution = Resolution;
		}

		public void Update(Vector3 PlayerLocation) {
			UsedInput.PlayerLocation = PlayerLocation;
			UsedInput.LastUnrenderedChunkCenter = UnloadedChunksCenter;
			UsedInput.LastRenderedChunkCenter = LoadedChunksCenter;

			ChunkManageResult mResult = ChunkManager.ManageChunks(UsedInput);

			if(mResult.NewState == ChunkWorkState.CancelLastJob || mResult.NewState == ChunkWorkState.DoNewJobAndCancelLastJob) {
				JobQueuer.CancelAllJobs();
				UnloadedChunksCenter = LoadedChunksCenter;
				NumUnrenderedChunks = 0;
			}
			if(mResult.NewState == ChunkWorkState.DoNewJobAndCancelLastJob) {
				UnityEngine.Debug.Log("State: DoNewJobAndCancelLastJob");

				UnloadedChunksCenter = mResult.NewCenter;
				NumUnrenderedChunks = mResult.Jobs.Count;
				MostRecentChunkJobList = mResult.AllChunkJobs;
				for(int i = 0; i < mResult.Jobs.Count; i++) {
					JobQueuer.QueueChunk(mResult.Jobs[i]);
				}
			}

			UnityEngine.Debug.Log("LC Count: " + JobQueuer.LoadedChunks.Count + ", NumUnrenderedChunks: " + NumUnrenderedChunks);

			if(JobQueuer.LoadedChunks.Count == NumUnrenderedChunks && NumUnrenderedChunks != 0) {
				NumUnrenderedChunks = 0;

				Hashtable NewChunks = new Hashtable();

				foreach(DictionaryEntry c1 in LoadedChunks) {
					bool shouldDestroy = true;
					foreach(ChunkJob c2 in MostRecentChunkJobList) {
						if((string)c1.Key == c2.Key) {
							shouldDestroy = false;
							NewChunks.Add(c1.Key, c1.Value);
							break;
						}
					}

					if(shouldDestroy) {
						UnityEngine.Object.Destroy(((Chunk)(c1.Value)).Object);
					}
				}

				foreach(ChunkJobResult JobRes in JobQueuer.LoadedChunks) {
					Chunk c = ChunkManager.RealizeChunk(JobRes, MeshPrefab);
					NewChunks.Add(c.Key, c);
				}

				LoadedChunks = NewChunks;
				LoadedChunksCenter = UnloadedChunksCenter;
			}

			JobQueuer.Update();
		}

	}
}