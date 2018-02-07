using System.Collections.Generic;
using UnityEngine;

namespace SE {
    public class MCMesh {
        public List<Vector3> Vertices;
        public int[] Triangles;
        public List<Vector3> Normals;

        public uint nodeID;
        public int nodeDepth;
        public float nodeSize;
        public Vector3 nodePosition;
    }
}