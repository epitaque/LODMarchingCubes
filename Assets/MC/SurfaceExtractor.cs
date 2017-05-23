using UnityEngine;
using System.Collections.Generic;


public static class SurfaceExtractor {
    public static Mesh ExtractSurface(Util.Sampler sample, float isovalue, int resolution, int xSize, int ySize, int zSize) {
        Mesh mesh = new Mesh();
        Util.GridCell cell = new Util.GridCell();
        cell.points = new Util.Point[8];
        for(int i = 0; i < 8; i++) {
            cell.points[i] = new Util.Point();
            cell.points[i].position = new Vector3();
        }

        List<Vector3> vertices = new List<Vector3>();

        Vector3[] OFFSETS = {
            new Vector3(0f,0f,0f), new Vector3(1f,0f,0f), new Vector3(1f,1f,0f), new Vector3(0f,1f,0f), 
            new Vector3(0f,0f,1f), new Vector3(1f,0f,1f), new Vector3(1f,1f,1f), new Vector3(0f,1f,1f) };	

        for(int x = 0; x < (resolution * xSize) - xSize; x += xSize) {
            //for(int y = 0; y < resolution * size; y += size) {
                int y = 0;
                for(int z = 0; z < resolution * zSize; z += zSize) {										
                    for(int i = 0; i < 8; i++) {
                        cell.points[i].position = new Vector3(x, y, z) + new Vector3(xSize *OFFSETS[i].x, ySize * OFFSETS[i].y, zSize*OFFSETS[i].z);
                        cell.points[i].density = sample(cell.points[i].position.x, cell.points[i].position.y, cell.points[i].position.z);
                    }
                    SE.Polyganiser.Polyganise(cell, vertices, isovalue);

                }
            //}
        }

        int x_ = (resolution * xSize) - xSize;
        int y_ = 0;
            for(int z = 0; z < resolution * zSize; z += zSize) {

                for(int i = 0; i < 8; i++) {
                    cell.points[i].position = new Vector3(x_, y_, z) + new Vector3(xSize *OFFSETS[i].x, ySize * OFFSETS[i].y, zSize*OFFSETS[i].z);
                    cell.points[i].density = sample(cell.points[i].position.x, cell.points[i].position.y, cell.points[i].position.z);
                }
                SE.Polyganiser.Polyganise(cell, vertices, isovalue);

            }


        int[] triangles = new int[vertices.Count];
        for(int i = 0; i < vertices.Count; i ++) {
            triangles[i] = i;
        }

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles;
        return mesh;
    }

    //public static Vector3 FirstTriOffsets
}