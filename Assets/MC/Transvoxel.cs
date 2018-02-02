using System.Collections.Generic;
using UnityEngine;

namespace SE.Transvoxel {
public static class Transvoxel {
    public static Vector3Int[] tvCellVertexOffsets = {
		new Vector3Int(0,0,0), new Vector3Int(1,0,0), new Vector3Int(2,0,0), // High-res lower row
		new Vector3Int(0,1,0), new Vector3Int(1,1,0), new Vector3Int(2,1,0), // High-res middle row
		new Vector3Int(0,2,0), new Vector3Int(1,2,0), new Vector3Int(2,2,0), // High-res upper row

		new Vector3Int(0,0,2), new Vector3Int(2,0,2), // Low-res lower row
		new Vector3Int(0,2,2), new Vector3Int(2,2,2)  // Low-res upper row
    };

	public static int[,] tvCellEdges = {
		{0, 1}, {1, 2}, {3, 4}, {4, 5}, {6, 7}, {7, 8}, // Horizontal edges
		{0, 3}, {1, 4}, {2, 5}, {3, 6}, {4, 7}, {5, 8}, // Vertical edges
		{0, 9}, {2, 10}, {6, 11}, {8, 12}, // Connected edges
		{9, 10}, {11, 12}, {9, 11}, {10, 12} // Half-resolution face
	};

    public static Vector3Int[] regCellVertexOffsets = {
        new Vector3Int(0, 0, 0), new Vector3Int(2, 0, 0), new Vector3Int(0, 2, 0), new Vector3Int(2, 2, 0),
        new Vector3Int(0, 0, 2), new Vector3Int(2, 0, 2), new Vector3Int(0, 2, 2), new Vector3Int(2, 2, 2),
    };

	public static int[,] regCellEdges = {
		{0,4}, {1,5}, {2,6}, {3,7},	// x-axis 
		{0,2}, {1,3}, {4,6}, {5,7},	// y-axis
		{0,1}, {2,3}, {4,5}, {6,7}	// z-axis
	};

	public static Quaternion[] tvCellRotations = {
		Quaternion.AngleAxis(-90, Vector3.up), // -x
		Quaternion.AngleAxis(90, Vector3.up), // +x
		Quaternion.AngleAxis(90, new Vector3(1, 0, 0)), // -y
		Quaternion.AngleAxis(-90, new Vector3(1, 0, 0)), // +y
		Quaternion.AngleAxis(180, Vector3.up), // -z
		Quaternion.identity, // +z
	};

    //int[] order = {0, 1, 2, 5, 8, 7, 6, 3, 4};


    public static List<Edge> RegCellEdges = new List<Edge>();

    public static List<Edge> TVEdges = new List<Edge>();

    public static List<Vector3> StartingPoints = new List<Vector3>();

    public static List<Vector3> OffsetPoints = new List<Vector3>();

	public static sbyte[][][][] GenerateChunkData(Vector3 min, int resolution, UtilFuncs.Sampler sample) {
		int res1 = resolution + 1;

		sbyte[][][][] data = new sbyte[res1][][][];

		float f = 0.01f;
		float nx, ny, nz;

		for(int x = 0; x < res1; x++) {
			data[x] = new sbyte[res1][][];
			for(int y = 0; y < res1; y++) {
				data[x][y] = new sbyte[res1][];
				for(int z = 0; z < res1; z++) {
					data[x][y][z] = new sbyte[4];
					nx = (float)x - ((float)res1)/2f;
					ny = (float)y - ((float)res1)/2f;
					nz = (float)z - ((float)res1)/2f;

					data[x][y][z][0] = (sbyte)(Mathf.Clamp(-8f * sample(nx, ny, nz), -127, 127));

					float dx = sample(nx+f, ny, nz) - sample(nx-f, ny, nz);
					float dy = sample(nx, ny+f, nz) - sample(nx, ny-f, nz);
					float dz = sample(nx, ny, nz+f) - sample(nx, ny, nz-f);

					float total = (dx*dx) + (dy*dy) + (dz*dz);
					total = Mathf.Sqrt(total);
					dx /= total; dx *= 127;
					dy /= total; dy *= 127;
					dz /= total; dz *= 127;

					data[x][y][z][1] = (sbyte)dx;
					data[x][y][z][2] = (sbyte)dy;
					data[x][y][z][3] = (sbyte)dz;
				} 
			}
		}

		return data;
	}

