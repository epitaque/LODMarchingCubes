using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using SE;

public static class LookupTableCreator
{
    public static readonly int HIGHEST_LOD_INDEX = 43;

    public static void GenerateLookupTable()
    {
        GenerateOffsetLookupTable();
        //GenerateUniqueEdgesLookupTable();
        //GenerateMCLodEdgeMappingTable();
        //GenerateMCLodEdgeToReIDTable(true);
        //GenerateUniqueEdgesLookupTableReuse();
    }

    public static string GenerateMCLodEdgeToReIDTable(bool copy = false)
    {
        byte[][,] eToReTable = new byte[HIGHEST_LOD_INDEX][,];
        string table = "public static byte[][,] MCLodEdgeToReID = new byte[][,] {\n";

        for (byte lod = 0; lod < HIGHEST_LOD_INDEX; lod++)
        {

            List<System.Tuple<byte, byte>> maximalEdgeIDToStandardEdgeID = new List<System.Tuple<byte, byte>>();

            for (int edgeNum = 0; edgeNum < Tables.MCLodUniqueEdges[lod].Length; edgeNum++)
            {
                byte bA = Tables.MCLodUniqueEdges[lod][edgeNum][0];
                byte bB = Tables.MCLodUniqueEdges[lod][edgeNum][1];

                Vector3 A = ByteToVector3(bA);
                Vector3 B = ByteToVector3(bB);

                if (IsMaximalEdge2(A, B))
                {
                    byte edgeID = (byte)edgeNum;
                    byte standardID = (byte)GetStandardID(A, B);
                    if (standardID != byte.MaxValue)
                    {
                        System.Tuple<byte, byte> pair = new System.Tuple<byte, byte>(edgeID, standardID);
                        maximalEdgeIDToStandardEdgeID.Add(pair);
                    }
                }
            }

            eToReTable[lod] = new byte[maximalEdgeIDToStandardEdgeID.Count, 2];

            table += "	new byte[,] {\n";
            for (int i = 0; i < maximalEdgeIDToStandardEdgeID.Count; i++)
            {
                eToReTable[lod][i, 0] = maximalEdgeIDToStandardEdgeID[i].Item1;
                eToReTable[lod][i, 1] = maximalEdgeIDToStandardEdgeID[i].Item2;

                table += "		{" + eToReTable[lod][i, 0] + ", " + eToReTable[lod][i, 1] + "}";

                if (i != maximalEdgeIDToStandardEdgeID.Count - 1)
                {
                    table += ",";
                }

                table += "\n";
            }

            table += "	}";
            if (lod != HIGHEST_LOD_INDEX - 1)
            {
                table += ",";
            }
            table += "\n";
        }
        table += "};";

        //if (copy) UnityEditor.EditorGUIUtility.systemCopyBuffer = table;

        return table;

    }

    public static byte GetStandardID(Vector3 A, Vector3 B)
    {
        if (A == B)
        {
            return byte.MaxValue;
        }
        for (byte i = 0; i < Tables.MCLodMaximalEdges.GetLength(0); i++)
        {
            int ax = Tables.MCLodMaximalEdges[i, 0, 0];
            int ay = Tables.MCLodMaximalEdges[i, 0, 1];
            int az = Tables.MCLodMaximalEdges[i, 0, 2];

            int bx = Tables.MCLodMaximalEdges[i, 1, 0];
            int by = Tables.MCLodMaximalEdges[i, 1, 1];
            int bz = Tables.MCLodMaximalEdges[i, 1, 2];

            Vector3 seA = new Vector3(ax, ay, az);
            Vector3 seB = new Vector3(bx, by, bz);
            if (A == seA && B == seB || (A == seB && B == seA))
            {
                return i;
            }
        }
        Debug.LogError("Error finding standard ID for vectors " + A + " and " + B);
        Debug.Assert(false);
        return byte.MaxValue;
    }

