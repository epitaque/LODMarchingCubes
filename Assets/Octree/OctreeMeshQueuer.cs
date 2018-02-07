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
	
	public GameObject MeshPrefab;
	public Transform Parent;

	public int WorldSize;
	public int Resolution;

	List<Node> MeshedNodes; 
	Hashtable UnityObjects;

	public Task currentTask = null;

	public Root Root;

	public OctreeMeshQueuer(Root root, int worldSize, int resolution, Transform parent, GameObject meshPrefab) {
		Root = root;
		WorldSize = worldSize;
		Resolution = resolution;
		Parent = parent;
		MeshPrefab = meshPrefab;
	}

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

	private void UpdateJobList(Root root, List<Node> MeshedNodes, Hashtable UnityObjects) {
		List<Node> newLeafNodes = new List<Node>();
		PopulateLeafNodeList(root.RootNode, newLeafNodes);

		float totalPolyganizeNodeTime = 0f;
		float totalAllBeforeTime = 0f;

		System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

		foreach(Node n in MeshedNodes.Except(newLeafNodes)) {
			UnityEngine.Object.Destroy((GameObject)UnityObjects[n.ID]);
			UnityObjects.Remove(n.ID);
		}
		IEnumerable<Node> toBePolyganized = newLeafNodes.Except(MeshedNodes);
		PolyganizeNodesAsync(toBePolyganized);

		Debug.Log("BENCH-MESH: AllBefore time: " + totalAllBeforeTime + " seconds.");
		Debug.Log("BENCH-MESH: PolyganizeNode time: " + totalPolyganizeNodeTime + " seconds.");

		MeshedNodes = newLeafNodes;
	}

	private async void PolyganizeNodesAsync(IEnumerable<Node> toBePolyganized) {
		List<Task<MCMesh>> tasks = new List<Task<MCMesh>>();
		foreach(Node n in toBePolyganized) {
			tasks.Add(MeshNode(n));
		}

		var continuation = Task.WhenAll(tasks);

		await continuation;
		if (continuation.Status == TaskStatus.RanToCompletion) {
			foreach(MCMesh m in continuation.Result) {
				RealizeNode(m);
			}
		}
		//Task<MCMesh> polyganizeNodesTasks = Task.WhenAll(toBePolyganized.Select(node => MeshNode(node)));
	}

	public async Task<MCMesh> MeshNode(Node node) {
		MCMesh m = await Task.Run(() => SE.Octree.Ops.PolyganizeNode(node, WorldSize, Resolution));
		m.nodeDepth = node.Depth;
		m.nodeID = node.ID;
		m.nodeSize = node.Size;
		m.nodePosition = node.Position;

		return m;
	}

	private void RealizeNode(MCMesh m) {
		GameObject clone = UnityEngine.Object.Instantiate(MeshPrefab, new Vector3(0, 0, 0), Quaternion.identity);
		Color c = UtilFuncs.SinColor(m.nodeDepth * 3f);
		clone.GetComponent<MeshRenderer>().material.color = new Color(c.r, c.g, c.b, 0.9f);
		clone.transform.localScale = Vector3.one * (WorldSize * m.nodeSize / Resolution);
		clone.name = "Node " + m.nodeID + ", Depth " + m.nodeDepth;
		

		MeshFilter mf = clone.GetComponent<MeshFilter>();
		Mesh um = new UnityEngine.Mesh();
		um.SetVertices(m.Vertices);
		um.SetNormals(m.Normals);
		um.triangles = m.Triangles;

		clone.GetComponent<Transform>().SetParent(Parent);
		clone.GetComponent<Transform>().SetPositionAndRotation(m.nodePosition * WorldSize, Quaternion.identity);

		UnityObjects[m.nodeID] = clone;
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