	public static void GenerateChunk(Vector3 min, List<Vector3> vertices, List<int> triangles, int resolution, UtilFuncs.Sampler sample, byte lod) {
		sbyte[][][][] data = GenerateChunkData(min, resolution, sample);
		GenerateChunkExterior(vertices, triangles, data, lod, resolution);
		GenerateChunkInterior(vertices, triangles, data, resolution);
		//GenerateTestTransitionCells(vertices, triangles, data, resolution);
	}

    public static void GenerateTestTransitionCells(List<Vector3> vertices, List<int> triangles, sbyte[][][][] data, int res) {
        for(int x = 0; x < 8; x += 2) {
			GenerateTransitionCell(new Vector3Int(x, 0, 0), vertices, triangles, 0, data, tvCellRotations[x/2]);
        }
		for(int x = 0; x < 4; x += 2) {
			GenerateTransitionCell(new Vector3Int(x, 0, 4), vertices, triangles, 0, data, tvCellRotations[x/2 + 4]);
        }
    }

	public static void GenerateChunkExterior(List<Vector3> vertices, List<int> triangles, sbyte[][][][] data, byte chunkLod, int res) {
		int rm2 = res - 2;
		for(int x = 0; x < res; x += 2) {
			for(int y = 0;  y < res; y += 2) {
				for(int z = 0; z < res; z += 2) {
					bool isMinimal = x == 0 || y == 0 || z == 0;
					bool isMaximal = x == rm2 || y == rm2 || z == rm2;
					if(!isMinimal && !isMaximal) continue;

					byte cellLod = 0;
					if(x == 0) cellLod |= 1; if(x == rm2) cellLod |= 2;
					if(y == 0) cellLod |= 4; if(y == rm2) cellLod |= 8;
					if(z == 0) cellLod |= 16; if(z == rm2) cellLod |= 32;
					cellLod = (byte)(cellLod & chunkLod);

					
					int iPos = -1;
					int numOnes = 0;
					for(int i = 0; i < 6; i++) {
						if(((cellLod >> i) & 1) == 1) {
							numOnes++;
							iPos = i;
						}
					}

					if(numOnes == 1) {
						Debug.Log("iPos: " + iPos);
                		GenerateTransitionCell(new Vector3Int(x, y, z), vertices, triangles, 0, data, tvCellRotations[iPos]);
					}
					else {
						Util.GridCell cell = new Util.GridCell();
						cell.points = new Util.Point[8];
						for(int i = 0; i < 8; i++) {
							Vector3Int offset = (new Vector3Int((int)Tables.CellOffsets[i].x, (int)Tables.CellOffsets[i].y, (int)Tables.CellOffsets[i].z))*2 + new Vector3Int(x, y, z);
							sbyte density = data[(int)offset.x][offset.y][offset.z][0];
							cell.points[i].density = density;
							cell.points[i].position = offset;
						}
						GenerateRegularCell(cell, vertices, triangles, 0);
					}
				}
			}
		}
	}

	public static void GenerateChunkInterior(List<Vector3> vertices, List<int> triangles, sbyte[][][][] data, int res) {
		for(int x = 2; x < res - 2; x++) {
			for(int y = 2; y < res - 2; y++) {
				for(int z = 2; z < res - 2; z++) {
					Util.GridCell cell = new Util.GridCell();
					cell.points = new Util.Point[8];

					for(int i = 0; i < 8; i++) {
						Vector3Int offset = new Vector3Int((int)Tables.CellOffsets[i].x, (int)Tables.CellOffsets[i].y, (int)Tables.CellOffsets[i].z) + new Vector3Int(x, y, z);
						sbyte density = data[(int)offset.x][offset.y][offset.z][0];
						cell.points[i].density = density;
						cell.points[i].position = offset;
					}

					GenerateRegularCell(cell, vertices, triangles, 0);
				}
			}
		} 
	}