    public static string GenerateMCLodEdgeMappingTable(bool copy = false)
    {
        byte[][,] edgeMapTable = new byte[HIGHEST_LOD_INDEX][,];

        string table = "public static byte[][,] MCLodEdgeMappingTable = new byte[][,] {\n";

        for (byte lod = 0; lod < HIGHEST_LOD_INDEX; lod++)
        {
            byte[][] gridCells = Tables.MCLodTable[lod];
            byte[,] edgeMap = new byte[Tables.MCLodTable[lod].Length, 12];

            table += "	new byte[" + edgeMap.GetLength(0) + "," + edgeMap.GetLength(1) + "] { // lod " + lod + " (" + System.Convert.ToString(lod, 2) + ")\n";

            for (int gridCellNum = 0; gridCellNum < gridCells.Length; gridCellNum++)
            {
                table += "		{";
                for (int edgeNum = 0; edgeNum < 12; edgeNum++)
                {
                    byte[] edge = new byte[2];
                    edge[0] = gridCells[gridCellNum][Tables.edgePairs[edgeNum, 0]];
                    edge[1] = gridCells[gridCellNum][Tables.edgePairs[edgeNum, 1]];

                    edgeMap[gridCellNum, edgeNum] = FindEdgeID(lod, edge);

                    table += edgeMap[gridCellNum, edgeNum];
                    if (edgeNum != 11)
                    {
                        table += ",";
                    }
                    table += " ";
                }
                table += "}";
                if (gridCellNum != gridCells.Length - 1)
                {
                    table += ",";
                }
                table += "\n";
            }

            table += "	}";
            if (lod != HIGHEST_LOD_INDEX - 1)
            {
                table += ",";
            }
            table += "\n";
        }
        table += "};";

        UnityEngine.Debug.Log("Edgemap Table: \n" + table);

        //if (copy) UnityEditor.EditorGUIUtility.systemCopyBuffer = table;

        return table;
    }

    public static byte FindEdgeID(byte lod, byte[] edge)
    {
        byte[][] uniqueEdges = Tables.MCLodUniqueEdges[lod];
        for (byte uniqueEdgeNum = 0; uniqueEdgeNum < uniqueEdges.Length; uniqueEdgeNum++)
        {
            byte[] uniqueEdge = uniqueEdges[uniqueEdgeNum];
            if (IsEdgeEqual(uniqueEdge, edge))
            {
                return uniqueEdgeNum;
            }
        }
        UnityEngine.Debug.Assert(false);
        return byte.MaxValue;
    }

    public static void GenerateUniqueEdgesLookupTable()
    {
        byte[][][] uniqueEdgesTable = new byte[HIGHEST_LOD_INDEX][][];
        string table = "public static byte[][][] MCLodUniqueEdges = new byte[][][] {\n";

        for (int lod = 0; lod < HIGHEST_LOD_INDEX; lod++)
        {
            byte[][] gridCellOffsets = SE.Tables.MCLodTable[lod];

            table += "	new byte[][] { // lod " + lod + " (" + System.Convert.ToString(lod, 2) + ")\n";

            //Dictionary<byte, byte> UniqueEdges = new Dictionary<byte, byte>();
            List<System.Tuple<byte, byte>> uniqueEdges = new List<System.Tuple<byte, byte>>();

            foreach (byte[] gridCellOffs in gridCellOffsets)
            {
                for (int j = 0; j < Tables.edgePairs.GetLength(0); j++)
                {
                    byte bA = gridCellOffs[Tables.edgePairs[j, 0]];
                    byte bB = gridCellOffs[Tables.edgePairs[j, 1]];

                    Vector3 A = ByteToVector3(bA);
                    Vector3 B = ByteToVector3(bB);

                    System.Tuple<byte, byte> currentEdge = new System.Tuple<byte, byte>(bA, bB);

                    bool unique = true;
                    foreach (System.Tuple<byte, byte> edge in uniqueEdges)
                    {
                        if (IsEdgeEqual(currentEdge, edge)) { unique = false; break; }
                    }

                    if (unique)
                    {
                        uniqueEdges.Add(currentEdge);
                    }

                    int nMaxes = 0;
                    if (A.x == 1 || A.y == 1 || A.z == 1) nMaxes++;
                    if (B.x == 1 || B.y == 1 || B.z == 1) nMaxes++;


                }
            }

            uniqueEdgesTable[lod] = new byte[uniqueEdges.Count][];

            int n = 0;
            foreach (System.Tuple<byte, byte> e in uniqueEdges)
            {
                if (n == 0)
                {
                    table += "		";
                }
                byte[] pair = new byte[2];
                pair[0] = e.Item1;
                pair[1] = e.Item2;
                table += "new byte[] { " + pair[0] + ", " + pair[1] + " }";
                if (n != uniqueEdges.Count - 1)
                {
                    table += ", ";
                    if (n % 4 == 3)
                    {
                        table += "\n		";
                    }
                }
                else
                {
                    table += "\n";
                }


                uniqueEdgesTable[lod][n] = pair;
                n++;
            }
            table += "	}";

            if (lod != 63)
            {
                table += ", ";
            }
            table += "\n";
        }
        table += "};";


        Debug.Log("Unique Edges Lookup Table: \n" + table);
    }

