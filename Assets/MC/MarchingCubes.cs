using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine;


// index3d(x, y, z) x*dim^2 + y*dim + z
// if(onMin)
// getEdge3d(x, y, z, w) (x*dim^2 + y*dim + z) * 3 + w


namespace SE {
    public static class MarchingCubes {
        public static List<Vector4> cubesGizmos = new List<Vector4>();

        public static List<Util.GridCell> TVDebugGridCells = new List<Util.GridCell>();
        public static List<Util.GridCell> TVDebugGridCellBounds = new List<Util.GridCell>();
        public static List<Vector3> DebugPoints = new List<Vector3>();


        // LOD byte
        // -x +x -y +z -z +z

        public static MCMesh PolygonizeArea(Vector3 min, float size, byte lod, int resolution, sbyte[][][][] data) {
            MCMesh m = new MCMesh();

            int res1 = resolution + 1;
            int resm1 = resolution - 1;

            cubesGizmos.Add(new Vector4(min.x + (resolution / 2), min.y + (resolution / 2), min.z + (resolution / 2), resolution));

            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<int> triangles = new List<int>();

            ushort[] edges = new ushort[res1 * res1 * res1 * 3];

            Vector3Int begin = new Vector3Int(0, 0, 0);
            Vector3Int end = new Vector3Int(res1, res1, res1);

            //CreateVertices(edges, begin, end, vertices, normals, res1, data);

            begin = new Vector3Int(1, 1, 1);
            end = new Vector3Int(resm1, resm1, resm1);

            //Triangulate(edges, begin, end, triangles, resolution, data);

            //GenerateTransitionCells(vertices, triangles, resolution, data, LOD);

            MCVT(vertices, triangles, normals, resolution, lod, data);

            m.Vertices = vertices;
            m.Triangles = triangles.ToArray();
            m.Normals = normals;

            return m;
        }

        public static void MCVT(List<Vector3> vertices, List<int> triangles, List<Vector3> normals, int resolution, byte lod, sbyte[][][][] data) {
            for(int x = 0; x < resolution; x += 2) {
                for(int y = 0; y < resolution; y += 2) {
                    for(int z = 0; z < resolution; z += 2) {
                        byte cellLod = 0;

                        if(x == 0) cellLod |= 1;
                        if(x == resolution - 2) cellLod |= 2;
                        if(y == 0) cellLod |= 4;
                        if(y == resolution - 2) cellLod |= 8;
                        if(z == 0) cellLod |= 16;
                        if(z == resolution - 2) cellLod |= 32;

                        cellLod = (byte)(lod & cellLod);

                        Util.GridCell tvCellBounds = new Util.GridCell();
                        tvCellBounds.points = new Util.Point[8];
                        for(int i = 0; i < 8; i++) {
                            tvCellBounds.points[i].position = new Vector3(x, y, z) + Tables.CellOffsets[i] * 2;
                            DebugPoints.Add(tvCellBounds.points[i].position);
                        }
                        TVDebugGridCellBounds.Add(tvCellBounds);

                        byte[][] offsets = Tables.MCLodTable[cellLod];

                        Debug.Log("offset length: " + offsets.Length);

                        if(offsets.Length > 0) {
                            for(int i = 0; i < offsets.Length; i++) {
                                Util.GridCell cell = new Util.GridCell();
                                cell.points = new Util.Point[8];

                                string strOffs = "Cell " + i + " offsets: ";

                                for(int j = 0; j < 8; j++) {
                                    cell.points[j] = new Util.Point();
                                    Vector3 pos = new Vector3(x + 1, y + 1, z + 1);
                                    byte offset = offsets[i][j];
                                    if((offset & 1) == 1) pos.x -= 1;
                                    if((offset & 2) == 2) pos.x += 1;
                                    if((offset & 4) == 4) pos.y -= 1;
                                    if((offset & 8) == 8) pos.y += 1;
                                    if((offset & 16) == 16) pos.z -= 1;
                                    if((offset & 32) == 32) pos.z += 1;
                                    
                                    strOffs += pos + " (" + offset + "), ";
                                    
                                    cell.points[j].position = pos;
                                    cell.points[j].density = (float)data[((int)pos.x)][((int)pos.y)][((int)pos.z)][0];

                                    { // Polyganise
                                        float isovalue = 0;
                                        Vector3[] vertlist = new Vector3[12];

                                        int iz,ntriang;
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

                                        int[,] edgepairs = { {0, 1}, {1, 2}, {2, 3}, {3, 0}, {4, 5}, {5, 6}, {6, 7}, {7, 4}, {0, 4}, {1, 5}, {2, 6}, {3, 7} };

                                        int andEd = 1;

                                        for(int ja = 0; ja < 12; ja++) {
                                            Util.Point A_ = cell.points[edgepairs[j,0]];
                                            Util.Point B_ = cell.points[edgepairs[j,0]];
                                            if((Tables.edgeTable[cubeindex] & andEd) == andEd) {
                                                vertlist[ja] = UtilFuncs.Lerp(isovalue, A_, B_);
                                            }
                                            andEd *= 2;
                                        }

                                        /* Create the triangle */
                                        for (i = 0; Tables.triTable[cubeindex][i] !=-1; i++) {
                                            vertices.Add(vertlist[Tables.triTable[cubeindex][i]]);
                                            triangles.Add(vertices.Count - 1);
                                        }			
                                    }


                                }

                                Debug.Log(strOffs);
                                Polyganise(cell, vertices, triangles, 0f);
                                TVDebugGridCells.Add(cell);

                            }
                        }
                    }
                }
            }
        }