    private readonly static ushort[] sums = {0x0001, 0x0002, 0x0004, 0x0080, 0x0100, 0x0008, 0x0040, 0x0020, 0x0010 };

    //private readonly static int[] lerpedPoss = {0, 2, 3, 5};

    public static void GenerateTransitionCell(Vector3Int min, List<Vector3> Vertices, List<int> Triangles, byte lod, sbyte[][][][] data, Quaternion rot) {
		Debug.Log("Rot: " + rot);

        //int caseCode = 0;
        Vector3[] tvCellVertexPositions = new Vector3[13];
        Vector3[] regCellVertexPositions = new Vector3[8];
        sbyte[] tvDensities = new sbyte[13];
        sbyte[] regDensities = new sbyte[8];
        for(int i = 0; i < 10; i++) {
			Vector3 rotatedOffset = (rot * (tvCellVertexOffsets[i] - Vector3.one)) + Vector3.one;

            tvCellVertexPositions[i] = min + rotatedOffset;
            tvDensities[i] = data[(int)tvCellVertexPositions[i].x][(int)tvCellVertexPositions[i].y][(int)tvCellVertexPositions[i].z][0];
        }
        tvDensities[0x9] = tvDensities[0];
        tvDensities[0xA] = tvDensities[2];
        tvDensities[0xB] = tvDensities[6];
        tvDensities[0xC] = tvDensities[8];
        for(int i = 0; i < 8; i++) {
			Vector3 rotatedOffset = (rot * (regCellVertexOffsets[i] - Vector3.one)) + Vector3.one;

            regCellVertexPositions[i] = min + rotatedOffset;
            regDensities[i] = data[(int)regCellVertexPositions[i].x][(int)regCellVertexPositions[i].y][(int)regCellVertexPositions[i].z][0];
        }
        for(int i = 0; i < 4; i++) {
            Vector3 lerped = Lerp2(0.75f, regCellVertexPositions[i], regCellVertexPositions[i + 4]);

            //Debug.Log("A: " + regCellVertexPositions[i] + ", B: " + regCellVertexPositions[i + 4] + " lerped: " + lerped);

            tvCellVertexPositions[9 + i] = lerped;
            regCellVertexPositions[i] = lerped;
        }

        foreach(Vector3 pos_ in tvCellVertexPositions) {
            OffsetPoints.Add(pos_);
        }

        StartingPoints.Add(min);

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

        string densities = "";
        for(int i = 0; i < 9; i++) {
            densities += tvDensities[i] + ", ";
            if(tvDensities[i] < 0) caseCode += sums[i];
        }

		// Edge gizmos
		for(int i = 0; i < tvCellEdges.GetLength(0); i++) {
			Vector3 A = tvCellVertexPositions[tvCellEdges[i,0]];
			Vector3 B = tvCellVertexPositions[tvCellEdges[i,1]];

			Edge e = new Edge();
            e.A = A;
            e.B = B;

			TVEdges.Add(e);
		}

		for(int i = 0; i < regCellEdges.GetLength(0); i++) {
			Vector3 A = regCellVertexPositions[regCellEdges[i, 0]];
			Vector3 B = regCellVertexPositions[regCellEdges[i, 1]];

			Edge e = new Edge();
            e.A = A;
            e.B = B;
			RegCellEdges.Add(e);
		}

        string CellInfo = "Transvoxel Cell Information\n";
        

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


        //Debug.Log("#Edges: " + edgeData.Length);

        string TVCellVertices = " - Vertices: ";
        for(int i = 0; i < 13; i++) {
            TVCellVertices += tvCellVertexPositions[i] + ", ";
        }
        CellInfo += TVCellVertices + "\n";
        CellInfo += " - Min: " + min + "\n";
        CellInfo += " - Densities: " + densities + "\n";
        CellInfo += " - Case code: " + caseCode + "\n";
        CellInfo += " - #Verts: " + vertCount + "\n";
        CellInfo += " - #Tris: " + triCount + "\n";
        CellInfo += " - #Edges: " + edges.Length + "\n";

        string EdgeData = " - Edge Data: ";
        for(int i = 0; i < edges.Length; i++) {
            EdgeData += edgeData[i] + ", ";
        }

        CellInfo += EdgeData + "\n";

        string BinaryEdgeData = " - Binary Edge Data: ";
        for(int i = 0; i < edges.Length; i++) {
            BinaryEdgeData += System.Convert.ToString((int)edgeData[i], 2) + ", ";
        }

        CellInfo += BinaryEdgeData + "\n";


        string StringEdges = " - Edge List: \n";

        Vector3[] vertices = new Vector3[vertCount];


        // vertex generation phase
        for(int i = 0; i < edges.Length; i++) {
            lowNibbles[i] = (byte)(edgeData[i] & 0x00FF);
            //Debug.Log("lowNibbles[" + i + "]: " + lowNibbles[i]);


            edges[i] = new byte[2];

            edges[i][1] = (byte)(lowNibbles[i] & 0x0F);
            edges[i][0] = (byte)((lowNibbles[i] & 0xF0) >> 4);

            //Debug.Log("edges [" + i + "] vert [0]: " + edges[i][0] + ", vert [1]: " + edges[i][1]);

            Vector3 A = tvCellVertexPositions[edges[i][0]];
            Vector3 B = tvCellVertexPositions[edges[i][1]];

            Edge e = new Edge();
            e.A = A;
            e.B = B;

            sbyte density1 = tvDensities[edges[i][0]];
            sbyte density2 = tvDensities[edges[i][1]];
            //Debug.Log("A (" + density1 + "): " + A + ", B (" + density2 + "): " + B);

            Vector3 result = Lerp(density1, density2, A.x, A.y, A.z, B.x, B.y, B.z);

            e.IsoVertex = result;
            //Edges.Add(e);

            //Debug.Log("Lerped: " + result);

            StringEdges += "   - Edge " + i + "\n";
            StringEdges += "     - Low nibble: " + lowNibbles[i] + "\n";
            StringEdges += "     - Low nibble (Binary): " + System.Convert.ToString(lowNibbles[i], 2) + "\n";
            StringEdges += "     - Vertex A#: " + edges[i][0] + ", Vertex B#: " + edges[i][1] + "\n";
            StringEdges += "     - Vertex A# (Binary): " + System.Convert.ToString(edges[i][0], 2) + ", Vertex B# (Binary): " + System.Convert.ToString(edges[i][1], 2) + "\n";
            StringEdges += "     - Vertex A (d=" + density1 + "): " + A + ", Vertex B (d=" + density2 + "):" + B + "\n";
            StringEdges += "     - Lerped Vertex: " + result;

            StringEdges += "\n";

            vertices[i] = result;
        }

        StringEdges += "\n";
        CellInfo += StringEdges;

        //Debug.Log(CellInfo);

        byte[] indices = tCellData.Indices();

        int[] intIndices = new int[triCount * 3];

        string indicesStr = "";

        for(int i = 0; i < triCount * 3; i++) {
            intIndices[i] = indices[i];
            indicesStr += indices[i] + ", ";
        }

        if(inverse) {
            for(int i = 0; i < intIndices.Length; i += 3) {
                int a = intIndices[i];
                intIndices[i] = intIndices[i + 1];
                intIndices[i + 1] = a;
            }
        }

        //Debug.Log("Indices: " + indicesStr);

        int prevVertCount = Vertices.Count;

        for(int i = 0; i < intIndices.Length; i++) {
            Triangles.Add(prevVertCount + intIndices[i]);
        }

        Vertices.AddRange(vertices);
        //Triangles.AddRange(intIndices);

        // REGULAR CELL GENERATION
        string regCellString = "Regular Cell Information\n";

        caseCode =    ((regDensities[0] >> 7) & 0x01)
                        | ((regDensities[1] >> 6) & 0x02)
                        | ((regDensities[2] >> 5) & 0x04)
                        | ((regDensities[3] >> 4) & 0x08)
                        | ((regDensities[4] >> 3) & 0x10)
                        | ((regDensities[5] >> 2) & 0x20)
                        | ((regDensities[6] >> 1) & 0x40)
                        | (regDensities[7] & 0x80);

        if ((caseCode ^ ((regDensities[7] >> 7) & 0xff)) == 0) return;

        byte regClass = TVTables.RegularCellClass[caseCode];
        TVTables.RegularCell regCell = TVTables.RegularCellData[regClass];

        triCount = regCell.GetTriangleCount();
        vertCount = regCell.GetVertexCount();

        Vector3[] regVertices = new Vector3[12];

        regCellString += " - Case Code: " + caseCode + "\n";
        regCellString += " - Tri Count: " + triCount + "\n";
        regCellString += " - Vert Count: " + vertCount + "\n";

        string densitiesString = "";
        for(int i = 0; i < 8; i++) {
            densitiesString += regDensities[i] + ", ";
        }

        string verticesString = "";
        for(int i = 0; i < 8; i++) {
            verticesString += regCellVertexPositions[i] + ", ";
        }

        regCellString += " - Densities: " + densitiesString + "\n";
        regCellString += " - Vertices: " + verticesString + "\n";

        for(int i = 0; i < vertCount; i++) {
            edgeData = TVTables.RegularVertexData[caseCode];
            byte lowNibble = (byte)(edgeData[i] & 0x00FF);
            byte indexA = (byte)(lowNibble & 0x0F);
            byte indexB = (byte)((lowNibble & 0xF0) >> 4);

            //edges[i][1] = (byte)(lowNibbles[i] & 0x0F);
            //edges[i][0] = (byte)((lowNibbles[i] & 0xF0) >> 4);

            Vector3 A = regCellVertexPositions[indexA];
            Vector3 B = regCellVertexPositions[indexB];

            sbyte DensityA = regDensities[indexA];
            sbyte DensityB = regDensities[indexB];

            regVertices[i] = Lerp(DensityA, DensityB, A.x, A.y, A.z, B.x, B.y, B.z);

            regCellString += " - Edge " + i + "\n";
            regCellString += "   - Vertex Data (Binary): " + System.Convert.ToString((int)edgeData[i], 2) + "\n";
            regCellString += "   - Low Nibble: " + lowNibble + "\n";
            regCellString += "   - Low Nibble (Binary): " + System.Convert.ToString((int)lowNibble, 2) + "\n";
            regCellString += "   - Index A (Binary): " + System.Convert.ToString((int)indexA, 2) + ", Index B (Binary): " + System.Convert.ToString((int)indexB, 2) + "\n";
            regCellString += "   - Index A: " + indexA + ", Index B: " + indexB + "\n";
            regCellString += "   - A (" + DensityA + "): " + A + ", B (" + DensityB + "): " + B + "\n";
            regCellString += "   - Lerped: " + regVertices[i] + "\n";
        }
        //Debug.Log(regCellString);

        byte[] regCellIndices = regCell.Indices();
        intIndices = new int[triCount * 3];
        prevVertCount = Vertices.Count;

        for(int i = 0; i < triCount * 3; i++) {
            intIndices[i] = prevVertCount + regCellIndices[i];
        }

        Triangles.AddRange(intIndices);
        Vertices.AddRange(regVertices);

        Debug.Assert(vertCount <= 12);
    }