    public static void GenerateUniqueEdgesLookupTableReuse(bool copy = false)
    {
        byte[][][] uniqueEdgesTable = new byte[HIGHEST_LOD_INDEX][][];
        string table = "public static int[][] MCLodUniqueEdgesReuse = new int[][] {\n";

        for (int lod = 0; lod < HIGHEST_LOD_INDEX; lod++)
        {
            byte[][] gridCellOffsets = SE.Tables.MCLodTable[lod];

            table += "	new int[] { // lod " + lod + " (" + System.Convert.ToString(lod, 2) + ")\n";

            int n = 0;
            for (int edgeNum = 0; edgeNum < Tables.MCLodUniqueEdges[lod].Length; edgeNum++)
            {
                byte[] edgePoints = Tables.MCLodUniqueEdges[lod][edgeNum];

                byte bA = edgePoints[0];
                byte bB = edgePoints[1];

                if (bA == bB) continue;
                n++;

                if (edgeNum == 0)
                {
                    table += "		";
                }
                else if (edgeNum % 8 == 0 && edgeNum != Tables.MCLodUniqueEdges[lod].Length - 1)
                {
                    table += "\n		";
                }

                Vector3 A = ByteToVector3(bA);
                Vector3 B = ByteToVector3(bB);

                Vector3 AShifted = A;
                Vector3 BShifted = B;

                Vector3 AltAShifted = A;
                Vector3 AltBShifted = B;

                byte EdgeA = bA;
                byte EdgeB = bB;
                byte ReuseCell = 0;
                byte ReuseIndex = 0;
                byte AlternateReuseIndex = 0;

                bool AltCellExists = false;

                if (IsMinimalEdge(A, B))
                {
                    bool XMinimalSide = A.x == -1 && B.x == -1;
                    bool YMinimalSide = A.y == -1 && B.y == -1;
                    bool ZMinimalSide = A.z == -1 && B.z == -1;

                    bool CellOnX = (lod & 1) != 1;
                    bool CellOnY = (lod & 4) != 4;
                    bool CellOnZ = (lod & 16) != 16;

                    if (XMinimalSide && CellOnX)
                    {
                        ReuseCell |= 1;
                        AShifted.x += 2;
                        BShifted.x += 2;

                        if (ZMinimalSide && CellOnZ)
                        {
                            ReuseCell |= 2;
                            AltAShifted.z += 2;
                            AltBShifted.z += 2;
                            AltCellExists = true;
                        }
                        if (YMinimalSide && CellOnY)
                        {
                            ReuseCell |= 4;
                            AltAShifted.y += 2;
                            AltBShifted.y += 2;
                            AltCellExists = true;
                        }
                    }
                    else if (ZMinimalSide && CellOnZ)
                    {
                        ReuseCell |= 2;
                        AShifted.z += 2;
                        BShifted.z += 2;

                        if (YMinimalSide && CellOnY)
                        {
                            ReuseCell |= 4;
                            AltAShifted.y += 2;
                            AltBShifted.y += 2;
                            AltCellExists = true;
                        }
                    }
                    else if (YMinimalSide && CellOnY)
                    {
                        ReuseCell |= 4;
                        AShifted.y += 2;
                        BShifted.y += 2;
                    }

                    if (ReuseCell != 0)
                    {
                        // try to find index of reused edge
                        byte id = GetStandardID(AShifted, BShifted);

                        if (id == byte.MaxValue)
                        {
                            Debug.LogError("Error creating UniqueEdgesLookupTableReuse: unable to find a reuse index. Edge A: " + A + "(shifted: " + AShifted + "), B: " + B + " (shifted: " + BShifted + ")");
                            Debug.Assert(false);
                        }
                        else
                        {
                            ReuseIndex = id;
                        }
                    }
                    if (AltCellExists)
                    {
                        byte id = GetStandardID(AltAShifted, AltBShifted);

                        if (id == byte.MaxValue)
                        {
                            Debug.LogError("Error creating UniqueEdgesLookupTableReuse: unable to find a reuse index for alternate cell. Edge A: " + A + "(shifted: " + AShifted + "), B: " + B + " (shifted: " + BShifted + ")");
                            Debug.Assert(false);
                        }
                        else
                        {
                            AlternateReuseIndex = id;
                        }

                    }
                }

				int finalEdge = ((int)bA) | ((int)bB << 6) | ((int)ReuseCell << 12) | ((int)ReuseIndex << 16) | ((int)AlternateReuseIndex << 22);

				if(bA > 63 || bB > 63) {
					Debug.Assert(false);
				}
				if(ReuseCell > 7) {
					Debug.Assert(false);
				}
				if(ReuseIndex > 63 || AlternateReuseIndex > 63) {
					Debug.Assert(false);
				}

                table += "0x" + finalEdge.ToString("X8");

                if (edgeNum != Tables.MCLodUniqueEdges[lod].Length - 1)
                {
                    table += ", ";
                }

                //table += " ";
            }


            table += "\n	}";

            if (lod != HIGHEST_LOD_INDEX)
            {
                table += ", ";
            }
            table += "\n";
        }
        table += "};";

        Debug.Log("Unique Edges Lookup Table Reuse: \n" + table);

        //if (copy) UnityEditor.EditorGUIUtility.systemCopyBuffer = table;
    }