        public static void FindEdgeId(byte lod, Vector3 A, Vector3 B) {
            //dim0: cell#
            //dim1: edge# (0-12)
            //dim2: edge# (0-1)

            // first step: get all the unique edges of the lod cell and number them
            //Vector3[][][] tempTable = new Vector3[Tables.MCLodTable[lod].Length][][];

            List<Vector3[]> UniqueEdges = new List<Vector3[]>();

            byte[][] tempOffsetTable = Tables.MCLodTable[lod];

            for(int i = 0; i < tempOffsetTable.Length; i++) {
                for(int j = 0; j < 12; j++) {
                    int a = Tables.edgePairs[j,0];
                    int b = Tables.edgePairs[j,1];

                    Vector3 A_ = ByteToVector3(tempOffsetTable[i][a]);
                    Vector3 B_ = ByteToVector3(tempOffsetTable[i][b]);

                    bool unique = true;

                    foreach(Vector3[] pair in UniqueEdges) {
                        if(pair[0] == A_ && pair[1] == B_) {unique = false; break;}
                        if(pair[0] == B_ && pair[1] == A_) {unique = false; break;}
                    }

                    if(unique) {
                        Vector3[] pair = {A_, B_};
                        UniqueEdges.Add(pair);
                    }

                }
            }



        }

        public static void GenerateUniqueEdgeLists() {
            Vector3[][][] table = new Vector3[63][][];

            for(int lod = 0; lod < 63; lod++) {

                List<Vector3[]> UniqueEdges = new List<Vector3[]>();

                byte[][] tempOffsetTable = Tables.MCLodTable[lod];

                for(int i = 0; i < tempOffsetTable.Length; i++) {
                    for(int j = 0; j < 12; j++) {
                        int a = Tables.edgePairs[j,0];
                        int b = Tables.edgePairs[j,1];

                        Vector3 A_ = ByteToVector3(tempOffsetTable[i][a]);
                        Vector3 B_ = ByteToVector3(tempOffsetTable[i][b]);

                        bool unique = true;

                        foreach(Vector3[] pair in UniqueEdges) {
                            if(pair[0] == A_ && pair[1] == B_) {unique = false; break;}
                            if(pair[0] == B_ && pair[1] == A_) {unique = false; break;}
                        }

                        if(unique) {
                            Vector3[] pair = {A_, B_};
                            UniqueEdges.Add(pair);
                        }

                    }
                }

                table[lod] = UniqueEdges.ToArray();
            }
        }

        public static Vector3 ByteToVector3(byte e) {
            Vector3 pos = new Vector3(1,1, 1);
            byte offset = e;
            if((offset & 1) == 1) pos.x -= 1;
            if((offset & 2) == 2) pos.x += 1;
            if((offset & 4) == 4) pos.y -= 1;
            if((offset & 8) == 8) pos.y += 1;
            if((offset & 16) == 16) pos.z -= 1;
            if((offset & 32) == 32) pos.z += 1;
            return pos;
        }