    public static void GenerateRegularCell(Util.GridCell cell, List<Vector3> vertices, List<int> triangles, float isovalue) {
        Vector3[] vertlist = new Vector3[12];

        int i,ntriang;
        int cubeindex;

        cubeindex = 0;
        if (cell.points[0].density < isovalue) cubeindex |= 1;
        if (cell.points[1].density < isovalue) cubeindex |= 2;
        if (cell.points[2].density < isovalue) cubeindex |= 4;
        if (cell.points[3].density < isovalue) cubeindex |= 8;
        if (cell.points[4].density < isovalue) cubeindex |= 16;
        if (cell.points[5].density < isovalue) cubeindex |= 32;
        if (cell.points[6].density < isovalue) cubeindex |= 64;
        if (cell.points[7].density < isovalue) cubeindex |= 128;

        /* Cube is entirely in/out of the surface */
        if (Tables.edgeTable[cubeindex] == 0) {
            return;
        }

        /* Find the vertices where the surface intersects the cube */
        if ((Tables.edgeTable[cubeindex] & 1) == 1)
            vertlist[0] = UtilFuncs.Lerp(isovalue,cell.points[0],cell.points[1]);
        if ((Tables.edgeTable[cubeindex] & 2) == 2)
            vertlist[1] = UtilFuncs.Lerp(isovalue,cell.points[1],cell.points[2]);
        if ((Tables.edgeTable[cubeindex] & 4) == 4)
            vertlist[2] = UtilFuncs.Lerp(isovalue,cell.points[2],cell.points[3]);
        if ((Tables.edgeTable[cubeindex] & 8) == 8)
            vertlist[3] = UtilFuncs.Lerp(isovalue,cell.points[3],cell.points[0]);
        if ((Tables.edgeTable[cubeindex] & 16) == 16)
            vertlist[4] = UtilFuncs.Lerp(isovalue,cell.points[4],cell.points[5]);
        if ((Tables.edgeTable[cubeindex] & 32) == 32)
            vertlist[5] = UtilFuncs.Lerp(isovalue,cell.points[5],cell.points[6]);
        if ((Tables.edgeTable[cubeindex] & 64) == 64)
            vertlist[6] = UtilFuncs.Lerp(isovalue,cell.points[6],cell.points[7]);
        if ((Tables.edgeTable[cubeindex] & 128) == 128)
            vertlist[7] = UtilFuncs.Lerp(isovalue,cell.points[7],cell.points[4]);
        if ((Tables.edgeTable[cubeindex] & 256) == 256)
            vertlist[8] = UtilFuncs.Lerp(isovalue,cell.points[0],cell.points[4]);
        if ((Tables.edgeTable[cubeindex] & 512) == 512)
            vertlist[9] = UtilFuncs.Lerp(isovalue,cell.points[1],cell.points[5]);
        if ((Tables.edgeTable[cubeindex] & 1024) == 1024)
            vertlist[10] = UtilFuncs.Lerp(isovalue,cell.points[2],cell.points[6]);
        if ((Tables.edgeTable[cubeindex] & 2048) == 2048)
            vertlist[11] = UtilFuncs.Lerp(isovalue,cell.points[3],cell.points[7]);

        /* Create the triangle */

        ntriang = 0;
        for (i=0;Tables.triTable[cubeindex][i]!=-1;i+=3) {
            ntriang++;
        }
        for (i = 0; Tables.triTable[cubeindex][i] !=-1; i++) {
			triangles.Add(vertices.Count);
            vertices.Add(vertlist[Tables.triTable[cubeindex][i]]);
        }
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

    public static Vector3 Lerp2(float mul, Vector3 A, Vector3 B) {
        return ((mul) * A) + ((1 - mul) * B);
    }

    public static void DrawGizmos() {
        Gizmos.color = new Color(255f/255f,99f/255f,71f/255f, 1f);
        foreach(Edge e in RegCellEdges) {
            UnityEngine.Gizmos.DrawLine(e.A, e.B);
        }
        Gizmos.color = Color.red;
        foreach(Edge e in TVEdges) {
            UnityEngine.Gizmos.DrawLine(e.A, e.B);
        }

        Gizmos.color = new Color(1, 0, 0, 0.07f);
        foreach(Edge e in TVEdges) {
            //UnityEngine.Gizmos.DrawSphere(e.IsoVertex, 0.2f);
        }

        Gizmos.color = Color.red;
        foreach(Vector3 point in StartingPoints) {
            //UnityEngine.Gizmos.DrawSphere(point, 0.3f);
        }

        Gizmos.color = Color.magenta;
        foreach(Vector3 point in OffsetPoints) {
            //UnityEngine.Gizmos.DrawSphere(point, 0.15f);
        }

    }

    public class Edge {
        public Vector3 A;
        public Vector3 B;
        public Vector3 IsoVertex;
    }

}
}