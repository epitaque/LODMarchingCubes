// Multithreaded Chunk Generator
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Threading;
using UnityEngine;

namespace Chunks {
public static class ChunkJobQueuer {
	public static void Initialize() {
		Tasks = null;
		WorkState = 0;
		Error = "";
		Results = null;
		NumDoneTasks = 0;
	}

	public static bool QueueTasks(ChunkJob[] tasks) {
		UnityEngine.Debug.Log("QueueTasks called");

		if(WorkState != 0) {
			return false;
		}
		else {
			NumDoneTasks = 0;
			Results = new ConcurrentBag<ChunkJobResult>();
			Error = "";
			Tasks = tasks;
			// 1. Create a new thread
			Thread thread = new Thread(new ThreadStart(WorkThreadFunction));
			thread.Start();

			return true;
		}

	}
	
	public static ChunkJobResult[] CheckStatus() {
		UnityEngine.Debug.Log("Num done tasks: " + NumDoneTasks + " / " + Tasks.Length);

		if(WorkState == 3) {
			UnityEngine.Debug.Log("Error processing chunkJobs: " + Error);

			WorkState = 0;
			return null;
		}
		else if(WorkState == 2) {
			WorkState = 0;
			return Results.ToArray();
		}
		else {
			WorkState = 0;
			return null;
		}
	}

	// 0 = Doing nothing
	// 1 = Work in progress
	// 2 = Done work, need processing
	// 3 = Done with errors

	private static int WorkState;
	private static ChunkJob[] Tasks;
	private static ConcurrentBag<ChunkJobResult> Results;
	private static string Error;
	private static int NumDoneTasks;

	private static void WorkThreadFunction() {
		try
		{
			Parallel.ForEach(Tasks, (ChunkJob job) => {
				Results.Add(ChunkGenerator.CreateChunk(job));
				NumDoneTasks++;
			});
			WorkState = 2;
		}
		catch (System.Exception ex)
		{
			Error = ex.Message;
			WorkState = 3;
		}
	}	
}
}