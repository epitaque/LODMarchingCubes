using System.Collections.Generic;
using UnityEngine;

namespace SE.DC {

public class OctreeDrawInfo 
{
	public int index;
	public int corners;
	public Vector3 position;
	public Vector3 averageNormal;
	public QEF.QEFSolver qef;

	public OctreeDrawInfo()
	{
		index = -1;
		corners = 0;
	}

};

public class OctreeNode
{
	public DCC.OctreeNodeType type;
	public Vector3 min;
	public int size;
	public OctreeNode[] children;
	public OctreeDrawInfo drawInfo;

	public static int numMinNodes = 0;

	public UtilFuncs.Sampler sample;
	public float isovalue;

	public OctreeNode()
	{
		type = DCC.OctreeNodeType.Node_Internal;
		min = Vector3.zero;
		size = 0;
		drawInfo = null;
	
		children = new OctreeNode[8];
		for (int i = 0; i < 8; i++)
		{
			children[i] = null;
		}
	}

	public OctreeNode(DCC.OctreeNodeType _type)
	{
		type = _type;
		min = Vector3.zero;
		size = 0;
		drawInfo = null;

		for (int i = 0; i < 8; i++)
		{
			children[i] = null;
		}
	}

	public static readonly Vector3[] CHILD_MIN_OFFSETS = {
		new Vector3(0, 0, 0), new Vector3(0, 0, 1), new Vector3(0, 1, 0), new Vector3(0, 1, 1),
		new Vector3(1, 0, 0), new Vector3(1, 0, 1), new Vector3(1, 1, 0), new Vector3(1, 1, 1)
	};

	public static OctreeNode ConstructOctreeNodes(OctreeNode node)
	{
		if(node == null) return null;

		if(node.size == 1) {
			return ConstructLeaf(node);
		}

		int childSize = node.size / 2;
		bool hasChildren = false;

		for(int i = 0; i < 8; i++) {
			OctreeNode child = new OctreeNode();
			child.size = childSize;
			child.min = node.min + (CHILD_MIN_OFFSETS[i] * childSize);
			child.type = DCC.OctreeNodeType.Node_Internal;
			child.isovalue = node.isovalue;
			child.sample = node.sample;

			node.children[i] = ConstructOctreeNodes(child);
			hasChildren |= node.children[i] != null;
		}

		if(!hasChildren) {
			return null;
		}

		return node;
	}

	public static void DrawGizmos(OctreeNode node) {
		if(node == null) return;
		if(node.type == DCC.OctreeNodeType.Node_Internal) {
			for(int i = 0; i < 8; i++) {
				DrawGizmos(node.children[i]);
			}
		}
		Gizmos.color = colors[(int)Mathf.Log(node.size, 2)];
		Gizmos.DrawWireCube(node.min + (Vector3.one * (node.size/2f)), Vector3.one * node.size);
	}
	private static Color[] colors = {
		new Color(1, 0, 0, 1f),
		new Color(0, 1, 0, 1f),
		new Color(0, 0, 1, 1f),
		new Color(1, 1, 0, 1f),
		new Color(0, 1, 1, 1f),
		new Color(1, 0, 1, 1f),
		new Color(1, 1, 1, 1f)
	};

