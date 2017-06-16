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
				if(!BusyThreads[i] && !BackgroundWorkers[i].IsBusy) {
					//UnityEngine.Debug.Log("Queueing Chunk Job.");

					BusyThreads[i] = true;
					ChunkJob chunkJob = (ChunkJob)UnloadedChunks.Dequeue();
					chunkJob.ThreadID = i;

					try {
						BackgroundWorkers[i].RunWorkerAsync(chunkJob);
					}
					catch(System.InvalidOperationException exc) {
						//UnloadedChunks.Enqueue(chunkJob);
					}
				}
			}
		}
	}

	void InitializeBackgroundWorkers() {
		BackgroundWorkers = new System.ComponentModel.BackgroundWorker[Threads];
		for(int i = 0; i < Threads; i++) {
			BackgroundWorkers[i] = new System.ComponentModel.BackgroundWorker();
			BackgroundWorkers[i].WorkerSupportsCancellation = true;
			BackgroundWorkers[i].DoWork += 
				new System.ComponentModel.DoWorkEventHandler(BackgroundWorkers_DoWork_ThreadedGenerateChunk);  
			BackgroundWorkers[i].RunWorkerCompleted +=  
				new System.ComponentModel.RunWorkerCompletedEventHandler(BackgroundWorkers_RunWorkerCompleted_ThreadedGenerateChunk);  
		}
	}

	public void QueueChunk(ChunkJob Job) {
		UnityEngine.Debug.Log("Job Queued");
		UnloadedChunks.Enqueue(Job);
	}

	void BackgroundWorkers_DoWork_ThreadedGenerateChunk (System.Object sender,
		System.ComponentModel.DoWorkEventArgs e) {

		System.ComponentModel.BackgroundWorker worker = (sender as System.ComponentModel.BackgroundWorker);

		ChunkJob Job = (ChunkJob)e.Argument;
		ChunkJobResult res = ChunkGenerator.CreateChunk(Job);

		if(worker.CancellationPending == true) {
			e.Cancel = true;
			return;
		}

		e.Result = res;
	}

	public void CancelAllJobs() {
		UnityEngine.Debug.Log("All jobs canceled.");

		for(int i = 0; i < Threads; i++) {
			if(BusyThreads[i] && BackgroundWorkers[i].IsBusy) {
				UnityEngine.Debug.Log("Worker " + i + " canceled.");

				BackgroundWorkers[i].CancelAsync();
				LoadedChunks.Clear();
				UnloadedChunks.Clear();
				BusyThreads[i] = false;
			}
		}
	}

	private void BackgroundWorkers_RunWorkerCompleted_ThreadedGenerateChunk (System.Object sender,  
		System.ComponentModel.RunWorkerCompletedEventArgs e) { 
		UnityEngine.Debug.Log("Worker completed.");
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