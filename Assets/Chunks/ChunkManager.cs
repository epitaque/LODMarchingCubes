using System.Collections.Generic;
using System.Collections;
using UnityEngine;


namespace Chunks {
	public static class ChunkManager {

		public static ChunkManageResult result;

		public static void ManageChunks(ChunkManageInput Input) {
			ChunkManageResult Result = new ChunkManageResult();
			Result.NewState = ChunkWorkState.DoNothing;

			Vector3 PlayerGridLocationNormalized = GetNormalizedGridLocation(Input.PlayerLocation, Input);
			Vector3 PlayerGridLocationRounded = GetRoundedGridLocation(PlayerGridLocationNormalized);

			UnityEngine.Debug.Log("PlayerGridLocationRounded: " + PlayerGridLocationRounded);
			UnityEngine.Debug.Log("Input.CurrentChunksCenter: " + Input.CurrentChunksCenter);


			if(PlayerGridLocationRounded != Input.CurrentChunksCenter) {
				Result.NewState = ChunkWorkState.DoNewJob;

				List<ChunkJob> NeededChunks = new List<ChunkJob>();
				Hashtable OldChunks = Input.LoadedChunks;
				List<ChunkJob> NewChunks = GetChunksAroundPoint(PlayerGridLocationRounded, Input);
				Result.AllChunkJobs = NewChunks;
				Result.NewCenter = PlayerGridLocationRounded;

				foreach(ChunkJob c in NewChunks) {
					if(!OldChunks.Contains(c.Key)) {
						NeededChunks.Add(c);
					}
				}

				Result.Jobs = NeededChunks;

			}

			result = Result;
		}
		public static ChunkManageResult CheckManageChunks() {
			if(result == null) {
				return null;
			}
			else {
				ChunkManageResult res = result;
				result = null;
				return res;
			}
		} 

		private static List<ChunkJob> GetChunksAroundPoint(Vector3 Point, ChunkManageInput Input) { // inspired by https://github.com/felixpalmer/lod-terrain/blob/master/js/app/terrain.js
			UnityEngine.Debug.Log("GetChunksAroundPoint called");
			
			List<ChunkJob> ChunkJobs = new List<ChunkJob>();
			float ChunkSize = Input.MinSizeOfCell * Input.Resolution;

			// First pretend like you're creating a chunks structure with center 0,0,0
			// Pretend Min chunk size is one
			// Then multiply all the chunk mins by the real chunk size
			// Then offset all the chunks by Point
			
			// Create center 2x2x2 cube first
			// No LODsides
			for(int x = -1; x < 1; x++) {
				for(int y = -1; y < 1; y++) {
					for(int z = -1; z < 1; z++) {
						ChunkJob c = MakeChunkJob( (new Vector3(x, y, z) + Point) * ChunkSize, Input.MinSizeOfCell, 0, Input);
						ChunkJobs.Add(c);
					}
				}
			}

			// Create hollow cubes of chunks
			// Iterate through full cube, but don't generate new chunk if not edge chunk
			for(int i = 0; i < Input.LODs; i++) {
				int size = (int)Mathf.Pow(2, i); // size of chunks (1, 2, 4, 8...)
				int dSize = size * 2;
				for(int x = -dSize; x < dSize; x += size) {
					for(int y = -dSize; y < dSize; y += size) {
						for(int z = -dSize; z < dSize; z += size) {
							// Check if edge chunk by first calculating LOD sides
							byte LOD = 0;
							if(x == -dSize) 	  LOD |= 1;  // -x
							if(x == dSize - size) LOD |= 2;  // +x
							if(y == -dSize) 	  LOD |= 4;  // -y
							if(y == dSize - size) LOD |= 8;  // +y
							if(z == -dSize) 	  LOD |= 16; // -z
							if(z == dSize - size) LOD |= 32; // +z
							if(LOD != 0) {
								ChunkJob c = MakeChunkJob((new Vector3(x, y, z) + Point) * ChunkSize, size * Input.MinSizeOfCell, LOD, Input);
								ChunkJobs.Add(c);
							}
						}
					}
				}
			}

			return ChunkJobs;
		}
		private static ChunkJob MakeChunkJob(Vector3 Min, float Size, byte LOD, ChunkManageInput Input) {
			ChunkJob Job = new ChunkJob();
			Job.Key = Size.ToString() + Min.ToString() + LOD.ToString();
			Job.LOD = LOD;
			Job.Min = Min;
			Job.Resolution = Input.Resolution;
			Job.CellSize = Size;
			return Job;
		}
		private static Vector3 GetRoundedGridLocation(Vector3 NormalizedGridLocation) {
			 Vector3 RoundedGridLocation = NormalizedGridLocation + new Vector3(0.5f, 0.5f, 0.5f);
			 RoundedGridLocation.x = (int)RoundedGridLocation.x;
			 RoundedGridLocation.y = (int)RoundedGridLocation.y;
			 RoundedGridLocation.z = (int)RoundedGridLocation.z;
			 return RoundedGridLocation;
		}
		private static Vector3 GetNormalizedGridLocation(Vector3 Point, ChunkManageInput Input) {
			return Input.PlayerLocation * (1 / (Input.Resolution * Input.MinSizeOfCell));
		}
	}
}