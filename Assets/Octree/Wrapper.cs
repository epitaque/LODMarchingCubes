using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace SE.Octree {
    public class Wrapper {
		Console Console;
		Debugger Debugger;
        Transform Parent;
        GameObject MeshPrefab;
		Root Root;

        List<Node> MeshedNodes; 
		Hashtable UnityObjects;
        public float WorldSize;

        public Wrapper(Transform parent, GameObject meshPrefab, float worldSize, Console console) {
            Parent = parent;
            MeshPrefab = meshPrefab;
            MeshedNodes = new List<Node>();
			UnityObjects = new Hashtable();
            WorldSize = worldSize;
			Console = console;
			Root = Ops.Create();
			
			Debugger = new Debugger(Root, WorldSize);
			Console.Debugger = Debugger;

        }

        public void Update(Vector3 position) {
			SE.Octree.Ops.Adapt(Root, position / 64f, 8, 15);
			Mesh();
        }

		public void MakeConforming() {
			//Debug.Log("Make conforming");
			Ops.LoopMakeConforming(Root, 2);
		}

        public void DrawGizmos() {
			SE.Octree.Ops.DrawGizmos(Root.RootNode);
			Debugger.DrawGizmos();
        }

        public void Mesh() {
			List<Node> newLeafNodes = new List<Node>();
            PopulateLeafNodeList(Root.RootNode, newLeafNodes);

			foreach(Node n in MeshedNodes.Except(newLeafNodes)) {
				Object.Destroy((GameObject)UnityObjects[n.ID]);
				UnityObjects.Remove(n.ID);
			}
			foreach(Node n in newLeafNodes.Except(MeshedNodes)) {
				MeshNode(n);
			}

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

		public void MeshNode(Node node) {
			GameObject clone = Object.Instantiate(MeshPrefab, new Vector3(0, 0, 0), Quaternion.identity);
			Color c = UtilFuncs.SinColor(node.Depth * 3f);
			clone.GetComponent<MeshRenderer>().material.color = new Color(c.r, c.g, c.b, 0.9f);
			clone.transform.localScale = Vector3.one * WorldSize;
			clone.name = "Node " + node.ID + ", Depth " + node.Depth;

			MeshFilter mf = clone.GetComponent<MeshFilter>();
			mf.mesh = SE.Octree.Ops.PolyganizeNode(node, WorldSize);
			clone.GetComponent<Transform>().SetParent(Parent);
			clone.GetComponent<Transform>().SetPositionAndRotation(node.Position * WorldSize, Quaternion.identity);

			UnityObjects[node.ID] = clone;
		}
    }
}