        public static void CreateVertices(ushort[] edges, Vector3Int begin, Vector3Int end, List<Vector3> vertices, List<Vector3> normals, int res1, sbyte[][][][] data) {
            int edgeNum = 0;
            ushort vertNum = 0;
            sbyte density1, density2;

            int res1_3 = res1 * 3;
            int res1_2_3 = res1 * res1 * 3;

            for(int x = begin.x; x < end.x; x++) {
                for(int y = begin.y; y < end.y; y++) {
                    for(int z = begin.z; z < end.z; z++, edgeNum += 3) {
                        edgeNum = GetEdge3D(x, y, z, 0, res1);
                        density1 = data[x][y][z][0];

                        if(density1 == 0) {
                            edges[edgeNum] = vertNum;
                            edges[edgeNum + 1] = vertNum;
                            edges[edgeNum + 2] = vertNum;
                            vertNum++;
                            normals.Add(new Vector3(data[x][y][z][1]/127f, data[x][y][z][2]/127f, data[x][y][z][3]/127f));
                            vertices.Add(new Vector3(x, y, z));
                            continue;
                        }
                        if(y >= begin.x + 1) {
                            density2 = data[x][y-1][z][0];
                            if((density1 & 256) != (density2 & 256)) {
                                if(density2 == 0) {
                                    edges[edgeNum] = edges[edgeNum - res1_3];
                                }
                                else {
                                    edges[edgeNum] = vertNum;
                                    vertNum++;
                                    normals.Add(LerpN(density1, density2, 
                                        data[x][y][z][1], data[x][y][z][2], data[x][y][z][3], 
                                        data[x][y-1][z][1], data[x][y-1][z][2], data[x][y-1][z][3]));
                                    vertices.Add(Lerp(density1, density2, x, y, z, x, y-1, z));
                                }
                            }
                        }
                        if(x >= begin.y + 1) {
                            density2 = data[x-1][y][z][0];
                            if((density1 & 256) != (density2 & 256)) {
                                if(density2 == 0) {
                                    edges[edgeNum+1] = edges[edgeNum - res1_2_3];
                                }
                                else {
                                    edges[edgeNum+1] = vertNum;
                                    vertNum++;
                                    normals.Add(LerpN(density1, density2, 
                                        data[x][y][z][1], data[x][y][z][2], data[x][y][z][3], 
                                        data[x-1][y][z][1], data[x-1][y][z][2], data[x-1][y][z][3]));
                                    vertices.Add(Lerp(density1, density2, x, y, z, x-1, y, z));
                                }
                            }
                        }
                        if(z >= begin.z + 1) {
                            density2 = data[x][y][z-1][0];
                            if((density1 & 256) != (density2 & 256)) {
                                if(density2 == 0) {
                                    edges[edgeNum+2] = edges[edgeNum - 3];
                                }
                                else {
                                    edges[edgeNum+2] = vertNum;
                                    vertNum++;
                                    normals.Add(LerpN(density1, density2, 
                                        data[x][y][z][1], data[x][y][z][2], data[x][y][z][3], 
                                        data[x][y][z-1][1], data[x][y][z-1][2], data[x][y][z-1][3]));
                                    vertices.Add(Lerp(density1, density2, x, y, z, x, y, z-1));
                                }
                            }
                        }
                    }
                }
            }
        }
        public static void Triangulate(ushort[] edges, Vector3Int begin, Vector3Int end, List<int> triangles, int resolution, sbyte[][][][] data) {
            sbyte[] densities = new sbyte[8];
            int i, j;
            int mcEdge;

            int res1 = resolution + 1;
            int res1_2 = res1 * res1;
            
            int t1, t2, t3;

            for(int x = begin.x; x < end.x; x++) {
				for(int y = begin.y; y < end.y; y++) {
					for(int z = begin.z; z < end.z; z++) {
                        byte caseCode = 0;

                        densities[0] = data[x][y][z+1][0];
                        densities[1] = data[x+1][y][z+1][0];
                        densities[2] = data[x+1][y][z][0];
                        densities[3] = data[x][y][z][0];
                        densities[4] = data[x][y+1][z+1][0];
                        densities[5] = data[x+1][y+1][z+1][0];
                        densities[6] = data[x+1][y+1][z][0];
                        densities[7] = data[x][y+1][z][0];

                        if (densities[0] < 0) caseCode |= 1;
                        if (densities[1] < 0) caseCode |= 2;
                        if (densities[2] < 0) caseCode |= 4;
                        if (densities[3] < 0) caseCode |= 8;
                        if (densities[4] < 0) caseCode |= 16;
                        if (densities[5] < 0) caseCode |= 32;
                        if (densities[6] < 0) caseCode |= 64;
                        if (densities[7] < 0) caseCode |= 128;

                        if(caseCode == 0 || caseCode == 255) continue;

                        for (i = 0; Tables.triTable[caseCode][i] != -1; i += 3) {
                            mcEdge = Tables.triTable[caseCode][i];
                            t1 = edges[3 * (
                                ( (x + Tables.MCEdgeToEdgeOffset[mcEdge, 0]) * res1_2) + 
                                ( (y + Tables.MCEdgeToEdgeOffset[mcEdge, 1]) * res1) + 
                                   z + Tables.MCEdgeToEdgeOffset[mcEdge, 2]) + 
                                       Tables.MCEdgeToEdgeOffset[mcEdge, 3]];
                            
                            mcEdge = Tables.triTable[caseCode][i+1];
                            t2 = edges[3 * (
                                ( (x + Tables.MCEdgeToEdgeOffset[mcEdge, 0]) * res1_2) + 
                                ( (y + Tables.MCEdgeToEdgeOffset[mcEdge, 1]) * res1) + 
                                   z + Tables.MCEdgeToEdgeOffset[mcEdge, 2]) + 
                                       Tables.MCEdgeToEdgeOffset[mcEdge, 3]];

                            mcEdge = Tables.triTable[caseCode][i+2];
                            t3 = edges[3 * (
                                ( (x + Tables.MCEdgeToEdgeOffset[mcEdge, 0]) * res1_2) + 
                                ( (y + Tables.MCEdgeToEdgeOffset[mcEdge, 1]) * res1) + 
                                   z + Tables.MCEdgeToEdgeOffset[mcEdge, 2]) + 
                                       Tables.MCEdgeToEdgeOffset[mcEdge, 3]];

                            if(t1 != t2 && t2 != t3 && t1 != t3) {
                                triangles.Add(t1);
                                triangles.Add(t2);
                                triangles.Add(t3);
                            }
                        }

                    }
                }
            }
        }
        public static void GenerateTransitionCells(List<Vector3> vertices, List<int> triangles, int resolution, sbyte[][][][] data, byte lod) {
            for(int x = 0; x < resolution; x += 2) {
                for(int y = 0; y < resolution; y += 2) {
                    for(int z = 0; z < resolution; z += 2) {
                        byte cellLod = 0;

                        if(x == 0) cellLod |= 1;
                        if(x == resolution - 2) cellLod |= 2;
                        if(y == 0) cellLod |= 4;
                        if(y == resolution - 2) cellLod |= 8;
                        if(z == 0) cellLod |= 16;
                        if(z == resolution - 2) cellLod |= 32;

                        cellLod = (byte)(lod & cellLod);

                        if(cellLod == 0) {
                            continue;
                        }

                        Util.GridCell tvCellBounds = new Util.GridCell();
                        tvCellBounds.points = new Util.Point[8];
                        for(int i = 0; i < 8; i++) {
                            tvCellBounds.points[i].position = new Vector3(x, y, z) + Tables.CellOffsets[i] * 2;
                            DebugPoints.Add(tvCellBounds.points[i].position);
                        }
                        TVDebugGridCellBounds.Add(tvCellBounds);

                        byte[][] offsets = Tables.MCLodTable[cellLod];

                        Debug.Log("offset length: " + offsets.Length);

                        if(offsets.Length > 0) {
                            for(int i = 0; i < offsets.Length; i++) {
                                Util.GridCell cell = new Util.GridCell();
                                cell.points = new Util.Point[8];

                                string strOffs = "Cell " + i + " offsets: ";

                                for(int j = 0; j < 8; j++) {
                                    cell.points[j] = new Util.Point();
                                    Vector3 pos = new Vector3(x + 1, y + 1, z + 1);
                                    byte offset = offsets[i][j];
                                    if((offset & 1) == 1) pos.x -= 1;
                                    if((offset & 2) == 2) pos.x += 1;
                                    if((offset & 4) == 4) pos.y -= 1;
                                    if((offset & 8) == 8) pos.y += 1;
                                    if((offset & 16) == 16) pos.z -= 1;
                                    if((offset & 32) == 32) pos.z += 1;
                                    
                                    strOffs += pos + " (" + offset + "), ";
                                    
                                    cell.points[j].position = pos;
                                    cell.points[j].density = (float)data[((int)pos.x)][((int)pos.y)][((int)pos.z)][0];




                                }

                                Debug.Log(strOffs);
                                Polyganise(cell, vertices, triangles, 0f);
                                TVDebugGridCells.Add(cell);

                            }
                        }

                        

                        //if(cellLod) 
                    }
                }
            }
        }

