using System;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace SE.Octree {

public class OctreeMeshJob {
	public Root Root;
}

public class OctreeMeshQueuer {
	public OctreeMeshJob currentJob;
	public OctreeMeshJob nextJob;
	
	public int WorldSize;
	public int Resolution;

	List<Node> MeshedNodes; 
	Hashtable UnityObjects;

	public Task currentTask = null;

	public Root Root;

	public void ProcessOctreeUpdate(Root root, int worldSize, int resolution) {
		WorldSize = worldSize;
		Resolution = resolution;

		OctreeMeshJob jobData = new OctreeMeshJob();
		jobData.Root = Root; 

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

		Debug.Log("Starting update job");

		currentJob.Root.Locked = true;

		Task.Factory.StartNew((object data) => {
			// Find which new nodes to polyganize/deletes
		}, "asdf");

		Action<object> job = (object data) => {
			Debug.Log("This is a print statement inside a job");
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

	private static void UpdateJobList(Root root, List<Node> MeshedNodes, Hashtable UnityObjects) {
		List<Node> newLeafNodes = new List<Node>();
		PopulateLeafNodeList(root.RootNode, newLeafNodes);

		float totalPolyganizeNodeTime = 0f;
		float totalAllBeforeTime = 0f;

		System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

		foreach(Node n in MeshedNodes.Except(newLeafNodes)) {
			UnityEngine.Object.Destroy((GameObject)UnityObjects[n.ID]);
			UnityObjects.Remove(n.ID);
		}
		foreach(Node n in newLeafNodes.Except(MeshedNodes)) {
			MeshNode(n, ref totalPolyganizeNodeTime, ref totalAllBeforeTime, sw);
		}

		Debug.Log("BENCH-MESH: AllBefore time: " + totalAllBeforeTime + " seconds.");
		Debug.Log("BENCH-MESH: PolyganizeNode time: " + totalPolyganizeNodeTime + " seconds.");

		MeshedNodes = newLeafNodes;
	}

	private static async void PolyganizeNodesAsync(List<Node> toBePolyganized) {
		await Task.WhenAll(toBePolyganized.Select(node => DoSomething(1, i, blogClient)));
	}

	public MCMesh MeshNode(Node node) {
		return SE.Octree.Ops.PolyganizeNode(node, WorldSize, Resolution);
	}

	private static RealizeNode(MCMesh m) {
		GameObject clone = Object.Instantiate(MeshPrefab, new Vector3(0, 0, 0), Quaternion.identity);
		Color c = UtilFuncs.SinColor(node.Depth * 3f);
		clone.GetComponent<MeshRenderer>().material.color = new Color(c.r, c.g, c.b, 0.9f);
		clone.transform.localScale = Vector3.one * (WorldSize * node.Size / Resolution);
		clone.name = "Node " + node.ID + ", Depth " + node.Depth;
		

		MeshFilter mf = clone.GetComponent<MeshFilter>();
		sw.Stop();
		totalAllBeforeTime += (float)sw.ElapsedMilliseconds/1000f;
		sw.Reset(); sw.Start();
		mf.mesh = SE.Octree.Ops.PolyganizeNode(GetRoot(), node, WorldSize, Resolution);
		sw.Stop();
		totalPolyganizeNodeTime += (float)sw.ElapsedMilliseconds/1000f;
		clone.GetComponent<Transform>().SetParent(Parent);
		clone.GetComponent<Transform>().SetPositionAndRotation(node.Position * WorldSize, Quaternion.identity);

		UnityObjects[node.ID] = clone;
	}

	private static void PopulateLeafNodeList(Node node, List<Node> leafNodes) {
		if(node.IsLeaf) {
			leafNodes.Add(node);
		}
		else {
			for(int i = 0; i < node.Children.Length; i++) {
				PopulateLeafNodeList(node.Children[i], leafNodes);
			}
		}
	}
}

}