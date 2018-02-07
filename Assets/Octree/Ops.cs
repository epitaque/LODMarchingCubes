using System.Collections.Generic;
using System.Collections;
using UnityEngine;

namespace SE.Octree {

public static class Ops {
    public static uint lastID = 1;

    // returns new octree from [-1, -1, -1] to [1, 1, 1]
    public static Root Create() {
        Root root = new Root();
        root.Nodes = new Dictionary<Vector4, Node>();
        root.IDNodes = new Dictionary<uint, Node>();

        Node rootNode = new Node();
        rootNode.Position = new Vector3(-1, -1, -1);
        rootNode.Size = 2;
        rootNode.IsLeaf = true;
        rootNode.ID = 0;
        rootNode.Depth = 0;
        rootNode.Key = new Vector4(rootNode.Position.x, rootNode.Position.y, rootNode.Position.z, rootNode.Depth);
        root.IDNodes.Add(rootNode.ID, rootNode);
        SplitNode(root, rootNode);
        root.RootNode = rootNode;
        return root;
    }

    public static void SplitNode(Root root, Node node) {
        Debug.Assert(node.IsLeaf);

        node.IsLeaf = false;
        node.Children = new Node[8];

        for(int i = 0; i < 8; i++) {
            Node n = new Node();
            node.Children[i] = n;
            n.Size = node.Size / 2f;
            n.ID = lastID;
            n.Depth = node.Depth + 1;
            lastID++;
            n.Parent = node;
            n.Position = node.Position + (Lookups.Offsets[i] * n.Size);
            n.IsLeaf = true;
            n.Key = new Vector4(n.Position.x, n.Position.y, n.Position.z, n.Depth);
            root.Nodes.Add(n.Key, n);
            root.IDNodes.Add(n.ID, n);
        }
    }

    public static void CoarsenNode(Root root, Node node) {
        UnityEngine.Debug.Assert(node.ID != 0);
        
        for(int i = 0; i < 8; i++) {
            if(!node.Children[i].IsLeaf) {
                //Debug.LogWarning("Coarsening node whose children isn't a leaf. ID: " + node.ID);
                CoarsenNode(root, node.Children[i]);
            }
        }
        for(int i = 0; i < 8; i++) {
            Debug.Assert(root.Nodes.Remove(node.Children[i].Key));
            Debug.Assert(root.IDNodes.Remove(node.Children[i].ID));
        }
        node.Children = null;
        node.IsLeaf = true;
    }

    // make sure position is between [-1, -1, -1] and [1, 1, 1]
    public static void Adapt(Root root, Vector3 position, int maxDepth, int maxIterations) {
		//Debug.Log("Adapting. Root IDNodes: " + root.IDNodes);
		//Debug.Log("Adapting. position: " + position + " maxDepth: " + maxDepth + ", maxIterations: " + maxIterations);
        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
        sw.Start();
        LoopRefine(root, position, maxDepth, maxIterations);
        //sw.Stop(); Debug.Log("BENCH-ADAPT: LoopRefine time: " + (float)sw.ElapsedMilliseconds/1000f + " seconds.");
        //sw.Reset(); sw.Start();
        LoopCoarsen(root, position, maxIterations);
        //sw.Stop(); Debug.Log("BENCH-ADAPT: LoopCoarsen time: " + (float)sw.ElapsedMilliseconds/1000f + " seconds.");
        //sw.Reset(); sw.Start();
        LoopMakeConforming(root, maxIterations);
        sw.Stop(); Debug.Log("BENCH-ADAPT: Adapt time: " + (float)sw.ElapsedMilliseconds + "ms.");
    }

    public static void LoopRefine(Root root, Vector3 position, int maxDepth, int maxIterations) {
        for(int i = 0; i < maxIterations; i++) {
           RecursiveRefine(root, root.RootNode, position, maxDepth);
        }
    }

    public static void LoopCoarsen(Root root, Vector3 position, int maxIterations) {
        for(int i = 0; i < maxIterations; i++) {
           if(RecursiveCoarsen(root, root.RootNode, position)) {
               break;
           }
           if(i == maxIterations - 1) {
                Debug.LogWarning("Maximum LoopCoarsen iterations reached at " + maxIterations);
           }
        }
    }

