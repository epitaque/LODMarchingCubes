using UnityEngine;
using System.Collections.Generic;
using Util;

public static class SurfaceExtractor {
    public static ExtractionResult ExtractSurface(ExtractionInput input) {
        ExtractionResult r = new ExtractionResult();

        List<GridCell> cells = new List<GridCell>();
        List<GridCell> debugTransitionCells1S = new List<GridCell>();
        List<GridCell> debugTransitionCells2S = new List<GridCell>();

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

                        TransitionCellResult tCellRes = ProcessTransitionCell(lod, min, input.Size, input.Sample);
                        for(int i = 0; i < tCellRes.cells.Length; i++) {
                            cells.Add(tCellRes.cells[i]);
                            SE.Polyganiser.Polyganise(tCellRes.cells[i], vertices, input.Isovalue);
                        }

                        if(tCellRes.numLODFaces == 1) {
                            debugTransitionCells1S.Add(debugTransitionCell);
                        }
                        else if(tCellRes.numLODFaces == 2) {
                            debugTransitionCells2S.Add(debugTransitionCell);
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
        r.DebugTransitionCells1S = debugTransitionCells1S;
        r.DebugTransitionCells2S = debugTransitionCells2S;
        r.DebugTransitionCells3S = new List<GridCell>();

        return r;
    }

    public static TransitionCellResult ProcessTransitionCell(byte lod, Vector3 min, Vector3 size, UtilFuncs.Sampler Sample) {
        TransitionCellResult result = new TransitionCellResult();

        int numLODFaces = 0;
        for(int i = 0; i < 6; i++) {
            if(((lod >> i) & 1) == 1) numLODFaces++;
        }

        result.numLODFaces = numLODFaces;

        if(numLODFaces > 0 && numLODFaces < 3) {
            int numGridCells = LODOffsets[numLODFaces - 1].Length;

            GridCell[] cells = new GridCell[numGridCells];
            Quaternion rotation = LODRotations[lod];

            for(int i = 0; i < numGridCells; i++) {
                cells[i] = new GridCell();
                cells[i].points = new Point[8];
                for(int j = 0; j < 8; j++) {
                    Vector3 rotatedOffset = (rotation * LODOffsets[numLODFaces - 1][i][j]) + new Vector3(1f, 1f, 1f);

                    cells[i].points[j].position = new Vector3(min.x + size.x * rotatedOffset.x, 
                                                              min.y + size.y * rotatedOffset.y, 
                                                              min.z + size.z * rotatedOffset.z);
                    cells[i].points[j].density = Sample(cells[i].points[j].position.x, 
                                                        cells[i].points[j].position.y, 
                                                        cells[i].points[j].position.z);
                }
            }

            result.cells = cells;
            return result;
        } 

        return null;
    }

    // total of 9 gridcells - can be reduced
    // first dimension: number of sides with LOD transitions
    // second dimension: number of gridcells
    // third dimension: number of offsets
    public readonly static Vector3[][][] LODOffsets = new Vector3[][][] {
        new Vector3[][] {
                new Vector3[] { // top left corner cell
                    new Vector3(0f,0f,0f), new Vector3(1f,1f,1f), new Vector3(1f,1f,1f), new Vector3(0f,1f,0f), 
                    new Vector3(0f,0f,1f), new Vector3(1f,1f,1f), new Vector3(1f,1f,1f), new Vector3(0f,1f,1f) 
                }, 
                new Vector3[] { // bottom left corner cell
                    new Vector3(0f,-1f,0f), new Vector3(1f,-1f,1f), new Vector3(1f,-1f,1f), new Vector3(0f,0f,0f), 
                    new Vector3(0f,-1f,1f), new Vector3(1f,-1f,1f), new Vector3(1f,-1f,1f), new Vector3(0f,0f,1f) 
                }, 
                new Vector3[] { // top right corner cell
                    new Vector3(0f,0f,-1f), new Vector3(1f,1f,-1f), new Vector3(1f,1f,-1f), new Vector3(0f,1f,-1f), 
                    new Vector3(0f,0f,0f), new Vector3(1f,1f,-1f), new Vector3(1f,1f,-1f), new Vector3(0f,1f,0f) 
                },
                new Vector3[] { // bottom right corner cell
                    new Vector3(0f,-1f,-1f), new Vector3(1f,-1f,-1f), new Vector3(1f,-1f,-1f), new Vector3(0f,0f,-1f), 
                    new Vector3(0f,-1f,0f), new Vector3(1f,-1f,-1f), new Vector3(1f,-1f,-1f), new Vector3(0f,0f,0f) 
                },
                new Vector3[] { // left edge cell
                    new Vector3(0f,0f,1f), new Vector3(1f,-1f,1f), new Vector3(0f,0f,0f), new Vector3(0f,0f,0f), 
                    new Vector3(0f,0f,1f), new Vector3(1f,-1f,1f), new Vector3(1f,1f,1f), new Vector3(0f,0f,1f) 
                },
                new Vector3[] { // right edge cell
                    new Vector3(0f,0f,0f), new Vector3(1f,-1f,-1f), new Vector3(0f,0f,-1f), new Vector3(0f,0f,-1f), 
                    new Vector3(0f,0f,0f), new Vector3(1f,-1f,-1f), new Vector3(1f,1f,-1f), new Vector3(0f,0f,0f) 
                },
                new Vector3[] { // bottom edge cell
                    new Vector3(0f,-1f,0f), new Vector3(1f,-1f,-1f), new Vector3(1f,-1f,-1f), new Vector3(0f,0f,0f), 
                    new Vector3(0f,-1f,0f), new Vector3(1f,-1f,1f), new Vector3(1f,-1f,1f), new Vector3(0f,0f,0f)	
                },
                new Vector3[] { // top edge cell
                    new Vector3(0f,0f,0f), new Vector3(1f,1f,-1f), new Vector3(1f,1f,-1f), new Vector3(0f,1f,0f), 
                    new Vector3(0f,0f,0f), new Vector3(1f,1f,1f), new Vector3(1f,1f,1f), new Vector3(0f,1f,0f)	
                },
                new Vector3[] { // middle cell
                    new Vector3(0f,0f,0f), new Vector3(1f,-1f,-1f), new Vector3(1f,1f,-1f), new Vector3(0f,0f,0f), 
                    new Vector3(0f,0f,0f), new Vector3(1f,-1f,1f), new Vector3(1f,1f,1f), new Vector3(0f,0f,0f)
                }
        },

        new Vector3[][] {
            new Vector3[] {
                new Vector3(-1f, 0f, 0f), new Vector3(0f, 0f, 0f), new Vector3(1f, 1f, -1f), new Vector3(-1f, 1f, -1f),
                new Vector3(-1f, 0f, 0f), new Vector3(0f, 0f, 0f), new Vector3(1f, 1f, 1f), new Vector3(-1f, 1f, 1f)
            },
            new Vector3[] {
                new Vector3(-1f, 0f, -1f), new Vector3(0f, 0f, -1f), new Vector3(1f, 1f, -1f), new Vector3(-1f, 1f, -1f),
                new Vector3(-1f, 0f, 0f), new Vector3(0f, 0f, 0f), new Vector3(0f, 0f, 0f), new Vector3(-1f, 0f, 0f)
            },
            new Vector3[] {
                new Vector3(-1f, 0f, 0f), new Vector3(0f, 0f, 0f), new Vector3(0f, 0f, 0f), new Vector3(-1f, 0f, 0f),
                new Vector3(-1f, 0f, 1f), new Vector3(0f, 0f, 1f), new Vector3(1f, 1f, 1f), new Vector3(-1f, 1f, 1f)
            },
            new Vector3[] {
                new Vector3(0f, -1f, 0f), new Vector3(1f, -1f, -1f), new Vector3(1f, 1f, -1f), new Vector3(0f, 0f, 0f),
                new Vector3(0f, -1f, 0f), new Vector3(1f, -1f, 1f), new Vector3(1f, 1f, 1f), new Vector3(0f, 0f, 0f)
            },
            new Vector3[] {
                new Vector3(0f, -1f, -1f), new Vector3(1f, -1f, -1f), new Vector3(1f, 1f, -1f), new Vector3(0f, 0f, -1f),
                new Vector3(0f, -1f, 0f), new Vector3(0f, -1f, 0f), new Vector3(0f, 0f, 0f), new Vector3(0f, 0f, 0f)
            },
            new Vector3[] {
                new Vector3(0f, -1f, 0f), new Vector3(0f, -1f, 0f), new Vector3(0f, 0f, 0f), new Vector3(0f, 0f, 0f),
                new Vector3(0f, -1f, 1f), new Vector3(1f, -1f, 1f), new Vector3(1f, 1f, 1f), new Vector3(0f, 0f, 1f)
            }
        },

        new Vector3[][] { // 3 sides
            new Vector3[] {
                new Vector3(-1f, -1f, -1f), new Vector3(1f, -1f, -1f), new Vector3(1f, 1f, -1f), new Vector3(-1f, 1f, -1f),
                new Vector3(-1f, -1f, 0f), new Vector3(0f, -1f, 0f), new Vector3(1f, 1f, 1f), new Vector3(0f, 0f, 0f)
            },
            new Vector3[] {
                new Vector3(0f, -1f, -1f), new Vector3(1f, -1f, -1f), new Vector3(1f, 1f, -1f), new Vector3(0f, 0f, -1f),
                new Vector3(0f, -1f, 0f), new Vector3(0f, -1f, 0f), new Vector3(0f, 0f, 0f), new Vector3(0f, 0f, 0f)
            },
            new Vector3[] {
                new Vector3(0f, -1f, 0f), new Vector3(0f, -1f, 0f), new Vector3(0f, 0f, 0f), new Vector3(0f, 0f, 0f),
                new Vector3(0f, -1f, 1f), new Vector3(1f, -1f, 1f), new Vector3(1f, 1f, 1f), new Vector3(0f, 0f, 1f)
            }
        }
    };

    public readonly static Dictionary<byte, Quaternion> LODRotations =
        new Dictionary<byte, Quaternion>() { 
            // Single sided LOD
            // -x, +x, -y, y, -z, z
            {1, Quaternion.AngleAxis(180, Vector3.up)}, 
            {2, Quaternion.identity},
            {4, Quaternion.AngleAxis(-90, new Vector3(0, 0, 1))}, 
            {8, Quaternion.AngleAxis(90, new Vector3(0, 0, 1))},
            {16, Quaternion.AngleAxis(90, Vector3.up)},
            {32, Quaternion.AngleAxis(-90, Vector3.up)},
        
            // Double sided LOD
            // +x+y, -x+y, -x-y, +x-y
            {2 + 8, Quaternion.identity}, 
            {1 + 8, Quaternion.AngleAxis(180, Vector3.up)}, 
            {1 + 4, Quaternion.AngleAxis(90, new Vector3(0, 0, 1))}, 
            {2 + 4, Quaternion.AngleAxis(90, new Vector3(0, 0, 1))},
            // +z+y, -z+y, -z-y, +z-y
            {32 + 8, Quaternion.AngleAxis(-90, Vector3.up)}, 
            {16 + 8, Quaternion.AngleAxis(90, Vector3.up)}, 
            {16 + 4, Quaternion.AngleAxis(90, Vector3.up) * Quaternion.AngleAxis(90, new Vector3(1, 0, 0))}, 
            {32 + 4, Quaternion.AngleAxis(-90, Vector3.up) * Quaternion.AngleAxis(90, new Vector3(1, 0, 0))},
            // +z+x, -z+x, -z-x, +z-x
            {32 + 2, Quaternion.identity},
            {16 + 2, Quaternion.identity},
            {16 + 1, Quaternion.identity},
            {32 + 1, Quaternion.identity}
    };
}
public class TransitionCellResult {
    public int numLODFaces;
    public GridCell[] cells;
}

public class ExtractionResult {
     public UnityEngine.Mesh Mesh;
     public List<GridCell> Cells;
     public List<GridCell> DebugTransitionCells1S;
     public List<GridCell> DebugTransitionCells2S;
     public List<GridCell> DebugTransitionCells3S;
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
		
        3-------6------2
       /.             /|
      10.           11 |
     /  0           /  2
    /   .          /   |     ^ Y
   7-------7------6    |     |
   |    0 . . 4 . |. . 1     --> X
   |   .          |   /		 \/ +Z
   1  8           3  9
   | .            | /
   |.             |/
   4-------5------5
*/

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