        public static void DrawGizmos() {
            Gizmos.color = Color.white;
            foreach(Vector4 cube in cubesGizmos) {
                UnityEngine.Gizmos.DrawWireCube(new Vector3(cube.x, cube.y, cube.z), Vector3.one * cube.w);
            }

            Gizmos.color = Color.red;
            DrawCubeGizmos();

            Gizmos.color = Color.blue;
            foreach(Vector3 point in DebugPoints) {
                //Gizmos.DrawSphere(point, 0.5f);
            }

            return;
        }

        public static float mu;
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

            mu = Mathf.Round((density1) / (density1 - density2) * 256) / 256.0f; 

            return new Vector3(x1 + mu * (x2 - x1), y1 + mu * (y2 - y1), z1 + mu * (z2 - z1));
        }

        public static Vector3 LerpN(float density1, float density2, float n1x, float n1y, float n1z, float n2x, float n2y, float n2z) {
            mu = Mathf.Round((density1) / (density1 - density2) * 256f) / 256f; 

            return new Vector3(n1x/127f + mu * (n2x/127f - n1x/127f), n1y/127f + mu * (n2y/127f - n1y/127f), n1z/127f + mu * (n2z/127f - n1z/127f));
        }

        public static int GetEdge3D(int x, int y, int z, int edgeNum, int res) {
            return (3 * ((x * res * res) + (y * res) + z)) + edgeNum;
        }