    public static void LoopMakeConforming(Root root, int maxIterations) {
        Debug.Log("LoopMakeConforming called");


        for(int i = 0; i < maxIterations; i++) {
            Hashtable splitList = new Hashtable();
            bool result = RecursiveMakeConforming(root, root.RootNode, splitList);
            foreach(Node n in splitList.Values) {
                if(n.IsLeaf) {
                    SplitNode(root, n);
                }
            }

            if(result) {
                break;
            }

            if(i == maxIterations - 1) {
                Debug.LogWarning("Maximum LoopMakeConforming iterations reached at " + maxIterations);
            }
        }
    }

    public static bool RecursiveMakeConforming(Root root, Node node, Hashtable splitList) {
        bool returning = true;
        if(node.IsLeaf) {
            Node[] neighbors = FindNeighbors(root, node);
            //Debug.Log("neighbors length: " + neighbors.Count);
            foreach(Node neighbor in neighbors) {
                if(neighbor == null) continue;
                if(node.ID == 28) {
                    Debug.Log("node #" + node.ID + " depth: " + node.Depth + ", neighbor (#" + neighbor.ID + ") depth: " + neighbor.Depth);
                }
                if(node.Depth - 1 > neighbor.Depth && neighbor.IsLeaf) {
                    //Debug.Assert(neighbor.IsLeaf);
                    //Debug.LogWarning("Splitting node " + neighbor.ID + " to make octree conforming");
                    if(!splitList.ContainsKey(neighbor.Key)) {
                        splitList.Add(neighbor.Key, neighbor);
                    }
                    //SplitNode(root, neighbor);
                    returning = false;
                }
            }
        }
        else {
            for(int i = 0; i < 8; i++) {
                if(!RecursiveMakeConforming(root, node.Children[i], splitList)) {
                    returning = false;
                }
            }
        }
        return returning;
    }

    public static readonly Vector3[] Directions = { 
        new Vector3(-1, 0, 0), new Vector3(1, 0, 0), 
        new Vector3(0, -1, 0), new Vector3(0, 1, 0), 
        new Vector3(0, 0, -1), new Vector3(0, 0, 1) };
    public static Node[] FindNeighbors(Root root, Node node) {
        Node[] neighbors = new Node[6];
        for(int i = 0; i < 6; i++) {
            Vector3 dir = Ops.Directions[i];
            neighbors[i] = RecursiveGetNeighbor(root, node, dir);
        }
        return neighbors;
    }

    public static Node RecursiveGetNeighbor(Root root, Node node, Vector3 direction) {
        if(node.Depth == 0) {
            return null;
        }
        Vector4 code = GetCollapsedCode(node, direction);
        if(root.Nodes.ContainsKey(code)) {
            return root.Nodes[code];
        }
        else {
            return RecursiveGetNeighbor(root, node.Parent, direction);
        }
    }

    public static Vector4 GetCollapsedCode(Node node, Vector3 direction) {
        Vector3 scaled = (direction*2)/Mathf.Pow(2, node.Depth);
        Vector3 newPos = node.Position + scaled;

        return new Vector4(newPos.x, newPos.y, newPos.z, node.Depth);
    }


    public static void RecursiveRefine(Root root, Node node, Vector3 position, int maxDepth) {
        //Debug.Log("Recursive refine at level " + node.Depth + ". PointInNode: " + PointInNode(node, position));
        if(node.IsLeaf) {
            if(node.Depth < maxDepth && PointInNode(node, position)) {
                SplitNode(root, node);
            }
        }
        else {
            for(int i = 0; i < 8; i++) {
                RecursiveRefine(root, node.Children[i], position, maxDepth);
            }
        }
    }

    public static bool RecursiveCoarsen(Root root, Node node, Vector3 position) {
        bool returning = true;
        if(node.IsLeaf) {
            if(!PointInNode(node.Parent, position) && node.Depth != 0) {
                CoarsenNode(root, node.Parent);
                returning = false;
            }
        }
        else {
            for(int i = 0; i < 8 && !node.IsLeaf; i++) {
                if(!RecursiveCoarsen(root, node.Children[i], position)) {
                    returning = false;
                }
            }  
        }
        return returning;
    }

