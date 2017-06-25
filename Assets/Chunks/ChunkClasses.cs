using System.Collections.Generic;
using System.Collections;
using UnityEngine;

namespace Chunks {
	public class ChunkManageInput {
		public Vector3 PlayerLocation;
		public int LODs;
		public Hashtable LoadedChunks;
		public Vector3 CurrentChunksCenter;
		public float MinSizeOfCell;
		public int Resolution;
	}
	public class ChunkManageResult {
		public ChunkWorkState NewState;
		public List<ChunkJob> Jobs;
		public List<ChunkJob> AllChunkJobs;
		public Vector3 NewCenter;
	}
	public class ChunkJob {
		public Vector3 Min;
		public byte LOD;
		public float CellSize;
		public int Resolution;
		public string Key;
		public int ThreadID;
		public float JobStartTime;
	}
	public class ChunkJobResult {
		public ChunkJob OriginalJob;
		public ExtractionResult Result;
		public long ProcessingTime;
		public string DebugPrint;
		public string Error;
	}
	public class Chunk {
		public Vector3 Min;
		public float CellSize;
		public byte LOD;
		public GameObject Object;
		public UnityEngine.Mesh Mesh;
		public int Resolution;
		public string Key;
	}
	public enum ChunkWorkState {
		DoNothing,
		DoNewJob
	}
}
