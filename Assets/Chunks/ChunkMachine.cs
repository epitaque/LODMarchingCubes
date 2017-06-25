using System.Collections.Generic;
using System.Collections;

using System.Threading;
using System.Threading.Tasks;

using UnityEngine;

namespace Chunks {
	public class ChunkMachine {
		int LODs;
		float MinCellSize;
		int Resolution;

		Hashtable LoadedChunks;

		int NumChunkJobs;

		Vector3 LoadedChunksCenter;
		Vector3 UnloadedChunksCenter;

		ChunkManageResult LastResult;
		ChunkManageInput UsedInput;

		List<ChunkJob> MostRecentAllChunkJobList;

		GameObject MeshPrefab;
		GameObject MeshParent;

		bool ActiveJob;

		List<Chunk> DestroyList;
		Queue<ChunkJobResult> RealizeList;
		List<Chunk> SavedChunks;
		Hashtable RealizedChunks;
		bool PreparingChunksMode;

		bool CurrentlyManagingChunks;

		int MaxNumToPrepare = 1;

		float LastUpdateTime = -1f;

		public ChunkMachine(int LODs, float MinCellSize, int MaxNumChunksToPrepare, int Resolution, ComputeShader NoiseComputeShader, GameObject MeshPrefab, GameObject MeshParent) {
			this.LODs = LODs;
			this.MinCellSize = MinCellSize;
			this.Resolution = Resolution;
			this.MeshPrefab = MeshPrefab;
			this.MeshParent = MeshParent;
			this.MaxNumToPrepare = MaxNumChunksToPrepare;

			this.ActiveJob = false;
			PreparingChunksMode = false;
			CurrentlyManagingChunks = false;

			ChunkJobQueuer.Initialize();
			LoadedChunks = new Hashtable();


			UsedInput = new ChunkManageInput();
			LoadedChunksCenter = new Vector3(float.MinValue, float.MinValue, float.MinValue);
			UnloadedChunksCenter = new Vector3(float.MinValue, float.MinValue, float.MinValue);
			UsedInput.LoadedChunks = LoadedChunks;
			UsedInput.LODs = LODs;

			UsedInput.MinSizeOfCell = MinCellSize;
			UsedInput.Resolution = Resolution;
		}

		public void Update(Vector3 PlayerLocation) {
			if(PreparingChunksMode) {
				PrepareChunks(MaxNumToPrepare);
				return;
			}
			else if(CurrentlyManagingChunks) {
				ChunkManageResult ManageResult = ChunkManager.CheckManageChunks();

				if(ManageResult != null) {
					UnityEngine.Debug.Log("Made it here");

					CurrentlyManagingChunks = false;
					if(ManageResult.NewState == ChunkWorkState.DoNewJob) {
						this.ActiveJob = true;
						

						LastResult = ManageResult;
						UnloadedChunksCenter = ManageResult.NewCenter;

						MostRecentAllChunkJobList = ManageResult.AllChunkJobs;

						ChunkJobQueuer.QueueTasks(ManageResult.Jobs.ToArray());
					}
				}
			}
			else if(UnityEngine.Time.time - LastUpdateTime > 3.0f) {
				LastUpdateTime = UnityEngine.Time.time;

				if(this.ActiveJob) {
					CheckForAndProcessLoadedChunks();
					return;
				}

				UsedInput.LoadedChunks = LoadedChunks;
				UsedInput.PlayerLocation = PlayerLocation;
				UsedInput.CurrentChunksCenter = LoadedChunksCenter;

				Task.Run(() => ChunkManager.ManageChunks(UsedInput));
				CurrentlyManagingChunks = true;

				return;
			}
		}

		private void PrepareChunks(int MaxNumToPrepare) {
			for(int i = 0; i < MaxNumToPrepare; i++) {
				if(RealizeList.Count == 0) {
					FinishPreparing();
					return;
				}

				Chunk c = ChunkManager.RealizeChunk(RealizeList.Dequeue(), MeshPrefab, MeshParent);
				RealizedChunks.Add(c.Key, c);
			}
		}

		private void FinishPreparing() {
			PreparingChunksMode = false;
			foreach(Chunk c in DestroyList) {
				UnityEngine.Object.Destroy(c.Object);
			}
			foreach(DictionaryEntry ent in RealizedChunks) {
				Chunk c = (Chunk)ent.Value;
				//c.Object.SetActive(true);
				//c.Object.GetComponent<MeshCollider>().enabled = true;
				c.Object.GetComponent<MeshRenderer>().enabled = true;
			}
			foreach(Chunk c in SavedChunks) {
				RealizedChunks.Add(c.Key, c);
			}
			LoadedChunks = RealizedChunks;
		}

		private void ResetPrepareCollections() {
			DestroyList = new List<Chunk>();
			RealizeList = new Queue<ChunkJobResult>();
			RealizedChunks = new Hashtable();
			SavedChunks = new List<Chunk>();
		}

		private void CheckForAndProcessLoadedChunks() {
			ChunkJobResult[] Results = ChunkJobQueuer.CheckStatus();

			if(Results != null) {
				UnityEngine.Debug.Log("All " + Results.Length + " jobs finished.");

				PreparingChunksMode = true;
				ActiveJob = false;
				LoadedChunksCenter = UnloadedChunksCenter;
				ResetPrepareCollections();

				Hashtable NewChunks = new Hashtable();

				// For each currently loaded chunk...
				foreach(DictionaryEntry c1 in LoadedChunks) {
					bool shouldDestroy = true;

					// Check if it's in the most recent chunk job list
					foreach(ChunkJob c2 in MostRecentAllChunkJobList) {

						// If it is, don't destroy it and add it to the new chunks list
						if((string)c1.Key == c2.Key) {
							shouldDestroy = false;
							SavedChunks.Add((Chunk)c1.Value);
							break;
						}
					}

					// Otherwise, destroy it
					if(shouldDestroy) {
						//UnityEngine.Debug.Log("Destroying chunk");
						//UnityEngine.Debug.Log("((Chunk)(c1.Value)).Object.name" + ((Chunk)(c1.Value)).Object.name);
						//UnityEngine.Object.Destroy(((Chunk)(c1.Value)).Object);
						DestroyList.Add((Chunk)c1.Value);
					}
				}

				long totalProcessingTime = 0;

				// For each chunk job...
				foreach(ChunkJobResult JobRes in Results) {
					if(JobRes == null) {
						UnityEngine.Debug.Log("JobResult is null.");
					}
					totalProcessingTime += JobRes.ProcessingTime;

					// Load its chunk
					RealizeList.Enqueue(JobRes);
					//Chunk c = ChunkManager.RealizeChunk(JobRes, MeshPrefab, MeshParent);
					//NewChunks.Add(c.Key, c);
				}

				UnityEngine.Debug.Log("Average processing time: " + (totalProcessingTime / Results.Length) + "ms.");

				//LoadedChunks = NewChunks;
			}
		}

	}
}