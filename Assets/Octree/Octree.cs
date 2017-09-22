using UnityEngine;

namespace SE.Octree {

public static class Octree {
    public static uint lastID = 1;

    // returns new octree from [-1, -1, -1] to [1, 1, 1]
    public static Node Create() {
        Node octree = new Node();
        octree.Position = new Vector3(-1, -1, -1);
        octree.Size = 2;
        octree.IsLeaf = true;
        octree.ID = 0;
        octree.Depth = 0;
        SplitNode(octree);
        return octree;
    }

    public static void SplitNode(Node node) {
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
        }
    }

    public static void CoarsenNode(Node node) {
        UnityEngine.Debug.Assert(node.ID != 0);
        if(!node.Children[0].IsLeaf) {
            Debug.LogWarning("Coarsening node whose children isn't a leaf. ID: " + node.ID);
            for(int i = 0; i < 8; i++) {
                CoarsenNode(node.Children[i]);
            }
        }
        else {
            node.Children = null;
            node.IsLeaf = true;
        }
    }

    // make sure position is between [-1, -1, -1] and [1, 1, 1]
    public static void Adapt(Node octree, Vector3 position, int maxDepth) {
        RecursiveCoarsen(octree, position, maxDepth);
        RecursiveRefine(octree, position, maxDepth);
    }

    public static void LoopCoarsen(Node octree, Vector3 position, int maxDepth) {

    }

    public static void RecursiveRefine(Node node, Vector3 position, int maxDepth) {
        //Debug.Log("Recursive refine at level " + node.Depth + ". PointInNode: " + PointInNode(node, position));
        if(node.IsLeaf) {
            if(node.Depth < maxDepth && PointInNode(node, position)) {
                SplitNode(node);
            }
        }
        else {
            for(int i = 0; i < 8; i++) {
                RecursiveRefine(node.Children[i], position, maxDepth);
            }
        }
    }

    public static void RecursiveCoarsen(Node node, Vector3 position, int maxDepth) {
        if(node.IsLeaf) {
            if(!PointInNode(node.Parent, position) && node.Depth != 0) {
                CoarsenNode(node.Parent);
            }
        }
        else {
            for(int i = 0; i < 8 && !node.IsLeaf; i++) {
                RecursiveCoarsen(node.Children[i], position, maxDepth);
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
}

}