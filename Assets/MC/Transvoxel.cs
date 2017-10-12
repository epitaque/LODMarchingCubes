using System.Collections.Generic;
using UnityEngine;

namespace SE.Transvoxel {
public static class Transvoxel {
    public static Vector3Int[] coords = {
            new Vector3Int(0,0,0), new Vector3Int(1,0,0), new Vector3Int(2,0,0), // High-res lower row
            new Vector3Int(0,1,0), new Vector3Int(1,1,0), new Vector3Int(2,1,0), // High-res middle row
            new Vector3Int(0,2,0), new Vector3Int(1,2,0), new Vector3Int(2,2,0), // High-res upper row

            new Vector3Int(0,0,2), new Vector3Int(2,0,2), // Low-res lower row
            new Vector3Int(0,2,2), new Vector3Int(2,2,2)  // Low-res upper row
    };


    //int[] order = {0, 1, 2, 5, 8, 7, 6, 3, 4};

    public static List<Edge> Edges = new List<Edge>();

    public static void GenerateTransitionCell(Vector3Int min, List<Vector3> Vertices, List<int> Triangles, byte lod, sbyte[] data) {
        //int caseCode = 0;
        int f = 1;

        int basis = 1;

        Vector3Int[] pos = {
                min + coords[0x00], min + coords[0x01], min + coords[0x02],
                min + coords[0x03], min + coords[0x04], min + coords[0x05],
                min + coords[0x06], min + coords[0x07], min + coords[0x08],
                min + coords[0x09], min + coords[0x0A],
                min + coords[0x0B], min + coords[0x0C],
        };

        sbyte[] fdata = new sbyte[13];
        for(int i = 0; i < 9; i++) {
            fdata[i] = data[i];
        }
        fdata[0x9] = data[0];
        fdata[0xA] = data[2];
        fdata[0xB] = data[6];
        fdata[0xC] = data[8];

        /*int caseCode =  (data[pos[0].x][pos[0].y][pos[0].z][0] & 256) * 0x001 |
                        data[pos[1]] & 256 * 0x002 |
                        data[pos[2]] & 256 * 0x004 |
                        data[pos[5]] & 256 * 0x008 |
                        data[pos[8]] & 256 * 0x010 |
                        data[pos[7]] & 256 * 0x020 |
                        data[pos[6]] & 256 * 0x040 |
                        data[pos[3]] & 256 * 0x080 |
                        data[pos[4]] & 256 * 0x100;         */                    

        int caseCode = 0;

        for(int i = 0; i < 9; i++) {
            if(data[i] > 0) caseCode |= f;
            f *= 2;
        }

        if (caseCode == 0 || caseCode == 511) {
            return;
        }

        byte classIndex = TVTables.TransitionCellClass[caseCode];

        TVTables.RegularCell tCellData = TVTables.TransitionRegularCellData[classIndex & 0x7F];
        bool inverse = (classIndex & 128) != 0;

        

        long vertCount = tCellData.GetVertexCount();
        long triCount = tCellData.GetTriangleCount();

        ushort[] edgeData = TVTables.TransitionVertexData[caseCode];

        byte[] lowNibbles = new byte[edgeData.Length];
        byte[][] edges = new byte[edgeData.Length][];

        Vector3[] vertices = new Vector3[12];

        Debug.Log("#Edges: " + edgeData.Length);

        // vertex generation phase
        for(int i = 0; i < edges.Length; i++) {
            lowNibbles[i] = (byte)(edgeData[i] & 0x00FF);
            Debug.Log("lowNibbles[" + i + "]: " + lowNibbles[i]);

            edges[i] = new byte[2];

            edges[i][1] = (byte)(lowNibbles[i] & 0x0F);
            edges[i][0] = (byte)((lowNibbles[i] & 0xF0) >> 4);

            Debug.Log("edges [" + i + "] vert [0]: " + edges[i][0] + ", vert [1]: " + edges[i][1]);

            Vector3 A = coords[edges[i][0]];
            Vector3 B = coords[edges[i][1]];

            Edge e = new Edge();
            e.A = A;
            e.B = B;

            sbyte density1 = fdata[edges[i][0]];
            sbyte density2 = fdata[edges[i][1]];
            Debug.Log("A (" + density1 + "): " + A + ", B (" + density2 + "): " + B);

            Vector3 result = Lerp(density1, density2, A.x, A.y, A.z, B.x, B.y, B.z);

            e.IsoVertex = result;
            Edges.Add(e);

            Debug.Log("Lerped: " + result);

            vertices[i] = result;
        }

        byte[] indices = tCellData.Indices();

        int[] intIndices = new int[triCount * 3];

        string indicesStr = "";

        for(int i = 0; i < triCount * 3; i++) {
            intIndices[i] = indices[i];
            indicesStr += indices[i] + ", ";
        }

        Debug.Log("Indices: " + indicesStr);

        Vertices.AddRange(vertices);
        Triangles.AddRange(intIndices);

        Debug.Assert(vertCount <= 12);
    }

    public static Vector3 Lerp(float density1, float density2, float x1, float y1, float z1, float x2, float y2, float z2) {
        if(density1 < 0.00001f && density1 > -0.00001f) {
            return new Vector3(x1, y1, z1);
        }
        if(density2 < 0.00001f && density2 > -0.00001f) {
            return new Vector3(x2, y2, z2);
        }
        /*if(Mathf.Abs(density1 - density2) < 0.00001f) {
            return new Vector3(x2, y2, z2);
        }*/

        float mu = Mathf.Round((density1) / (density1 - density2) * 256) / 256.0f; 

        return new Vector3(x1 + mu * (x2 - x1), y1 + mu * (y2 - y1), z1 + mu * (z2 - z1));
    }

    public static void DrawGizmos() {
        Gizmos.color = Color.gray;
        foreach(Edge e in Edges) {
            UnityEngine.Gizmos.DrawLine(e.A, e.B);
        }

        Gizmos.color = Color.green;
        foreach(Edge e in Edges) {
            UnityEngine.Gizmos.DrawSphere(e.IsoVertex, 0.2f);
        }
    }

    public class Edge {
        public Vector3 A;
        public Vector3 B;
        public Vector3 IsoVertex;
    }

}
}