	public static OctreeNode ConstructLeaf(OctreeNode leaf) {
		int corners = 0;
		for(int i = 0; i < 8; i++) {
			Vector3 cornerPos = leaf.min + CHILD_MIN_OFFSETS[i];
			float density = leaf.sample(cornerPos.x, cornerPos.y, cornerPos.z);
			int material = density < leaf.isovalue ? DCC.Material_Solid : DCC.Material_Air;
			corners |= (material << i);
		}

		if(corners == 0 || corners == 255) return null;
		Vector3[] positions = new Vector3[DCC.MAX_CROSSINGS];
		Vector3[] normals = new Vector3[DCC.MAX_CROSSINGS];
		int edgeCount = 0;

		for(int i = 0; i < 12 && edgeCount < DCC.MAX_CROSSINGS; i++) {
			int c1 = DCC.edgevmap[i][0];
			int c2 = DCC.edgevmap[i][1];

			int m1 = (corners >> c1) & 1;
			int m2 = (corners >> c2) & 1;

			if((m1 == DCC.Material_Air && m2 == DCC.Material_Air) ||
   			(m1 == DCC.Material_Solid && m2 == DCC.Material_Solid)) {
				   continue;
			}

			Vector3 p1 = leaf.min + CHILD_MIN_OFFSETS[c1];
			Vector3 p2 = leaf.min + CHILD_MIN_OFFSETS[c2];
  			Vector3 p = ApproximateZeroCrossingPosition(p1, p2, leaf.sample);

			positions[edgeCount] = p;
			normals[edgeCount] = CalculateSurfaceNormal(p, leaf.sample);

			edgeCount++;
		}

		OctreeDrawInfo drawInfo = new SE.DC.OctreeDrawInfo();
		drawInfo.qef = new QEF.QEFSolver();
		for(int i = 0; i < edgeCount; i++) {
			drawInfo.qef.Add(positions[i], normals[i]);
		}

		drawInfo.position = drawInfo.qef.Solve(1e-6f, 4, 1e-6f);

		Vector3 min = leaf.min;
		Vector3 max = leaf.min + Vector3.one * leaf.size;
		if (drawInfo.position.x < min.x || drawInfo.position.x > max.x ||
			drawInfo.position.y < min.y || drawInfo.position.y > max.y ||
			drawInfo.position.z < min.z || drawInfo.position.z > max.z)
		{
			drawInfo.position = drawInfo.qef.MassPoint;
		}

		for (int i = 0; i < edgeCount; i++)
		{
			drawInfo.averageNormal += normals[i];
		}
		drawInfo.averageNormal = drawInfo.averageNormal.normalized;

		drawInfo.corners = corners;

		leaf.type = DCC.OctreeNodeType.Node_Leaf;
		leaf.drawInfo = drawInfo;
		OctreeNode.numMinNodes++;

		return leaf;
	}
	public static Vector3 ApproximateZeroCrossingPosition(Vector3 p0, Vector3 p1, UtilFuncs.Sampler sample)
	{
		// approximate the zero crossing by finding the min value along the edge
		float minValue = 100000;
		float t = 0;
		float currentT = 0;
		const int steps = 8;
		const float increment = 1f / (float)steps;
		while (currentT <= 1f)
		{
			Vector3 p = p0 + ((p1 - p0) * currentT);
			float density = Mathf.Abs(sample(p.x, p.y, p.z));
			if (density < minValue)
			{
				minValue = density;
				t = currentT;
			}

			currentT += increment;
		}

		return p0 + ((p1 - p0) * t);
	}
	public static Vector3 CalculateSurfaceNormal(Vector3 p, UtilFuncs.Sampler sample) {
		 const float H = 0.001f;
		 float dx = sample(p.x + H, p.y, p.z) - sample(p.x - H, p.y, p.z);
		 float dy = sample(p.x, p.y + H, p.z) - sample(p.x, p.y - H, p.z);
		 float dz = sample(p.x, p.y, p.z + H) - sample(p.x, p.y, p.z - H);

		return new Vector3(dx, dy, dz).normalized;
	}
    public static void GenerateVertexIndices(OctreeNode node, List<Vector3> vertices, List<Vector3> normals) {
        if(node == null) return;
		if(node.type != DCC.OctreeNodeType.Node_Leaf) {
			for(int i = 0; i < node.children.Length; i++) {
				GenerateVertexIndices(node.children[i], vertices, normals);
			}
		}
		else {
			OctreeDrawInfo d = node.drawInfo;
			d.index = vertices.Count;
			vertices.Add(d.position);
			normals.Add(d.averageNormal);
		}
    }
    public static void ContourProcessEdge(OctreeNode[] nodes, int dir, List<int> indices) {
        int minSize = 1000000;  // arbitrary big number
        int minIndex = 0;
        int[] indices_ = { -1, -1, -1, -1 };
        bool flip = false;
        bool[] signChange = { false, false, false, false };

        for (int i = 0; i < 4; i++) {
            int edge = DCC.processEdgeMask[dir][i];
            int c1 = DCC.edgevmap[edge][0];
            int c2 = DCC.edgevmap[edge][1];

            int m1 = (nodes[i].drawInfo.corners >> c1) & 1;
            int m2 = (nodes[i].drawInfo.corners >> c2) & 1;

            if (nodes[i].size < minSize) {
                minSize = nodes[i].size;
                minIndex = i;
                flip = m1 != DCC.Material_Air; 
            }

            indices_[i] = nodes[i].drawInfo.index;

            signChange[i] = 
            (m1 == DCC.Material_Air && m2 != DCC.Material_Air) ||
            (m1 != DCC.Material_Air && m2 == DCC.Material_Air);
        }

        if (signChange[minIndex]) {
            if (!flip) {
                indices.Add(indices_[0]);
                indices.Add(indices_[1]);
                indices.Add(indices_[3]);

                indices.Add(indices_[0]);
                indices.Add(indices_[3]);
                indices.Add(indices_[2]);
            }
            else {
                indices.Add(indices_[0]);
                indices.Add(indices_[3]);
                indices.Add(indices_[1]);

                indices.Add(indices_[0]);
                indices.Add(indices_[2]);
                indices.Add(indices_[3]);
            }
        }
    }
	public static void ContourEdgeProc(OctreeNode[] nodes, int dir, List<int> indices) {
		for(int i = 0; i < 4; i++) if(nodes[i] == null) return;

		if (nodes[0].type != DCC.OctreeNodeType.Node_Internal &&
			nodes[1].type != DCC.OctreeNodeType.Node_Internal &&
			nodes[2].type != DCC.OctreeNodeType.Node_Internal &&
			nodes[3].type != DCC.OctreeNodeType.Node_Internal) {
			ContourProcessEdge(nodes, dir, indices);
		}
		else {
			for (int i = 0; i < 2; i++) {
				OctreeNode[] edgeNodes = new OctreeNode[4];
				int[] c = {
					DCC.edgeProcEdgeMask[dir][i][0],
					DCC.edgeProcEdgeMask[dir][i][1],
					DCC.edgeProcEdgeMask[dir][i][2],
					DCC.edgeProcEdgeMask[dir][i][3],
				};

				for (int j = 0; j < 4; j++) {
					if (nodes[j].type == DCC.OctreeNodeType.Node_Leaf || nodes[j].type == DCC.OctreeNodeType.Node_Psuedo) {
						edgeNodes[j] = nodes[j];
					}
					else {
						edgeNodes[j] = nodes[j].children[c[j]];
					}
				}

				ContourEdgeProc(edgeNodes, DCC.edgeProcEdgeMask[dir][i][4], indices);
			}
		}
	}
	public static void ContourFaceProc(OctreeNode[] nodes, int dir, List<int> indices) {
		if (nodes[0] == null || nodes[1] == null) {
			return;
		}

		if (nodes[0].type == DCC.OctreeNodeType.Node_Internal || 
			nodes[1].type == DCC.OctreeNodeType.Node_Internal)
		{
			for (int i = 0; i < 4; i++) {
				OctreeNode[] faceNodes = new OctreeNode[2];
				int[] c = {
					DCC.faceProcFaceMask[dir][i][0], 
					DCC.faceProcFaceMask[dir][i][1], 
				};

				for (int j = 0; j < 2; j++) {
					if (nodes[j].type != DCC.OctreeNodeType.Node_Internal) {
						faceNodes[j] = nodes[j];
					}
					else {
						faceNodes[j] = nodes[j].children[c[j]];
					}
				}

				ContourFaceProc(faceNodes, DCC.faceProcFaceMask[dir][i][2], indices);
			}
			
			int[][] orders = {
				new int[] { 0, 0, 1, 1 },
				new int[] { 0, 1, 0, 1 },
			};
			for (int i = 0; i < 4; i++) {
				OctreeNode[] edgeNodes = new OctreeNode[4];
				int[] c = {
					DCC.faceProcEdgeMask[dir][i][1],
					DCC.faceProcEdgeMask[dir][i][2],
					DCC.faceProcEdgeMask[dir][i][3],
					DCC.faceProcEdgeMask[dir][i][4],
				};

				int[] order = orders[DCC.faceProcEdgeMask[dir][i][0]];
				for (int j = 0; j < 4; j++) {
					if (nodes[order[j]].type == DCC.OctreeNodeType.Node_Leaf ||
						nodes[order[j]].type == DCC.OctreeNodeType.Node_Psuedo) {
						edgeNodes[j] = nodes[order[j]];
					}
					else {
						edgeNodes[j] = nodes[order[j]].children[c[j]];
					}
				}

				ContourEdgeProc(edgeNodes, DCC.faceProcEdgeMask[dir][i][5], indices);
			}
		}
	}
	public static void ContourCellProc(OctreeNode node, List<int> indices) {
		if (node == null) return;

		if (node.type == DCC.OctreeNodeType.Node_Internal) {
			for (int i = 0; i < 8; i++) {
				ContourCellProc(node.children[i], indices);
			}

			for (int i = 0; i < 12; i++) {
				OctreeNode[] faceNodes = new OctreeNode[2];
				int[] c = { DCC.cellProcFaceMask[i][0], DCC.cellProcFaceMask[i][1] };
				
				faceNodes[0] = node.children[c[0]];
				faceNodes[1] = node.children[c[1]];

				ContourFaceProc(faceNodes, DCC.cellProcFaceMask[i][2], indices);
			}

			for (int i = 0; i < 6; i++) {
				OctreeNode[] edgeNodes = new OctreeNode[4];
				int[] c = {
					DCC.cellProcEdgeMask[i][0],
					DCC.cellProcEdgeMask[i][1],
					DCC.cellProcEdgeMask[i][2],
					DCC.cellProcEdgeMask[i][3],
				};

				for (int j = 0; j < 4; j++) {
					edgeNodes[j] = node.children[c[j]];
				}

				ContourEdgeProc(edgeNodes, DCC.cellProcEdgeMask[i][4], indices);
			}
		}
	}


};

}

