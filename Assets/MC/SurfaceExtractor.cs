using UnityEngine;
using System.Collections.Generic;


public static class SurfaceExtractor {
    public static ExtractionResult ExtractSurface(ExtractionInput input) {
        ExtractionResult r = new ExtractionResult();

        List<Util.GridCell> cells = new List<Util.GridCell>();

        Mesh mesh = new Mesh();
        Util.GridCell cell = new Util.GridCell();
        cell.points = new Util.Point[8];
        for(int i = 0; i < 8; i++) {
            cell.points[i] = new Util.Point();
            cell.points[i].position = new Vector3();
        }


        List<Vector3> vertices = new List<Vector3>();

        List<Vector3> sampledPoints = new List<Vector3>();

        Vector3[] OFFSETS = {
            new Vector3(0f,0f,0f), new Vector3(1f,0f,0f), new Vector3(1f,1f,0f), new Vector3(0f,1f,0f), 
            new Vector3(0f,0f,1f), new Vector3(1f,0f,1f), new Vector3(1f,1f,1f), new Vector3(0f,1f,1f) 
        };	

        for(int x = 0; x < (input.Resolution.x * input.Size.x) - input.Size.x; x += input.Size.x) {
            for(int y = 0; y < input.Resolution.y * input.Size.y; y += input.Size.y) {
                for(int z = 0; z < input.Resolution.z * input.Size.z; z += input.Size.z) {										
                    for(int i = 0; i < 8; i++) {
                        cell.points[i].position = new Vector3(x, y, z) + new Vector3(input.Size.x * OFFSETS[i].x, input.Size.y * OFFSETS[i].y, input.Size.z * OFFSETS[i].z);
                        cell.points[i].density = input.Sample(cell.points[i].position.x, cell.points[i].position.y, cell.points[i].position.z);
                        sampledPoints.Add(cell.points[i].position);
                    }
                    cells.Add(cell.Clone());
                    SE.Polyganiser.Polyganise(cell, vertices, input.Isovalue);
                }
            }
        }

        int x_ = (input.Resolution.x * input.Size.x) - input.Size.x;
        int y_ = 0;
        for(int y = 0; y < input.Size.y * input.Resolution.y; y += input.Size.y * 2) {
            for(int z = input.Size.z; z < input.Resolution.z * input.Size.z; z += input.Size.z * 2) {
                for(int j = 0; j < LOD2Offsets.GetLength(0); j++) {
                    for(int i = 0; i < 8; i++) {
                        cell.points[i].position = new Vector3(x_, y_, z) + new Vector3(input.Size.x * LOD2Offsets[j,i].x, 
                                                                                    input.Size.y * LOD2Offsets[j,i].y, 
                                                                                    input.Size.z * LOD2Offsets[j,i].z);
                        cell.points[i].density = input.Sample(cell.points[i].position.x, cell.points[i].position.y, cell.points[i].position.z);
                        sampledPoints.Add(cell.points[i].position);
                    }
                    cells.Add(cell.Clone());
                    SE.Polyganiser.Polyganise(cell, vertices, input.Isovalue);
                }
            }
        }


        int[] triangles = new int[vertices.Count];
        for(int i = 0; i < vertices.Count; i ++) {
            triangles[i] = i;
        }

        r.sampledPoints = sampledPoints;
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles;
        r.m = mesh;
        r.cells = cells;

        return r;
    }

    // [triNum][offsetNum]
    public readonly static Vector3[,] LODOffsets = {
        {           
            new Vector3(0f,0f,0f), new Vector3(0f,0f,0f), new Vector3(0f,1f,0f), new Vector3(0f,1f,0f), 
            new Vector3(0f,0f,1f), new Vector3(1f,0f,1f), new Vector3(1f,1f,1f), new Vector3(0f,1f,1f)	
        },
        {
            new Vector3(0f,0f,0f), new Vector3(1f,0f,-1f), new Vector3(1f,1f,-1f), new Vector3(0f,1f,0f), 
            new Vector3(0f,0f,0f), new Vector3(1f,0f,1f), new Vector3(1f,1f,1f), new Vector3(0f,1f,0f)	
        },
        {
            new Vector3(0f,0f,-1f), new Vector3(1f,0f,-1f), new Vector3(1f,1f,-1f), new Vector3(0f,1f,-1f), 
            new Vector3(0f,0f,0f), new Vector3(1f,0f,-1f), new Vector3(1f,1f,-1f), new Vector3(0f,1f,0f)
        }	
    };

    // total of 5 gridcells

    public readonly static Vector3[,] LOD2Offsets = {
       /* { // bottom left corner cell
            new Vector3(0f,0f,-1f), new Vector3(1f,0f,1f), new Vector3(1f,0f,1f), new Vector3(0f,2f,-1f), 
            new Vector3(0f,0f,1f), new Vector3(1f,0f,1f), new Vector3(1f,0f,1f), new Vector3(0f,2f,1f) 
        },*/
        { // top left corner cell
            new Vector3(0f,0f,-1f), new Vector3(1f,2f,1f), new Vector3(1f,2f,1f), new Vector3(0f,2f,-1f), 
            new Vector3(0f,0f,1f), new Vector3(1f,2f,1f), new Vector3(1f,2f,1f), new Vector3(0f,2f,1f) 
        },
        /*{ // top left corner cell
            new Vector3(0f,0f,0f), new Vector3(1f,0f,0f), new Vector3(1f,1f,0f), new Vector3(0f,1f,0f), 
            new Vector3(0f,0f,1f), new Vector3(1f,0f,1f), new Vector3(1f,1f,1f), new Vector3(0f,1f,1f) 
        },
        { // top right corner cell
            new Vector3(0f,0f,0f), new Vector3(1f,0f,0f), new Vector3(1f,1f,0f), new Vector3(0f,1f,0f), 
            new Vector3(0f,0f,1f), new Vector3(1f,0f,1f), new Vector3(1f,1f,1f), new Vector3(0f,1f,1f) 
        },
        { // middle to 4 cell
            new Vector3(0f,0f,0f), new Vector3(1f,0f,0f), new Vector3(1f,1f,0f), new Vector3(0f,1f,0f), 
            new Vector3(0f,0f,1f), new Vector3(1f,0f,1f), new Vector3(1f,1f,1f), new Vector3(0f,1f,1f) 
        }*/
    };
}

public class ExtractionResult {
     public UnityEngine.Mesh m;
     public List<Util.GridCell> cells;
     public List<Vector3> sampledPoints;
     public Vector3 offset;
}

public class ExtractionInput {
    public Util.Sampler Sample;
    public float Isovalue;
    
    public Util.Vector3i Resolution;
    public Util.Vector3i Size;
}

/*
Vertex and Edge Index Map
		
        7-------6------6
       /.             /|
      10.           11 |
     /  0           /  2
    /   .          /   |     ^ Y
   3-------7------2    |     |
   |    4 . . 4 . |. . 5     --> X
   |   .          |   /		 \/ -Z
   1  8           3  9
   | .            | /
   |.             |/
   0-------5------1
*/