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
}
    
}