    /*public static bool IsNotReusableEdge(byte A, byte B, byte lod) {
		bool XMinimalSide = false;
		if()
	}*/

    public static bool IsMinimalEdge(Vector3 A, Vector3 B)
    {
        return (A.x == -1 || A.y == -1 || A.z == -1) && (B.x == -1 || B.y == -1 || B.z == -1);
    }

    public static bool IsMaximalEdge(Vector3 A, Vector3 B)
    {
        return (A.x == 1 || A.y == 1 || A.z == 1) && (B.x == 1 || B.y == 1 || B.z == 1);
    }

    public static bool IsMaximalEdge2(Vector3 A, Vector3 B) {
        for(int edgeNum = 0; edgeNum < Tables.MCLodMaximalEdges.GetLength(0); edgeNum++) {
            Vector3 mA = new Vector3(Tables.MCLodMaximalEdges[edgeNum,0,0], Tables.MCLodMaximalEdges[edgeNum,0,1], Tables.MCLodMaximalEdges[edgeNum,0,2]);
            Vector3 mB = new Vector3(Tables.MCLodMaximalEdges[edgeNum,1,0], Tables.MCLodMaximalEdges[edgeNum,1,1], Tables.MCLodMaximalEdges[edgeNum,1,2]);
            if((A == mA && B == mB) || (A == mB && B == mA)) return true;
        }
        return false;
    }

    private static bool IsEdgeEqual(System.Tuple<byte, byte> e1, System.Tuple<byte, byte> e2)
    {
        return (e1.Item1 == e2.Item1 && e1.Item2 == e2.Item2) || (e1.Item2 == e2.Item1 && e1.Item1 == e2.Item2);
    }

    private static bool IsEdgeEqual(byte[] e1, byte[] e2)
    {
        return (e1[0] == e2[0] && e1[1] == e2[1]) || (e1[1] == e2[0] && e1[0] == e2[1]);
    }

    private static Vector3 ByteToVector3(byte vec3)
    {
        Vector3 pos = Vector3.zero;
        byte b = vec3;

        if ((b & 1) == 1) pos.x -= 1;
        if ((b & 2) == 2) pos.x += 1;
        if ((b & 4) == 4) pos.y -= 1;
        if ((b & 8) == 8) pos.y += 1;
        if ((b & 16) == 16) pos.z -= 1;
        if ((b & 32) == 32) pos.z += 1;

        return pos;
    }

