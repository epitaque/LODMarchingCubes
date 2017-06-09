using UnityEngine;
using System.Collections.Generic;
using Util;

public static class SurfaceExtractor {
    public static ExtractionResult ExtractSurface(ExtractionInput input) {
        ExtractionResult r = new ExtractionResult();

        List<GridCell> cells = new List<GridCell>();

        Mesh mesh = new Mesh();
        GridCell cell = new GridCell();
        cell.points = new Point[8];
        for(int i = 0; i < 8; i++) {
            cell.points[i] = new Point();
            cell.points[i].position = new Vector3();
        }

        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> sampledPoints = new List<Vector3>();

        Vector3[] OFFSETS = {
            new Vector3(0f,0f,0f), new Vector3(1f,0f,0f), new Vector3(1f,1f,0f), new Vector3(0f,1f,0f), 
            new Vector3(0f,0f,1f), new Vector3(1f,0f,1f), new Vector3(1f,1f,1f), new Vector3(0f,1f,1f) 
        };	

        // Generate Cells
        for(int x = 0; x < input.Resolution.x; x++) {
            for(int y = 0; y < input.Resolution.y; y++) {
                for(int z = 0; z < input.Resolution.z; z++) {
                    byte edgeSides = 0;
                    if(x == 0) edgeSides |= 1;
                    if(x == input.Resolution.x - 1) edgeSides |= 2;
                    if(y == 0) edgeSides |= 4;
                    if(y == input.Resolution.y - 1) edgeSides |= 8;
                    if(z == 0) edgeSides |= 16;
                    if(z == input.Resolution.z - 1) edgeSides |= 32;

                    byte lod = (byte)(input.LODSides & edgeSides);

                    // cell is regular
                    if(lod == 0) {
                        for(int i = 0; i < 8; i++) {
                            cell.points[i].position = new Vector3(input.Size.x * (x + OFFSETS[i].x), 
                                                                input.Size.y * (y + OFFSETS[i].y), 
                                                                input.Size.z * (z + OFFSETS[i].z));
                            cell.points[i].density = input.Sample(cell.points[i].position.x, cell.points[i].position.y, cell.points[i].position.z);
                            sampledPoints.Add(cell.points[i].position);
                        }
                        cells.Add(cell.Clone());
                        SE.Polyganiser.Polyganise(cell, vertices, input.Isovalue);
                    }
                    // cell is (part) of a transition cell
                    else {
                        bool checkXaxis = true;
                        bool checkYaxis = true;
                        bool checkZaxis = true;

                        // x-axis face
                        if( (lod & 1) == 1 || (lod & 2) == 2 ) {
                            
                        }

                        Vector3 min = new Vector3(input.Size.x * x, 
                                                  input.Size.y * y, 
                                                  input.Size.z * z);

                        GridCell[] cellsToProcess = ProcessTransitionCell(lod, min, input.Size, input.Sample);

                        for(int i = 0; i < cells.Count; i++) {
                            SE.Polyganiser.Polyganise(cellsToProcess[i], vertices, input.Isovalue);
                        }
                    }
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

    public static GridCell[] ProcessTransitionCell(byte lod, Vector3 min, Vector3 size, UtilFuncs.Sampler Sample) {


        return null;
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
        { // top left corner cell ISSUE CELL
            new Vector3(0f,1f,0f), new Vector3(1f,2f,1f), new Vector3(1f,2f,1f), new Vector3(0f,2f,0f), 
            new Vector3(0f,1f,1f), new Vector3(1f,2f,1f), new Vector3(1f,2f,1f), new Vector3(0f,2f,1f) 
        }, 
        { // bottom left corner cell
            new Vector3(0f,0f,0f), new Vector3(1f,0f,1f), new Vector3(1f,0f,1f), new Vector3(0f,1f,0f), 
            new Vector3(0f,0f,1f), new Vector3(1f,0f,1f), new Vector3(1f,0f,1f), new Vector3(0f,1f,1f) 
        }, 
        { // top right corner cell
            new Vector3(0f,1f,-1f), new Vector3(1f,2f,-1f), new Vector3(1f,2f,-1f), new Vector3(0f,2f,-1f), 
            new Vector3(0f,1f,0f), new Vector3(1f,2f,-1f), new Vector3(1f,2f,-1f), new Vector3(0f,2f,0f) 
        },
        { // bottom right corner cell
            new Vector3(0f,0f,-1f), new Vector3(1f,0f,-1f), new Vector3(1f,0f,-1f), new Vector3(0f,1f,-1f), 
            new Vector3(0f,0f,0f), new Vector3(1f,0f,-1f), new Vector3(1f,0f,-1f), new Vector3(0f,1f,0f) 
        },
        { // left edge cell
            new Vector3(0f,1f,1f), new Vector3(1f,0f,1f), new Vector3(0f,1f,0f), new Vector3(0f,1f,0f), 
            new Vector3(0f,1f,1f), new Vector3(1f,0f,1f), new Vector3(1f,2f,1f), new Vector3(0f,1f,1f) 
        },
        { // right edge cell
            new Vector3(0f,1f,0f), new Vector3(1f,0f,-1f), new Vector3(0f,1f,-1f), new Vector3(0f,1f,-1f), 
            new Vector3(0f,1f,0f), new Vector3(1f,0f,-1f), new Vector3(1f,2f,-1f), new Vector3(0f,1f,0f) 
        },
        { // bottom edge cell
            new Vector3(0f,0f,0f), new Vector3(1f,0f,-1f), new Vector3(1f,0f,-1f), new Vector3(0f,1f,0f), 
            new Vector3(0f,0f,0f), new Vector3(1f,0f,1f), new Vector3(1f,0f,1f), new Vector3(0f,1f,0f)	
        },
        { // top edge cell
            new Vector3(0f,1f,0f), new Vector3(1f,2f,-1f), new Vector3(1f,2f,-1f), new Vector3(0f,2f,0f), 
            new Vector3(0f,1f,0f), new Vector3(1f,2f,1f), new Vector3(1f,2f,1f), new Vector3(0f,2f,0f)	
        },
        { // middle cell
            new Vector3(0f,1f,0f), new Vector3(1f,0f,-1f), new Vector3(1f,2f,-1f), new Vector3(0f,1f,0f), 
            new Vector3(0f,1f,0f), new Vector3(1f,0f,1f), new Vector3(1f,2f,1f), new Vector3(0f,1f,0f) 
        }
    };


    // -x, +x, -y, y, -z, z
    public readonly static Quaternion[] VectorAxisModifications = {
        Quaternion.Euler(180, 0, 0), Quaternion.Euler(0, 0, 0),
        Quaternion.Euler(0, -90, 0), Quaternion.Euler(0, 90, 0),
        Quaternion.Euler(0, 0, -90), Quaternion.Euler(0, 0, 90)
    };
}
public class ExtractionResult {
     public UnityEngine.Mesh m;
     public List<GridCell> cells;
     public List<Vector3> sampledPoints;
     public Vector3 offset;
}
public class ExtractionInput {
    public UtilFuncs.Sampler Sample;
    public float Isovalue;
    
    public Vector3i Resolution;
    public Vector3 Size;
    // first six bits represent the sides of the chunk that are LOD transition sides -x, +x, -y, +y, -z, +z
    public byte LODSides;
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