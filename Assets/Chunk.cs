using System.Collections.Generic;
using UnityEngine;

namespace SE.DC {
	public class DCMesh {
		public List<Vector3> Vertices;
		public List<int> Indices;
		public List<Vector3> Normals;
		public DCMesh() { Vertices = new List<Vector3>(); Indices = new List<int>(); Normals = new List<Vector3>(); }
	}

	public class Chunk {
		OctreeNode root;
		public int size;
		public Vector3 min;
		public DCMesh mesh;

		static readonly Vector3[] OFFSETS = { 
			new Vector3(0,0,0), new Vector3(1,0,0), new Vector3(0,0,1), new Vector3(1,0,1), 
			new Vector3(0,1,0), new Vector3(1,1,0), new Vector3(0,1,1), new Vector3(1,1,1) 
		};

		public delegate bool SelectionFunction(Vector3 min, Vector3 max);

		public static List<OctreeNode> FindSeamNodes(Chunk chunk, ChunkManager chunkManager) {
			List<OctreeNode> seamNodes = new List<OctreeNode>();

			Vector3 baseChunkMin = chunk.min;
			Vector3 seamValues = baseChunkMin + new Vector3(chunk.size, chunk.size, chunk.size);

			SelectionFunction[] selectionFuncs =
			{
				(Vector3 min, Vector3 max) =>
				{ 
					return max.x == seamValues.x || max.y == seamValues.y || max.z == seamValues.z;
				},
				
				(Vector3 min, Vector3 max) =>
				{ 
					return min.x == seamValues.x; 
				},
				
				(Vector3 min, Vector3 max) =>
				{ 
					return min.z == seamValues.z; 
				},
				
				(Vector3 min, Vector3 max) => 
				{ 
					return min.x == seamValues.x && min.z == seamValues.z; 
				},

				(Vector3 min, Vector3 max) => 
				{ 
					return min.y == seamValues.y; 
				},
				
				(Vector3 min, Vector3 max) => 
				{ 
					return min.x == seamValues.x && min.y == seamValues.y; 
				},

				(Vector3 min, Vector3 max) => 
				{ 
					return min.y == seamValues.y && min.z == seamValues.z; 
				},
				
				(Vector3 min, Vector3 max) => 
				{ 
					return min.x == seamValues.x && min.y == seamValues.y && min.z == seamValues.z; 
				},
			};

			for (int i = 0; i < 8; i++)
			{
				Vector3 offsetMin = OFFSETS[i] * chunk.size;
				Vector3 chunkMin = baseChunkMin + offsetMin;
				Chunk c = chunkManager.GetChunk(chunkMin);
				if (c != null)
				{
					List<OctreeNode> chunkNodes = FindNodes(chunk, selectionFuncs[i]);
					seamNodes.AddRange(chunkNodes);
				}
			}

			return seamNodes;

		}

		public static List<OctreeNode> FindNodes(Chunk chunk, SelectionFunction func) {
			List<OctreeNode> nodes = new List<OctreeNode>();
			OctreeNode root = chunk.root;
			FindNodes(root, func, nodes);
			return nodes;
		}
		public static void FindNodes(OctreeNode node, SelectionFunction func, List<OctreeNode> nodes) {
			if(func(node.min, node.min + Vector3.one * node.size)) {
				nodes.Add(node);
			}

			if(node.children != null) {
				for(int i = 0; i < node.children.Length; i++) {
					if(node.children[i] != null) FindNodes(node.children[i], func, nodes);
				}
			}
		}
	}
}