    public static string GenerateOffsetLookupTable()
    {
        //addLodOffsets();

        string table = "public static byte[][][] MCLodTable = new byte[][][] {\n";

        byte[][][] offsetTable = new byte[HIGHEST_LOD_INDEX][][];

        for (byte lod = 0; lod < HIGHEST_LOD_INDEX; lod++)
        {
            Vector3[][] offsets = GetOffsetsForLod(lod);
            byte[][] bOffsets = ConvertToByteOffsets(offsets);
            offsetTable[lod] = bOffsets;

            table += "	new byte[][] { // lod " + lod + " (" + System.Convert.ToString(lod, 2) + ")\n";

            for (int i = 0; i < bOffsets.Length; i++)
            {
                table += "		new byte[] { ";
                for (int j = 0; j < bOffsets[0].Length; j++)
                {
                    table += bOffsets[i][j];
                    if (j != bOffsets[0].Length - 1)
                    {
                        table += ", ";
                    }
                }
                table += " }";
                if (i != bOffsets.Length - 1)
                {
                    table += ",";
                }
                table += "\n";

            }

            table += "	}";

            if (lod != 63)
            {
                table += ", ";
            }
            table += "\n";
        }
        table += "};";

        Debug.Log("Offset Lookup Table: \n" + table);

        SE.Tables.MCLodTable = offsetTable;

        return table;
    }

    public static Vector3[][] GetOffsetsForLod(byte lod)
    {
        int numLODFaces = 0;
        for (int i = 0; i < 6; i++)
        {
            if (((lod >> i) & 1) == 1) numLODFaces++;
        }

        if (numLODFaces == 0)
        {
            Vector3[][] offsets = LODOffsets[lod];

			if(lod == 0) {
				Debug.Log("Lod 0, returning offsets array with length: " + offsets.Length);
			}

            return offsets;
        }
        else if (!LODRotations.ContainsKey(lod))
        {
            return new Vector3[][] { };
        }
        else
        {
            int numGridCells = LODOffsets[numLODFaces].Length;

            Vector3[][] offsets = new Vector3[numGridCells][];
            //GridCell[] cells = new GridCell[numGridCells];
            Quaternion rotation = LODRotations[lod];

            for (int i = 0; i < numGridCells; i++)
            {
                string stroffsets = "Lod offsets for gridcell " + i + ": ";
                offsets[i] = new Vector3[8];
                for (int j = 0; j < 8; j++)
                {
                    Vector3 rotatedOffset = (rotation * LODOffsets[numLODFaces][i][j]);
                    stroffsets += rotatedOffset + ", ";
                    offsets[i][j] = rotatedOffset;
                }
            }
			/*for (int i = 0; i < numGridCells; i++) {
                offsets[i] = new Vector3[8];
                for (int j = 0; j < 8; j++)
                {
                    Vector3 rotatedOffset = (rotation * LODOffsets[numLODFaces][i][j]);
                    offsets[i][j] = rotatedOffset;
                }

			}*/

            return offsets;
        }
    }

    public static byte[][] ConvertToByteOffsets(Vector3[][] offsets)
    {
        byte[][] bOffsets = new byte[offsets.Length][];


        for (int i = 0; i < offsets.Length; i++)
        {
            string byteOffsetStr = "ByteOffsets: ";

            bOffsets[i] = new byte[8];
            for (int j = 0; j < 8; j++)
            {

                byte bOff = 0;
                if (Mathf.Round(offsets[i][j].x) == -1) bOff |= 1;
                if (Mathf.Round(offsets[i][j].x) == 1) bOff |= 2;
                if (Mathf.Round(offsets[i][j].y) == -1) bOff |= 4;
                if (Mathf.Round(offsets[i][j].y) == 1) bOff |= 8;
                if (Mathf.Round(offsets[i][j].z) == -1) bOff |= 16;
                if (Mathf.Round(offsets[i][j].z) == 1) bOff |= 32;
                bOffsets[i][j] = bOff;
                byteOffsetStr += "[reg: " + offsets[i][j] + " , byte: " + bOff + "], ";
            }
            //Debug.Log(byteOffsetStr);
        }

        return bOffsets;
    }

    public static void addLodOffsets()
    {
        Vector3[][] regCellOffsets = new Vector3[8][];
        for (int i = 0; i < 8; i++)
        {
            regCellOffsets[i] = new Vector3[8];
            for (int j = 0; j < 8; j++)
            {
                regCellOffsets[i][j] = SE.Tables.CellOffsets[i] + SE.Tables.CellOffsets[j] - Vector3.one;
            }
        }
        LODOffsets[0] = regCellOffsets;
    }

