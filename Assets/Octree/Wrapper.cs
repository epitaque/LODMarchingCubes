using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace SE.Octree {
    public class Wrapper {
		Console Console;
		//Debugger Debugger;
        Transform Parent;
        GameObject MeshPrefab;
		Root Root;

        List<Node> MeshedNodes; 
		Hashtable UnityObjects;
        public float WorldSize;
		public int MaxDepth;

        public Wrapper(Transform parent, GameObject meshPrefab, float worldSize, int maxDepth, Console console) {
            Parent = parent;
            MeshPrefab = meshPrefab;
            MeshedNodes = new List<Node>();
			UnityObjects = new Hashtable();
            WorldSize = worldSize;
			MaxDepth = maxDepth;
			Console = console;
			Root = Ops.Create();
			
			//Debugger = new Debugger(Root, WorldSize);
			//Console.Debugger = Debugger;

        }

        public void Update(Vector3 position) {
			System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
			sw.Start();
			SE.Octree.Ops.Adapt(Root, position / WorldSize, MaxDepth, 15);
        	sw.Stop(); Debug.Log("BENCH-UPDATE: SE.Octree.Ops.Adapt time: " + (float)sw.ElapsedMilliseconds/1000f + " seconds.");
			sw.Reset(); sw.Start();
			Mesh();
			sw.Stop(); Debug.Log("BENCH-UPDATE: Mesh time: " + (float)sw.ElapsedMilliseconds/1000f + " seconds.");
        }

		public void MakeConforming() {
			//Debug.Log("Make conforming");
			Ops.LoopMakeConforming(Root, 2);
		}

        public void DrawGizmos() {
			SE.Octree.Ops.DrawGizmos(Root.RootNode);
			//Debugger.DrawGizmos();
        }

        public void Mesh() {
			List<Node> newLeafNodes = new List<Node>();
            PopulateLeafNodeList(Root.RootNode, newLeafNodes);

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
			clone.transform.localScale = Vector3.one * WorldSize;
			clone.name = "Node " + node.ID + ", Depth " + node.Depth;
			

			MeshFilter mf = clone.GetComponent<MeshFilter>();
			sw.Stop();
			totalAllBeforeTime += (float)sw.ElapsedMilliseconds/1000f;
			sw.Reset(); sw.Start();
			mf.mesh = SE.Octree.Ops.PolyganizeNode(Root, node, WorldSize);
			sw.Stop();
			totalPolyganizeNodeTime += (float)sw.ElapsedMilliseconds/1000f;
			clone.GetComponent<Transform>().SetParent(Parent);
			clone.GetComponent<Transform>().SetPositionAndRotation(node.Position * WorldSize, Quaternion.identity);

			UnityObjects[node.ID] = clone;
		}
    }
}