    public static bool PointInNode(Node node, Vector3 point) {
        return (point.x >= 
                node.Position.x && 
                point.y >= 
                node.Position.y && 
                point.z >= 
                node.Position.z &&
                
                point.x <= (node.Position.x + node.Size) && 
                point.y <= (node.Position.y + node.Size) && 
                point.z <= (node.Position.z + node.Size));
    }

    public static void DrawGizmos(Node octree, float worldSize) {
        DrawGizmosRecursive(octree, worldSize);
    }

    public static void DrawGizmosRecursive(Node node, float worldSize) {
        if(!node.IsLeaf) {
            for(int i = 0; i < 8; i++) {
                DrawGizmosRecursive(node.Children[i], worldSize);
            }
        }
        DrawNode(node, worldSize);
    }

    public static void DrawNode(Node node, float worldSize) {
        Gizmos.color = UtilFuncs.SinColor( ((float)(node.Depth) * 15f));
        UnityEngine.Gizmos.DrawWireCube( (node.Position + new Vector3(node.Size / 2f, node.Size / 2f, node.Size / 2f)) * worldSize, node.Size * Vector3.one * worldSize);
    }

    public static MCMesh PolyganizeNode(Node node, float worldSize, int resolution) {
		float mul = node.Size;

        UtilFuncs.Sampler sample = (float x, float y, float z) => UtilFuncs.Sample(
			( ((x*node.Size) / resolution) + node.Position.x) * worldSize, 
			( ((y*node.Size) / resolution) + node.Position.y) * worldSize, 
			( ((z*node.Size) / resolution) + node.Position.z) * worldSize);

        /*Node[] neighbors = FindNeighbors(root, node);

        int currentSide = 1;

        for(int i = 0; i < 6; i++) {
            if(neighbors[i] != null && neighbors[i].Depth < node.Depth) {
                lod |= (byte)currentSide;
            }
            currentSide = currentSide << (byte)1;
        }*/
        node.LODSides = 0;

		sbyte[][][][] data = GenerateChunkData(resolution, sample);

        MCMesh m = SE.MarchingCubes.PolygonizeArea(new Vector3(0, 0, 0), node.LODSides, resolution, data);

		//Mesh m2 = new Mesh();
		//m2.SetVertices(m.Vertices);
		//m2.SetNormals(m.Normals);
		//m2.triangles = m.Triangles;

        return m;
    }

	public static sbyte[][][][] GenerateChunkData(int resolution, UtilFuncs.Sampler sample) {
		int res1 = resolution + 1;

		sbyte[][][][] data = new sbyte[res1][][][];

		float f = 0.01f;
		float nx, ny, nz;

		for(int x = 0; x < res1; x++) {
			data[x] = new sbyte[res1][][];
			for(int y = 0; y < res1; y++) {
				data[x][y] = new sbyte[res1][];
				for(int z = 0; z < res1; z++) {
					data[x][y][z] = new sbyte[4];
					nx = (float)x; //- ((float)res1)/2f;
					ny = (float)y; //- ((float)res1)/2f;
					nz = (float)z; //- ((float)res1)/2f;

					data[x][y][z][0] = (sbyte)(Mathf.Clamp(-8f * sample(nx, ny, nz), -127, 127));

					float dx = sample(nx+f, ny, nz) - sample(nx-f, ny, nz);
					float dy = sample(nx, ny+f, nz) - sample(nx, ny-f, nz);
					float dz = sample(nx, ny, nz+f) - sample(nx, ny, nz-f);

					float total = (dx*dx) + (dy*dy) + (dz*dz);
					total = Mathf.Sqrt(total);
					dx /= total; dx *= 127;
					dy /= total; dy *= 127;
					dz /= total; dz *= 127;

					data[x][y][z][1] = (sbyte)dx;
					data[x][y][z][2] = (sbyte)dy;
					data[x][y][z][3] = (sbyte)dz;
				} 
			}
		}

		return data;
	}
}

}