    // total of 9 gridcells - can be reduced
    // first dimension: number of sides with LOD transitions
    // second dimension: number of gridcells
    // third dimension: number of offsets
    public readonly static Vector3[][][] LODOffsets = new Vector3[][][] {
        new Vector3[][] {
        },

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
                new Vector3(-1f, -1f, 0f), new Vector3(0f, -1f, 0f), new Vector3(0f, 0f, 0f), new Vector3(-1f, 0f, 0f),
                new Vector3(-1f, -1f, 1f), new Vector3(1f, -1f, 1f), new Vector3(1f, 1f, 1f), new Vector3(-1f, 1f, 1f),
            },
           new Vector3[] {
                new Vector3(-1f, 0f, -1f), new Vector3(0f, 0f, -1f), new Vector3(1f, 1f, -1f), new Vector3(-1f, 1f, -1f),
                new Vector3(-1f, 0f, 0f), new Vector3(0f, 0f, 0f), new Vector3(1f, 1f, 1f), new Vector3(-1f, 1f, 1f),
            },
            new Vector3[] {
                new Vector3(0f, -1f, -1f), new Vector3(1f, -1f, -1f), new Vector3(1f, 1f, -1f), new Vector3(0f, 0f, -1f),
                new Vector3(0f, -1f, 0f), new Vector3(1f, -1f, 1f), new Vector3(1f, 1f, 1f), new Vector3(0f, 0f, 0f),
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
            {1 + 4, Quaternion.AngleAxis(180, new Vector3(0, 0, 1))},
            {2 + 4, Quaternion.AngleAxis(-90, new Vector3(0, 0, 1))},
			// +z+y, -z+y, -z-y, +z-y
			{32 + 8, Quaternion.AngleAxis(-90, Vector3.up)},
            {16 + 8, Quaternion.AngleAxis(90, Vector3.up)},
            {16 + 4, Quaternion.AngleAxis(90, Vector3.up) * Quaternion.AngleAxis(-90, new Vector3(0, 0, 1))},
            {32 + 4, Quaternion.AngleAxis(-90, Vector3.up) * Quaternion.AngleAxis(-90, new Vector3(0, 0, 1))},
			// +z+x, -z+x, -z-x, +z-x
			{32 + 2, Quaternion.AngleAxis(90, new Vector3(1, 0, 0))},
            {16 + 2, Quaternion.AngleAxis(90, new Vector3(1, 0, 0)) * Quaternion.AngleAxis(-90, new Vector3(0, 0, 1))},
            {16 + 1, Quaternion.AngleAxis(90, new Vector3(1, 0, 0)) * Quaternion.AngleAxis(-180, new Vector3(0, 0, 1))},
            {32 + 1, Quaternion.AngleAxis(90, new Vector3(1, 0, 0)) * Quaternion.AngleAxis(90, new Vector3(0, 0, 1))},

			// Triple Sided LOD
			// +x+y+z -x+y+z +x-y+z -x-y+z 
			{2 + 8 + 32, Quaternion.identity},
            {1 + 8 + 32, Quaternion.AngleAxis(-90, new Vector3(0, 1, 0))},
            {2 + 4 + 32, Quaternion.AngleAxis(90, new Vector3(1, 0, 0))},
            {1 + 4 + 32, Quaternion.AngleAxis(-90, new Vector3(0, 1, 0)) * Quaternion.AngleAxis(90, new Vector3(1, 0, 0))},
			// +x+y-z -x+y-z +x-y-z -x-y-z
			{2 + 8 + 16, Quaternion.AngleAxis(90, new Vector3(0, 1, 0))},
            {1 + 8 + 16, Quaternion.AngleAxis(180, new Vector3(0, 1, 0))},
            {2 + 4 + 16, Quaternion.AngleAxis(90, new Vector3(0, 1, 0)) * Quaternion.AngleAxis(90, new Vector3(1, 0, 0))},
            {1 + 4 + 16, Quaternion.AngleAxis(180, new Vector3(0, 1, 0)) * Quaternion.AngleAxis(90, new Vector3(1, 0, 0))},
    };

}