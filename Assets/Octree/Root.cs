using UnityEngine;
using System.Collections.Generic;

namespace SE.Octree {

public class Root {
	public bool Locked;
    public Node RootNode;
    public Dictionary<uint, Node> IDNodes; // index: ID | value: Node
    public Dictionary<Vector4, Node> Nodes; // index: Vector4 (xyz=position, w=Depth) | value: Node

	// copies everything but the dictionaries
	public Root DeepCopy() {
		Root other = (Root)this.MemberwiseClone();
		other.RootNode = RootNode.DeepCopy();
		other.IDNodes = IDNodes;
		other.Nodes = Nodes;
		return other;
	}
}

} 