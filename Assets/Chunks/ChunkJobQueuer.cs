// Multithreaded Chunk Generator
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace Chunks {
public class ChunkJobQueuer {
	public Queue LoadedChunks; // ChunkJobResult
	public Queue UnloadedChunks; // ChunkJob
	
	private int Threads = 4;
	private System.ComponentModel.BackgroundWorker[] BackgroundWorkers;  

	private bool[] BusyThreads;

	public ChunkJobQueuer (int NumberOfThreads) {
		LoadedChunks = new Queue();
		UnloadedChunks = new Queue();

		Threads = NumberOfThreads;
		BusyThreads = new bool[Threads];
		for(int i = 0; i < Threads; i++) {
			BusyThreads[i] = false;
		}

		InitializeBackgroundWorkers();
	}

	public void Update() {
		if(UnloadedChunks.Count > 0) {
			for(int i = 0; i < Threads; i++) {
				if(!BusyThreads[i]) {
					BusyThreads[i] = true;
					ChunkJob chunkJob = (ChunkJob)UnloadedChunks.Dequeue();
					chunkJob.ThreadID = i;
					BackgroundWorkers[i].RunWorkerAsync(chunkJob);
					Update();
				}
			}
		}
	}

	void InitializeBackgroundWorkers() {
		BackgroundWorkers = new System.ComponentModel.BackgroundWorker[Threads];
		for(int i = 0; i < Threads; i++) {
			BackgroundWorkers[i] = new System.ComponentModel.BackgroundWorker();
			BackgroundWorkers[i].DoWork += 
				new System.ComponentModel.DoWorkEventHandler(BackgroundWorkers_DoWork_ThreadedGenerateChunk);  
			BackgroundWorkers[i].RunWorkerCompleted +=  
				new System.ComponentModel.RunWorkerCompletedEventHandler(BackgroundWorkers_RunWorkerCompleted_ThreadedGenerateChunk);  
		}
	}

	public void QueueChunk(ChunkJob Job) {
		UnloadedChunks.Enqueue(Job);
	}

	void BackgroundWorkers_DoWork_ThreadedGenerateChunk (System.Object sender,
		System.ComponentModel.DoWorkEventArgs e) {

		ChunkJob Job = (ChunkJob)e.Argument;
		ChunkJobResult res = ChunkGenerator.CreateChunk(Job);

		if((sender as System.ComponentModel.BackgroundWorker).CancellationPending == true) {
			return;
		}

		e.Result = res;
	}

	public void CancelAllJobs() {
		for(int i = 0; i < Threads; i++) {
			if(BusyThreads[i] && BackgroundWorkers[i].IsBusy) {
				BackgroundWorkers[i].CancelAsync();
				LoadedChunks.Clear();
				UnloadedChunks.Clear();
				BusyThreads[i] = false;
			}
		}
	}

	private void BackgroundWorkers_RunWorkerCompleted_ThreadedGenerateChunk (System.Object sender,  
		System.ComponentModel.RunWorkerCompletedEventArgs e) {  
        if(e.Error != null)
        {
            UnityEngine.Debug.LogError("There was an error! " + e.Error.ToString());
        }
		else {
			ChunkJobResult result = (ChunkJobResult)e.Result;  
			LoadedChunks.Enqueue(result);

			BusyThreads[result.OriginalJob.ThreadID] = false;

			if(UnloadedChunks.Count > 0) {
				Update();
			}
		}
	}  
}
}