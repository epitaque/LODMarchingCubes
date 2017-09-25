using System.Collections.Generic;
using UnityEngine;

using SE.Octree;

namespace SE {
    public class Debugger {
        public List<Vector3> GizmoPoints;
        public List<Vector4> Cubes;
        Root Root;
        float WorldSize;

        public Debugger(Root root, float worldSize) {
            GizmoPoints = new List<Vector3>();
            Cubes = new List<Vector4>();
            WorldSize = worldSize;
            Root = root;
        }

        public void TestNeighborFetching(string NodeID, string NeighborNumber, string SnapLevel) {
            uint nodeID;
            int neighborNumber;
            int snapLevel;

            Debug.Assert(uint.TryParse(NodeID, out nodeID));
            Debug.Assert(int.TryParse(NeighborNumber, out neighborNumber));
            Debug.Assert(int.TryParse(SnapLevel, out snapLevel));
            Debug.Assert(Root != null);
            Debug.Assert(Root.IDNodes.ContainsKey(nodeID));

            Vector4 code = SE.Octree.Ops.GetCollapsedCode(Root.IDNodes[nodeID], Ops.Directions[neighborNumber]);

            Debug.Log("Code: " + code);

            Vector3 position = new Vector3(code.x, code.y, code.z) * WorldSize;
            GizmoPoints.Clear();
            GizmoPoints.Add(position);
        }

        public void RecursiveGetNeighbor(string NodeID, string NeighborNumber) {
            uint nodeID;
            int neighborNumber;

            Debug.Assert(uint.TryParse(NodeID, out nodeID));
            Debug.Assert(int.TryParse(NeighborNumber, out neighborNumber));
            Debug.Assert(Root != null);
            Debug.Assert(Root.IDNodes.ContainsKey(nodeID));

            Node n = Ops.RecursiveGetNeighbor(Root, Root.IDNodes[nodeID], Ops.Directions[neighborNumber]);
            Cubes.Clear();
            if(n == null) {
                Debug.Log("No neighbor #" + neighborNumber + " for node #" + nodeID);
            }
            else {
                Cubes.Add(new Vector4(n.Position.x + n.Size/2f, n.Position.y + n.Size/2f, n.Position.z + n.Size/2f, n.Size * 0.9f) * WorldSize);
            }
        }

        public void DrawGizmos() {
            Gizmos.color = Color.magenta;
            foreach(Vector3 point in GizmoPoints) {
                Gizmos.DrawSphere(point, 2f);
            }
            foreach(Vector4 cube in Cubes) {
                Gizmos.DrawCube(new Vector3(cube.x, cube.y, cube.z), Vector3.one * cube.w);
            }
        }
    }
}