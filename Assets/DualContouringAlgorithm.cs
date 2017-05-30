using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using SE.DC;

namespace SE {
public static class DualContouringAlgorithm {
    private static OctreeNode root;

    public static Util.ExtractionResult Run(int resolution, float isovalue, UtilFuncs.Sampler sample, bool flatShading) {
        Stopwatch sw = Stopwatch.StartNew();
        Util.ExtractionResult result = new Util.ExtractionResult();

        result.mesh = GenerateMesh(resolution, isovalue, sample , flatShading);
        result.time = sw.ElapsedMilliseconds;
        sw.Stop();
        return result;
    }

    private static Mesh GenerateMesh(int resolution, float isovalue, UtilFuncs.Sampler sample, bool flatShading) {
        Stopwatch sw = Stopwatch.StartNew();

        List<Vector3> vertices = new List<Vector3>();
        OctreeNode octree = new OctreeNode();
        root = octree;
        octree.size = resolution;
        octree.sample = (float x, float y, float z) => { return -sample(x, y, z); };
        octree.isovalue = isovalue;
        OctreeNode.ConstructOctreeNodes(octree);

        UnityEngine.Debug.Log("Octree Construction time: " + sw.ElapsedMilliseconds);
        UnityEngine.Debug.Log("Num min nodes: " + OctreeNode.numMinNodes);
        sw.Stop();

        return GenerateMeshFromOctree(octree);

    }

    private static Mesh GenerateMeshFromOctree(OctreeNode node)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<int> indices = new List<int>();

        if (node == null) return null;

        OctreeNode.GenerateVertexIndices(node, vertices, normals);
        OctreeNode.ContourCellProc(node, indices);

        Mesh m = new Mesh();
        m.vertices = vertices.ToArray();
        m.normals = normals.ToArray();
        m.triangles = indices.ToArray();
        return m;
    }

    public static void DrawGizmos() {
        OctreeNode.DrawGizmos(root);

        
    }

};
}
