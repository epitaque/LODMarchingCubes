using UnityEngine;
using System.Collections.Generic;
using Util;

public static class SurfaceExtractor {
    public static ExtractionResult ExtractSurface(ExtractionInput input) {
        ExtractionResult r = new ExtractionResult();

        List<GridCell> cells = new List<GridCell>();
        List<GridCell> debugTransitionCells = new List<GridCell>();

        Mesh mesh = new Mesh();
        GridCell cell = new GridCell();
        cell.points = new Point[8];
        for(int i = 0; i < 8; i++) {
            cell.points[i] = new Point();
            cell.points[i].position = new Vector3();
        }

        List<Vector3> vertices = new List<Vector3>();

        Vector3[] OFFSETS = {
            new Vector3(0f,0f,0f), new Vector3(1f,0f,0f), new Vector3(1f,1f,0f), new Vector3(0f,1f,0f), 
            new Vector3(0f,0f,1f), new Vector3(1f,0f,1f), new Vector3(1f,1f,1f), new Vector3(0f,1f,1f) 
        };	

        // Generate Regular Cells
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
                        }
                        cells.Add(cell.Clone());
                        SE.Polyganiser.Polyganise(cell, vertices, input.Isovalue);
                    }
                }
            }
        }

        // Generate Transition Cells
        for(int x = 0; x < input.Resolution.x/2; x++) {
            for(int y = 0; y < input.Resolution.y/2; y++) {
                for(int z = 0; z < input.Resolution.z/2; z++) {
                    byte edgeSides = 0;
                    if(x == 0) edgeSides |= 1;
                    if(x == (input.Resolution.x/2) - 1) edgeSides |= 2;
                    if(y == 0) edgeSides |= 4;
                    if(y == (input.Resolution.y/2) - 1) edgeSides |= 8;
                    if(z == 0) edgeSides |= 16;
                    if(z == (input.Resolution.z/2) - 1) edgeSides |= 32;

                    byte lod = (byte)(input.LODSides & edgeSides);

                    // Is transition cell
                    if(lod != 0) {
                        Vector3 min =  new Vector3(input.Size.x * x * 2, 
                                                   input.Size.y * y * 2, 
                                                   input.Size.z * z * 2);

                        GridCell debugTransitionCell = new GridCell();
                        debugTransitionCell.points = new Point[8];
                        for(int i = 0; i < 8; i++) {
                            debugTransitionCell.points[i].position = min + OFFSETS[i] * input.Size.x * 2f;
                        }
                        debugTransitionCells.Add(debugTransitionCell);

                        GridCell[] transitionCells = ProcessTransitionCell(lod, min, input.Size, input.Sample);
                        for(int i = 0; i < transitionCells.Length; i++) {
                            cells.Add(transitionCells[i]);
                            SE.Polyganiser.Polyganise(transitionCells[i], vertices, input.Isovalue);
                        }
                    }
                }
            }
        }


        int[] triangles = new int[vertices.Count];
        for(int i = 0; i < vertices.Count; i ++) {
            triangles[i] = i;
        }

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles;
        r.Mesh = mesh;
        r.Cells = cells;
        r.DebugTransitionCells = debugTransitionCells;

        return r;
    }

    public static GridCell[] ProcessTransitionCell(byte lod, Vector3 min, Vector3 size, UtilFuncs.Sampler Sample) {
        // only one face is a LOD face
        // -x, +x, -y, +y, -z, +z
        if(lod == 1 || lod == 2 || lod == 4 
            || lod == 8 || lod == 16 || lod == 32) {
            int shiftNumber = 0;

            for(int i = 0; i <= 6; i++) {
                if(lod >> i == 1) {
                    shiftNumber = i;
                    break;        
                }
            }


            // Step 1: Computer how the offsets need to be rotated
            Quaternion rotation = VectorAxisModifications[shiftNumber];
            GridCell[] cells = new GridCell[LOD2Offsets.GetLength(0)];
            //Debug.Log("Rotation: " + rotation);

            // Step 2: Generate GridCells based on rotated offsets
            for(int i = 0; i < LOD2Offsets.GetLength(0); i++) {
                cells[i] = new GridCell();
                cells[i].points = new Point[8];
                for(int j = 0; j < 8; j++) {
                    //Debug.Log("Offset: " + (LOD2Offsets[i, j] + new Vector3(1f, 1f, 1f)));
                    Vector3 rotatedOffset = (rotation * LOD2Offsets[i, j]) + new Vector3(1f, 1f, 1f);
                   // Debug.Log("Rotated Offset: " + rotatedOffset);

                    cells[i].points[j].position = new Vector3(min.x + size.x * rotatedOffset.x, 
                                                              min.y + size.y * rotatedOffset.y, 
                                                              min.z + size.z * rotatedOffset.z);
                    cells[i].points[j].density = Sample(cells[i].points[j].position.x, 
                                                        cells[i].points[j].position.y, 
                                                        cells[i].points[j].position.z);
                }
            }
            return cells;
        }

        return new GridCell[0];
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

    // total of 9 gridcells

    public readonly static Vector3[,] LOD2Offsets = {
        { // top left corner cell
            new Vector3(0f,0f,0f), new Vector3(1f,1f,1f), new Vector3(1f,1f,1f), new Vector3(0f,1f,0f), 
            new Vector3(0f,0f,1f), new Vector3(1f,1f,1f), new Vector3(1f,1f,1f), new Vector3(0f,1f,1f) 
        }, 
        { // bottom left corner cell
            new Vector3(0f,-1f,0f), new Vector3(1f,-1f,1f), new Vector3(1f,-1f,1f), new Vector3(0f,0f,0f), 
            new Vector3(0f,-1f,1f), new Vector3(1f,-1f,1f), new Vector3(1f,-1f,1f), new Vector3(0f,0f,1f) 
        }, 
        { // top right corner cell
            new Vector3(0f,0f,-1f), new Vector3(1f,1f,-1f), new Vector3(1f,1f,-1f), new Vector3(0f,1f,-1f), 
            new Vector3(0f,0f,0f), new Vector3(1f,1f,-1f), new Vector3(1f,1f,-1f), new Vector3(0f,1f,0f) 
        },
        { // bottom right corner cell
            new Vector3(0f,-1f,-1f), new Vector3(1f,-1f,-1f), new Vector3(1f,-1f,-1f), new Vector3(0f,0f,-1f), 
            new Vector3(0f,-1f,0f), new Vector3(1f,-1f,-1f), new Vector3(1f,-1f,-1f), new Vector3(0f,0f,0f) 
        },
        { // left edge cell
            new Vector3(0f,0f,1f), new Vector3(1f,-1f,1f), new Vector3(0f,0f,0f), new Vector3(0f,0f,0f), 
            new Vector3(0f,0f,1f), new Vector3(1f,-1f,1f), new Vector3(1f,1f,1f), new Vector3(0f,0f,1f) 
        },
        { // right edge cell
            new Vector3(0f,0f,0f), new Vector3(1f,-1f,-1f), new Vector3(0f,0f,-1f), new Vector3(0f,0f,-1f), 
            new Vector3(0f,0f,0f), new Vector3(1f,-1f,-1f), new Vector3(1f,1f,-1f), new Vector3(0f,0f,0f) 
        },
        { // bottom edge cell
            new Vector3(0f,-1f,0f), new Vector3(1f,-1f,-1f), new Vector3(1f,-1f,-1f), new Vector3(0f,0f,0f), 
            new Vector3(0f,-1f,0f), new Vector3(1f,-1f,1f), new Vector3(1f,-1f,1f), new Vector3(0f,0f,0f)	
        },
        { // top edge cell
            new Vector3(0f,0f,0f), new Vector3(1f,1f,-1f), new Vector3(1f,1f,-1f), new Vector3(0f,1f,0f), 
            new Vector3(0f,0f,0f), new Vector3(1f,1f,1f), new Vector3(1f,1f,1f), new Vector3(0f,1f,0f)	
        },
        { // middle cell
            new Vector3(0f,0f,0f), new Vector3(1f,-1f,-1f), new Vector3(1f,1f,-1f), new Vector3(0f,0f,0f), 
            new Vector3(0f,0f,0f), new Vector3(1f,-1f,1f), new Vector3(1f,1f,1f), new Vector3(0f,0f,0f) 
        }
    };


    // -x, +x, -y, y, -z, z
    public readonly static Quaternion[] VectorAxisModifications = {
        Quaternion.AngleAxis(180, Vector3.up), Quaternion.Euler(0, 0, 0),
        Quaternion.AngleAxis(-90, new Vector3(0, 0, 1)), Quaternion.AngleAxis(90, new Vector3(0, 0, 1)),
        Quaternion.AngleAxis(90, Vector3.up), Quaternion.AngleAxis(-90, Vector3.up),
    };
}
public class ExtractionResult {
     public UnityEngine.Mesh Mesh;
     public List<GridCell> Cells;
     public List<GridCell> DebugTransitionCells;
     public Vector3 Offset;
}
public class ExtractionInput {
    public UtilFuncs.Sampler Sample;
    public float Isovalue;
    
    public Vector3i Resolution;
    public Vector3 Size;
    // first six bits represent the sides of the chunk that are LOD transition sides 
    // -x, +x, -y, +y, -z, +z
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