        public static void DrawCubeGizmos() {
            int nGridcells = 0;
            foreach(Util.GridCell cell in TVDebugGridCells) {
                nGridcells++;
                DrawGridCell(cell);
            }
            Debug.Log("Drew " + nGridcells + " gridcells.");
            foreach(Util.GridCell cell in TVDebugGridCellBounds) {
                DrawGridCell(cell);
            }
        }

        public static void DrawGridCell(Util.GridCell cell) {
            for(int i = 0; i < 12; i++) {
                Vector3 vert1 = cell.points[Tables.edgePairs[i, 0]].position;
                Vector3 vert2 = cell.points[Tables.edgePairs[i, 1]].position;
                Gizmos.DrawLine(vert1, vert2);
            }
        }

        public static Vector4 ReverseGetEdge3D(int edgeNum, int res) {
            int res3 = res * res * res;
            int res2 = res * res;

            int w = edgeNum % 3;

            int edgeDiv3 = (edgeNum - w) / 3;

            int x = edgeDiv3 / res2;
            int y = (edgeDiv3 - x*res2) / res;
            int z = (edgeDiv3 - ( (x*res2) + (y*res)));

            return new Vector4(x, y, z, w);
        }

        public static readonly int[,] EdgeOffsets = {
            {0, -1, 0}, {-1, 0, 0}, {0, 0, -1}
        };

		public static void Polyganise(Util.GridCell cell, List<Vector3> vertices, List<int> triangles, float isovalue)
		{
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
			for (i = 0; Tables.triTable[cubeindex][i] !=-1; i++) {
				vertices.Add(vertlist[Tables.triTable[cubeindex][i]]);
                triangles.Add(vertices.Count - 1);
			}			
		}

    }

    public class Edge {
        public Vector3 point1;
        public Vector3 point2;
        public Vector3 isoVertex;
    }
}
