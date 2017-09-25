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
        if(!node.Children[0].IsLeaf) {
            Debug.LogWarning("Coarsening node whose children isn't a leaf. ID: " + node.ID);
            for(int i = 0; i < 8; i++) {
                CoarsenNode(root, node.Children[i]);
            }
        }
        else {
            for(int i = 0; i < 8; i++) {
                root.Nodes.Remove(node.Children[i].Key);
                root.IDNodes.Remove(node.Children[i].ID);
            }
            node.Children = null;
            node.IsLeaf = true;
        }
    }

    // make sure position is between [-1, -1, -1] and [1, 1, 1]
    public static void Adapt(Root root, Vector3 position, int maxDepth, int maxIterations) {
        LoopRefine(root, position, maxDepth, maxIterations);
        //LoopCoarsen(root, position, maxIterations);
    }

    public static void LoopRefine(Root root, Vector3 position, int maxDepth, int maxIterations) {
        for(int i = 0; i < maxIterations; i++) {
           RecursiveRefine(root, root.RootNode, position, maxDepth);
        }
    }

    public static void LoopCoarsen(Root root, Vector3 position, int maxIterations) {
        for(int i = 0; i < maxIterations; i++) {
           RecursiveCoarsen(root, root.RootNode, position);
        }
    }

    public static void LoopMakeConforming(Root root, int maxIterations) {
        for(int i = 0; i < maxIterations; i++) {
            if(RecursiveMakeConforming(root, root.RootNode)) {
                //return;
            }
        }
    }

    public static bool RecursiveMakeConforming(Root root, Node node) {
        bool returning = true;
        if(node.IsLeaf) {
            List<Node> neighbors = FindNeighbors(root, node);
            Debug.Log("neighbors length: " + neighbors.Count);
            foreach(Node neighbor in neighbors) {
                if(node.ID == 28) {
                    Debug.Log("node #" + node.ID + " depth: " + node.Depth + ", neighbor (#" + neighbor.ID + ") depth: " + neighbor.Depth);
                }
                if(neighbor.Depth < node.Depth + 1 && neighbor.IsLeaf) {
                    //Debug.Assert(neighbor.IsLeaf);
                    Debug.LogWarning("Splitting node " + neighbor.ID + " to make octree conforming");
                    SplitNode(root, neighbor);
                    returning = false;
                }
            }
        }
        else {
            for(int i = 0; i < 8; i++) {
                RecursiveMakeConforming(root, node.Children[i]);
            }
        }
        return returning;
    }

    public static readonly Vector3[] Directions = { 
        new Vector3(1, 0, 0), new Vector3(-1, 0, 0), 
        new Vector3(0, 1, 0), new Vector3(0, -1, 0), 
        new Vector3(0, 0, 1), new Vector3(0, 0, -1) };
    public static List<Node> FindNeighbors(Root root, Node node) {
        List<Node> neighbors = new List<Node>();
        foreach(Vector3 dir in Ops.Directions) {
            Node neighbor = RecursiveGetNeighbor(root, node, dir);
            if(neighbor != null) {
                neighbors.Add(neighbor);
            }
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

    public static void RecursiveCoarsen(Root root, Node node, Vector3 position) {
        if(node.IsLeaf) {
            if(!PointInNode(node.Parent, position) && node.Depth != 0) {
                CoarsenNode(root, node.Parent);
            }
        }
        else {
            for(int i = 0; i < 8 && !node.IsLeaf; i++) {
                RecursiveCoarsen(root, node.Children[i], position);
            }  
        }
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

    public static void DrawGizmos(Node octree) {
        DrawGizmosRecursive(octree);
    }

    public static void DrawGizmosRecursive(Node node) {
        if(!node.IsLeaf) {
            for(int i = 0; i < 8; i++) {
                DrawGizmosRecursive(node.Children[i]);
            }
        }
        DrawNode(node);
    }

    public static float scale = 64f;
    public static void DrawNode(Node node) {
        Gizmos.color = UtilFuncs.SinColor( ((float)(node.Depth) * 15f));
        UnityEngine.Gizmos.DrawWireCube( (node.Position + new Vector3(node.Size / 2f, node.Size / 2f, node.Size / 2f)) * scale, node.Size * Vector3.one * scale);
    }

    public static Mesh PolyganizeNode(Node node, float WorldSize) {
        Mesh m = new Mesh();
        ExtractionInput input = new ExtractionInput();
        input.Isovalue = 0f;
        input.Resolution = new Util.Vector3i(16, 16, 16);
        float size = WorldSize / (Mathf.Pow(2, node.Depth));
        input.Size = new Vector3(node.Size/16f, node.Size/16f, node.Size/16f);
        input.Sample = (float x, float y, float z) => UtilFuncs.Sample((x + node.Position.x) * 8f, (y + node.Position.y) * 8f, (z + node.Position.z) * 8f);
        input.LODSides = new byte();

        ExtractionResult res = SurfaceExtractor.ExtractSurface(input);

        m.vertices = res.Vertices;
        m.triangles = res.Triangles;

        m.RecalculateNormals();

        return m;
    }
}

}