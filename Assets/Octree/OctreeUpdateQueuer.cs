using System;
using System.Threading.Tasks;
using System.Threading;
using UnityEngine;

namespace SE.Octree {

public class OctreeUpdateJob {
	public Root Root;
	public Vector3 Position;
	public int MaxDepth;
	public int Radius;
}

public class OctreeUpdateQueuer {
	public OctreeUpdateJob currentJob;
	public OctreeUpdateJob nextJob;

	public Task currentTask = null;

	public Root Root;

	public OctreeUpdateQueuer(Root root) {
		Root = root;
	}

	public void EnqueueUpdate(Vector3 position, int maxDepth) {
		OctreeUpdateJob jobData = new OctreeUpdateJob();
		jobData.Root = Root; 
		jobData.Position = position;
		jobData.MaxDepth = maxDepth;

		nextJob = jobData;
		Update();
	}

	public void Update() {
		if(currentTask != null && currentTask.Status == TaskStatus.RanToCompletion) {
			Debug.Log("Current task completed execution ");
		}

		if(currentTask == null || currentTask.Status == TaskStatus.RanToCompletion) {
			currentTask = null;
			currentJob = nextJob;
			nextJob = null;
			StartTask();
		}
	}

	private void StartTask() {
		if(currentJob == null || currentTask != null) return;

		Debug.Log("Starting Octree Update job");

		currentJob.Root.Locked = true;
		Action<object> job = (object data) => {
			OctreeUpdateJob cData = (OctreeUpdateJob)data;
			Root realRoot = Root;
			Root copy = realRoot.DeepCopy();

			Root = copy;
			SE.Octree.Ops.Adapt(realRoot, cData.Position, cData.MaxDepth, 100);
			Root = realRoot;

			Debug.Log("Adapt finished");

			//Root = copy;
		};

		currentTask = Task.Factory.StartNew(job, currentJob);
	}
}

}