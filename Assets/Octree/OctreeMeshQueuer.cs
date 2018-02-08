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

	public float WorldSize;
	public int Resolution;

	List<Node> MeshedNodes = new List<Node>(); 
	Hashtable UnityObjects = new Hashtable();

	public bool DoneFirstTask = false;

	public OctreeMeshQueuer(float worldSize, int resolution, Transform parent, GameObject meshPrefab) {
		WorldSize = worldSize;
		Resolution = resolution;
		Parent = parent;
		MeshPrefab = meshPrefab;
	}

	public void EnqueueMeshUpdate(Root root) {
		OctreeMeshJob jobData = new OctreeMeshJob();
		jobData.Root = root; 

		nextJob = jobData;
		Update();
	}

	public void Update() {
		if(DoneFirstTask) {
			Debug.Log("Finished octree update...");
		}
		if(currentJob == null || DoneFirstTask) {
			currentJob = nextJob;
			nextJob = null;
			DoneFirstTask = false;
			StartTask();
		}
	}

	private void StartTask() {
		if(currentJob == null) return;

		Debug.Log("Starting update job");


		MeshOctree(currentJob.Root.DeepCopy());
	}

	private void MeshOctree(Root root) {
		Debug.Log("Mesh Octree called");
		List<Node> newLeafNodes = new List<Node>();
		PopulateLeafNodeList(root.RootNode, newLeafNodes);

		//float totalPolyganizeNodeTime = 0f;
		//float totalAllBeforeTime = 0f;

		//System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

		foreach(Node n in MeshedNodes.Except(newLeafNodes)) {
			UnityEngine.Object.Destroy((GameObject)UnityObjects[n.ID]);
			UnityObjects.Remove(n.ID);
		}

		Debug.Log("Got here 2");

		IEnumerable<Node> toBePolyganized = newLeafNodes.Except(MeshedNodes);

		Debug.Log("Boutta PolyganizeNodesAsync");

		PolyganizeNodesAsync(toBePolyganized);
		MeshedNodes = newLeafNodes;

		//Debug.Log("Async BENCH-MESH: AllBefore time: " + totalAllBeforeTime + " seconds.");
		//Debug.Log("Async BENCH-MESH: PolyganizeNode time: " + totalPolyganizeNodeTime + " seconds.");

	}

	private async void PolyganizeNodesAsync(IEnumerable<Node> toBePolyganized) {
		Debug.Log("PolyganizeNodesAsync called");
		List<Task<MCMesh>> tasks = new List<Task<MCMesh>>();
		foreach(Node n in toBePolyganized) {
			tasks.Add(MeshNode(n));
		}

		var continuation = Task.WhenAll(tasks);
		Debug.Log("Boutta await continuation");

		await continuation;
		if (continuation.Status == TaskStatus.RanToCompletion) {
			foreach(MCMesh m in continuation.Result) {
				RealizeNode(m);
			}
		}

		DoneFirstTask = true;
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
		Debug.Log("Realizing node!");

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
		mf.mesh = um;

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