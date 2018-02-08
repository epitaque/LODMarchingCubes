using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace SE.Octree {
    public class Wrapper {
		Vector3 lastPosition;

		Console Console;
		//Debugger Debugger;
        Transform Parent;
        GameObject MeshPrefab;
		//Root Root;
        List<Node> MeshedNodes; 
		Hashtable UnityObjects;
		OctreeUpdateQueuer OctreeQueuer;
		OctreeMeshQueuer MeshQueuer;

		public Root Root = null;
        public float WorldSize;
		public int MaxDepth;
		public int Resolution;
		public bool MeshEveryFrame = false;
		public bool AsynchronousOctreeUpdate = false;
		public bool AsynchronousMeshUpdate = false;

        public Wrapper(Transform parent, GameObject meshPrefab, float worldSize, int maxDepth, int resolution, Console console, bool meshEveryFrame, bool asynchronousOctreeUpdate, bool asynchronousMeshUpdate) {
            Parent = parent;
            MeshPrefab = meshPrefab;
            MeshedNodes = new List<Node>();
			UnityObjects = new Hashtable();
            WorldSize = worldSize;
			MaxDepth = maxDepth;
			Resolution = resolution;
			Console = console;
			MeshEveryFrame = meshEveryFrame;
			AsynchronousOctreeUpdate = asynchronousOctreeUpdate;
			AsynchronousMeshUpdate = asynchronousMeshUpdate;
			if(!AsynchronousOctreeUpdate) {
				Root = Ops.Create();
			}
			else {
				OctreeQueuer = new OctreeUpdateQueuer(Ops.Create());
			}

			if(AsynchronousMeshUpdate) {
				MeshQueuer = new OctreeMeshQueuer(WorldSize, Resolution, Parent, MeshPrefab);
			}
			lastPosition = new Vector3(-999999, -999999, -999999);
        }

		public Root GetRoot() {
			if(AsynchronousOctreeUpdate) {
				return this.OctreeQueuer.Root;
			}
			else {
				return Root;
			}
		}

        public void Update(Vector3 position, bool Manual) {
			//Debug.Log("updating at position " + position);
			if(position != lastPosition) {
				if(AsynchronousOctreeUpdate) {
					OctreeQueuer.EnqueueUpdate(position / WorldSize, MaxDepth);
				}
				else {
					Octree.Ops.Adapt(GetRoot(), position / WorldSize, MaxDepth, 100);
				}

				if(AsynchronousMeshUpdate && (Manual || MeshEveryFrame)) {
					MeshQueuer.EnqueueMeshUpdate(GetRoot());
				}

				lastPosition = position;
			}
			else {
				if(AsynchronousOctreeUpdate) {
					OctreeQueuer.Update();
				}
				if(AsynchronousMeshUpdate) {
					MeshQueuer.Update();
				}
			}
			System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

			//sw.Start();
			//SE.Octree.Ops.Adapt(Root, position / WorldSize, MaxDepth, 15);
        	//sw.Stop(); Debug.Log("BENCH-UPDATE: SE.Octree.Ops.Adapt time: " + (float)sw.ElapsedMilliseconds/1000f + " seconds.");
			//sw.Reset(); sw.Start();
			//sw.Stop(); Debug.Log("BENCH-UPDATE: Mesh time: " + (float)sw.ElapsedMilliseconds/1000f + " seconds.");
        }

		public void MakeConforming() {
			//Debug.Log("Make conforming");
			Ops.LoopMakeConforming(GetRoot(), 2);
		}

        public void DrawGizmos() {
			//if(!Root.Locked) {
			SE.Octree.Ops.DrawGizmos(GetRoot().RootNode, WorldSize);
			//}
			//Debugger.DrawGizmos();
        }

        public void Mesh() {
			if(AsynchronousMeshUpdate) {
				Debug.Log("Meshing async");
				MeshAsync();
			}
			else {
				MeshSynchronous();
			}
        }

		public void MeshSynchronous() {
			List<Node> newLeafNodes = new List<Node>();
            PopulateLeafNodeList(GetRoot().RootNode, newLeafNodes);

			float totalPolyganizeNodeTime = 0f;
			float totalAllBeforeTime = 0f;

			System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

			foreach(Node n in MeshedNodes.Except(newLeafNodes)) {
				Object.Destroy((GameObject)UnityObjects[n.ID]);
				UnityObjects.Remove(n.ID);
			}
			foreach(Node n in newLeafNodes.Except(MeshedNodes)) {
				MeshNode(n, ref totalPolyganizeNodeTime, ref totalAllBeforeTime, sw);
			}

        	Debug.Log("BENCH-MESH: AllBefore time: " + totalAllBeforeTime + " seconds.");
        	Debug.Log("BENCH-MESH: PolyganizeNode time: " + totalPolyganizeNodeTime + " seconds.");

            MeshedNodes = newLeafNodes;
		}

		public void MeshAsync() {
			MeshQueuer.EnqueueMeshUpdate(GetRoot());
		}

		public void PopulateLeafNodeList(Node node, List<Node> leafNodes) {
			if(node.IsLeaf) {
				leafNodes.Add(node);
			}
			else {
				for(int i = 0; i < node.Children.Length; i++) {
					PopulateLeafNodeList(node.Children[i], leafNodes);
				}
			}
		}

		public void MeshNode(Node node, ref float totalPolyganizeNodeTime, ref float totalAllBeforeTime, System.Diagnostics.Stopwatch sw) {
			sw.Start();
			GameObject clone = Object.Instantiate(MeshPrefab, new Vector3(0, 0, 0), Quaternion.identity);
			Color c = UtilFuncs.SinColor(node.Depth * 3f);
			clone.GetComponent<MeshRenderer>().material.color = new Color(c.r, c.g, c.b, 0.9f);
			clone.transform.localScale = Vector3.one * (WorldSize * node.Size / Resolution);
			clone.name = "Node " + node.ID + ", Depth " + node.Depth;
			

			MeshFilter mf = clone.GetComponent<MeshFilter>();
			sw.Stop();
			totalAllBeforeTime += (float)sw.ElapsedMilliseconds/1000f;
			sw.Reset(); sw.Start();
			MCMesh mcm = SE.Octree.Ops.PolyganizeNode(node, WorldSize, Resolution);
			Mesh m = new Mesh();
			m.SetVertices(mcm.Vertices);
			m.SetNormals(mcm.Normals);
			m.triangles = mcm.Triangles;
			mf.mesh = m;
			sw.Stop();
			totalPolyganizeNodeTime += (float)sw.ElapsedMilliseconds/1000f;
			clone.GetComponent<Transform>().SetParent(Parent);
			clone.GetComponent<Transform>().SetPositionAndRotation(node.Position * WorldSize, Quaternion.identity);

			UnityObjects[node.ID] = clone;
		}
    }
}

