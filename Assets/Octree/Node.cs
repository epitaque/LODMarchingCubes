using System.Collections.Generic;
using UnityEngine;

namespace SE.Octree {

public class Node {
    public Node[] Children;
    public Node Parent;
    public Vector3 Position;
    public uint ID;
    public float Size;
    public bool IsLeaf;
    public int Depth;
    public Vector4 Key;
	public byte LODSides;

	public Node DeepCopy() {
		Node other = (Node) this.MemberwiseClone();
		other.Parent = null;
		if(Children != null) {
			other.Children = new Node[8];
			for(int i = 0; i < 8; i++) { 
				other.Children[i] = Children[i].DeepCopy();
				other.Children[i].Parent = this;
			}
		}
		return other;
	}
}
    
}