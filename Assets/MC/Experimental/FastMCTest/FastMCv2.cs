using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine;


// index3d(x, y, z) x*dim^2 + y*dim + z
// if(onMin)
// getEdge3d(x, y, z, w) (x*dim^2 + y*dim + z) * 3 + w


namespace MarchingCubes {
    public static class FastMC2 {

        public static List<Edge> debugEdges;

        public static List<Edge> mcDebugEdges;

        public static IEnumerable<Vector3> mcOnlyVertices;
        public static IEnumerable<Vector3> onlyFastVertices;

        public static List<Vector4>[] MCEdgeToEdgeXYZWOffset;

        public static List<Vector3> EdgeGennedVerticesA;

        public static MCMesh PolygonizeArea(Vector3 min, float size, int resolution, sbyte[][][] data) {
            MCMesh m = new MCMesh();

            int res1 = resolution + 1;

            MCEdgeToEdgeXYZWOffset = new List<Vector4>[12];
            for(int i = 0; i < 12; i++) {
                MCEdgeToEdgeXYZWOffset[i] = new List<Vector4>();
            }

            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            debugEdges = new List<Edge>();

            int[] edges = new int[res1 * res1 * res1 * 3];

            CreateVertices(edges, vertices, res1, data, debugEdges);

            EdgeGennedVerticesA = vertices;

            Debug.Log("vertex count: " + vertices.Count);
            Debug.Log("distinct vertex count: " + vertices.Distinct().Count());
            


            Debug.Log("edge count: " + debugEdges.Distinct().Count());
            Debug.Log("distinct edge count: " + debugEdges.Distinct().Count());

            List<Vector3> regMCGeneratedVertices = new List<Vector3>();


            GenVerticesViaRegularMC(regMCGeneratedVertices, resolution, data);
            mcOnlyVertices = regMCGeneratedVertices.Except(vertices);
            onlyFastVertices = vertices.Except(regMCGeneratedVertices);
            Debug.Log("#vertices generated in v1 that are not generated in v2: " + FindUniqueVertices(regMCGeneratedVertices, vertices).Count);

            Debug.Log("regular MC vertex count: " + regMCGeneratedVertices.Count);
            Debug.Log("distinct MC vertex count: " + regMCGeneratedVertices.Distinct().Count());

            PrintMCEdgeToEdgeXYZWOffsetTable();

            Triangulate(edges, triangles, resolution, data);

            Debug.Log("index count: " + triangles.Count);


            m.Vertices = vertices;
            m.Triangles = triangles.ToArray();

            return m;
        }


        public static List<Vector3> FindUniqueVertices(List<Vector3> listA, List<Vector3> itemsToExclude) {
            List<Vector3> uniques = new List<Vector3>();
            for(int i = 0; i < listA.Count; i++) {
                bool unique = true;
                for(int j = 0; j < itemsToExclude.Count; j++) {
                    if(Vector3.Distance(itemsToExclude[j], listA[i]) < 0.01f) {
                        unique = false;
                        break;
                    }
                }
                if(unique) {
                    uniques.Add(listA[i]);
                }
            }
            return uniques;
        }

        public static int MCGennedVertexToEdgeNum(Vector3 MCGennedVertex, List<Vector3> EdgeGennedVertices) {
            for(int i = 0; i < EdgeGennedVertices.Count; i++) {
                if(EdgeGennedVertices[i] == MCGennedVertex) {
                    return i;
                }
            }
            return -1;
        }

        public static void GenVerticesViaRegularMC(List<Vector3> Vertices, int resolution, sbyte[][][] data) {
            //List<Vector3> Vertices = new List<Vector3>();
            Hashtable ExistingVertices = new Hashtable();
            /*Map<Vector3, int> map = Collections.synchronizedMap(
                 new LinkedHashMap<String, String>());*/


            //List<Vector3> Vertices = new List<Vector3>();
            List<int> Triangles = new List<int>();

            int ntriang, i;

            int trinum = 0;
            Vector3[] VertList = new Vector3[12];

		    //Debug.Log(data[0][8][64]);
            sbyte[] densities = new sbyte[8];

			for(int x = 0; x < resolution; x++) {
				for(int y = 0; y < resolution; y++) {
					for(int z = 0; z < resolution; z++) {
                        byte caseCode = 0;

                        densities[0] = data[x  ][y  ][z  ];
                        densities[1] = data[x + 1][y][z];
                        densities[2] = data[x + 1][y + 1][z];
                        densities[3] = data[x][y + 1][z];
                        densities[4] = data[x][y][z + 1];
                        densities[5] = data[x + 1][y][z + 1];
                        densities[6] = data[x + 1][y + 1][z + 1];
                        densities[7] = data[x][y + 1][z + 1];
                        if (densities[0] < 0) caseCode |= 1;
                        if (densities[1] < 0) caseCode |= 2;
                        if (densities[2] < 0) caseCode |= 4;
                        if (densities[3] < 0) caseCode |= 8;
                        if (densities[4] < 0) caseCode |= 16;
                        if (densities[5] < 0) caseCode |= 32;
                        if (densities[6] < 0) caseCode |= 64;
                        if (densities[7] < 0) caseCode |= 128;

                        if(x == 0 && y == 0 && z == 0) {
                            Debug.Log("case code for xyz 0: " + caseCode + ", densities[0]: " + densities[0]);
                        }

                        if(caseCode == 0 || caseCode == 255) continue;

                        if ((Tables.edgeTable[caseCode] & 1) == 1)
                            VertList[0] = Lerp(densities[0], densities[1], x, y, z, x + 1, y, z);
                            //vertlist[0] = UtilFuncs.Lerp(isovalue,cell.points[0],cell.points[1]);
                        if ((Tables.edgeTable[caseCode] & 2) == 2)
                            VertList[1] = Lerp(densities[1], densities[2], x + 1, y, z, x + 1, y + 1, z);
                            //vertlist[1] = UtilFuncs.Lerp(isovalue,cell.points[1],cell.points[2]);
                        if ((Tables.edgeTable[caseCode] & 4) == 4)
                            VertList[2] = Lerp(densities[2], densities[3], x + 1, y + 1, z, x, y + 1, z);
                            //vertlist[2] = UtilFuncs.Lerp(isovalue,cell.points[2],cell.points[3]);
                        if ((Tables.edgeTable[caseCode] & 8) == 8)
                            VertList[3] = Lerp(densities[3], densities[0], x, y + 1, z, x, y, z);
                            //vertlist[3] = UtilFuncs.Lerp(isovalue,cell.points[3],cell.points[0]);
                        if ((Tables.edgeTable[caseCode] & 16) == 16)
                            VertList[4] = Lerp(densities[4], densities[5], x, y, z + 1, x + 1, y, z + 1);
                            //vertlist[4] = UtilFuncs.Lerp(isovalue,cell.points[4],cell.points[5]);
                        if ((Tables.edgeTable[caseCode] & 32) == 32)
                            VertList[5] = Lerp(densities[5], densities[6], x + 1, y, z + 1, x + 1, y + 1, z + 1);
                            //vertlist[5] = UtilFuncs.Lerp(isovalue,cell.points[5],cell.points[6]);
                        if ((Tables.edgeTable[caseCode] & 64) == 64)
                            VertList[6] = Lerp(densities[6], densities[7], x + 1, y + 1, z + 1, x, y + 1, z + 1);
                            //vertlist[6] = UtilFuncs.Lerp(isovalue,cell.points[6],cell.points[7]);
                        if ((Tables.edgeTable[caseCode] & 128) == 128)
                            VertList[7] = Lerp(densities[7], densities[4], x, y + 1, z + 1, x, y, z + 1);
                            //vertlist[7] = UtilFuncs.Lerp(isovalue,cell.points[7],cell.points[4]);
                        if ((Tables.edgeTable[caseCode] & 256) == 256)
                            VertList[8] = Lerp(densities[0], densities[4], x, y, z, x, y, z + 1);
                            //vertlist[8] = UtilFuncs.Lerp(isovalue,cell.points[0],cell.points[4]);
                        if ((Tables.edgeTable[caseCode] & 512) == 512)
                            VertList[9] = Lerp(densities[1], densities[5], x + 1, y, z, x + 1, y, z + 1);
                            //vertlist[9] = UtilFuncs.Lerp(isovalue,cell.points[1],cell.points[5]);
                        if ((Tables.edgeTable[caseCode] & 1024) == 1024)
                            VertList[10] = Lerp(densities[2], densities[6], x + 1, y + 1, z, x + 1, y + 1, z + 1);
                            //vertlist[10] = UtilFuncs.Lerp(isovalue,cell.points[2],cell.points[6]);
                        if ((Tables.edgeTable[caseCode] & 2048) == 2048)
                            VertList[11] = Lerp(densities[3], densities[7], x, y + 1, z, x, y + 1, z + 1);
                            //vertlist[11] = UtilFuncs.Lerp(isovalue,cell.points[3],cell.points[7]))


                        /*ntriang = 0;
                        for (i=0; Tables.triTable[caseCode][i] != -1; i += 3 ) {
                            triangles[ntriang].p[0] = vertlist[Tables.triTable[cubeindex][i  ]];
                            triangles[ntriang].p[1] = vertlist[Tables.triTable[cubeindex][i+1]];
                            triangles[ntriang].p[2] = vertlist[Tables.triTable[cubeindex][i+2]];
                            ntriang++;
                        }*/


                        for (i = 0; Tables.triTable[caseCode][i] !=-1; i++) {
                            int tri = Tables.triTable[caseCode][i];
                            Vector3 vert = VertList[tri];

                            int EdgeNum = MCGennedVertexToEdgeNum(vert, EdgeGennedVerticesA);

                            Debug.Assert(EdgeNum != -1);
                            int mcEdgeNum = tri;
                            Vector4 code = ReverseGetEdge3D(EdgeNum, resolution + 1);
                            Vector4 mcCoord = new Vector4(x, y, z, 0f);


                            Debug.Log("Marching Cube at " + mcCoord + " on edge " + tri + " has edge with code " + code);

                            if(!MCEdgeToEdgeXYZWOffset[tri].Contains(mcCoord - code)) {
                                MCEdgeToEdgeXYZWOffset[tri].Add( - mcCoord + code);
                            }


                            if(!ExistingVertices.Contains(vert)) {
                                Vertices.Add(vert);
                                ExistingVertices.Add(vert, trinum);
                                Triangles.Add(trinum);
                                trinum++;
                            }
                            else {
                                Triangles.Add((int)ExistingVertices[vert]);
                            }
                        }
                    }
                }
            }
        }

        public static void PrintMCEdgeToEdgeXYZWOffsetTable() {
            Debug.Log("|||MCEdgeToEdgeXYZWOffsetTable|||");
            for(int i = 0; i < 12; i++) {
                List<Vector4> offsets = MCEdgeToEdgeXYZWOffset[i];
                string toPrint = "Edge " + i;
                for(int j = 0; j < offsets.Count; j++) {
                    toPrint += "[" + j + "]: " + offsets[j];
                }
                Debug.Log(toPrint);
            }
        }


        public static void CreateVertices(int[] edges, List<Vector3> vertices, int res1, sbyte[][][] data, List<Edge> debugEdges) {
            int edgeNum = 0;
            int vertNum = 0;
            int iNum = 0;
            for(int x = 0; x < res1; x++) {
                for(int y = 0; y < res1; y++) {
                    for(int z = 0; z < res1; z++) {
                        for(int w = 0; w < 3; w++) {
                            edgeNum = GetEdge3D(x, y, z, res1, w);

                            if(iNum != edgeNum) {
                                Debug.LogWarning("Warning: iNum != edgeNum. xyzw: " + x + ", " + y + ", " + z + ", " + w + ", iNum: " + iNum + ", edgeNum: " + edgeNum);
                            }
                            iNum++;

                            bool terminating = false;

                            Vector4 reverseTest = ReverseGetEdge3D(edgeNum, res1);
                            //Debug.Log("ReverseEdge3D on edge:" + edgeNum + "." + " Guessed xyzw: " + reverseTest + " Actual: " + x + ", " + y + ", " + z + ", " + w);

                            if(reverseTest.x != x) {
                                Debug.Log("ReverseEdge3D x failed! real x : " + x + " guessed x " + reverseTest.x);
                                terminating = true;
                            }
                            if(reverseTest.y != y) {
                                Debug.Log("ReverseEdge3D y failed! real y : " + y + " guessed y " + reverseTest.y);
                                terminating = true;                                
                            }
                            if(reverseTest.z != z) {
                                Debug.Log("ReverseEdge3D z failed! real z : " + z + " guessed z " + reverseTest.z);
                                terminating = true;                                
                            }
                            if(reverseTest.w != w) {
                                Debug.Log("ReverseEdge3D w failed! real w : " + w + " guessed w " + reverseTest.w);
                                terminating = true;                                
                            }

                            if(terminating) {
                                Debug.LogError("ReverseEdge3D failed on edge:" + edgeNum + ", terminating early." + " Guessed xyzw: " + reverseTest + " Actual: " + x + ", " + y + ", " + z + ", " + w);
                                return;
                            }

                            int offsetX = EdgeOffsets[w,0];
                            int offsetY = EdgeOffsets[w,1];
                            int offsetZ = EdgeOffsets[w,2];

                            //int edgeNum = GetEdge3D(x, y, z, res1, w);
                            if(x + offsetX < 0 || y + offsetY < 0 || z + offsetZ < 0) {
                                edges[edgeNum] = -1;
                                edgeNum++;
                                continue;
                            }
                        
                            Edge e = new Edge();
                            e.point1 = new Vector3(x, y, z);
                            e.point2 = new Vector3(x + offsetX, y + offsetY, z + offsetZ);

                            e.isoVertex = Vector3.one;

                            debugEdges.Add(e);

                            sbyte density1 = data[x][y][z];
                            sbyte density2 = data[x + offsetX][y + offsetY][z + offsetZ];

                            if((density1 < 0 && density2 < 0) || (density1 > 0 && density2 > 0)) {
                                edges[edgeNum] = -1;
                                edgeNum++;
                                continue;
                            }

                            // if there's a vertex at density2 and y != 0, then


                            if(((density1 < 0.00001f && density1 > -0.00001f) && w != 0) || ((density2 < 0.00001f && density2 > -0.00001f) && !(w == 0 && y == 1))) {
                                edges[edgeNum] = edges[edgeNum] - w;
                                edgeNum++;
                                continue;
                            }

                            edges[edgeNum] = vertNum;
                            vertNum++;
                            Vector3 vert = Lerp(density1, density2, x, y, z, x + offsetX, y + offsetY, z + offsetZ);
                            vertices.Add(vert);

                            e.isoVertex = vert;
                        }
                    }
                }
            }
        }
        public static void Triangulate(int[] edges, List<int> triangles, int resolution, sbyte[][][] data) {
            sbyte[] densities = new sbyte[8];
            int i;
            int mcEdge;

            for(int x = 0; x < resolution; x++) {
				for(int y = 0; y < resolution; y++) {
					for(int z = 0; z < resolution; z++) {
                        byte caseCode = 0;

                        densities[0] = data[x  ][y  ][z  ];
                        densities[1] = data[x + 1][y][z];
                        densities[2] = data[x + 1][y + 1][z];
                        densities[3] = data[x][y + 1][z];
                        densities[4] = data[x][y][z + 1];
                        densities[5] = data[x + 1][y][z + 1];
                        densities[6] = data[x + 1][y + 1][z + 1];
                        densities[7] = data[x][y + 1][z + 1];
                        if (densities[0] < 0) caseCode |= 1;
                        if (densities[1] < 0) caseCode |= 2;
                        if (densities[2] < 0) caseCode |= 4;
                        if (densities[3] < 0) caseCode |= 8;
                        if (densities[4] < 0) caseCode |= 16;
                        if (densities[5] < 0) caseCode |= 32;
                        if (densities[6] < 0) caseCode |= 64;
                        if (densities[7] < 0) caseCode |= 128;

                        if(caseCode == 0 || caseCode == 255) return;

                        for (i = 0; Tables.triTable[caseCode][i] !=-1; i++) {
                            mcEdge = Tables.triTable[caseCode][i];

                            int edgeNum = MCEdgeToEdgeNum(x, y, z, resolution + 1, mcEdge);
                            
                            triangles.Add(edges[edgeNum]);
                        }
                    }
                }
            }
        }

        public static void DrawGizmos() {
            //Debug.Log("drawing gizmo...");

            Gizmos.color = Color.white;

            foreach(Edge e in debugEdges) {
                Gizmos.DrawLine(e.point1 + (e.point1 * 0.0f), e.point2 + (e.point1 * 0.0f));
                if(e.isoVertex != Vector3.one) {
                    Gizmos.DrawSphere(e.isoVertex + (e.point1 * 0.0f), 0.1f);
                }
            }

            Gizmos.color = Color.red;

            foreach(Vector3 onlyMCVertex in mcOnlyVertices) {
                if(UnityEngine.Random.Range(0, 2) == 1) Gizmos.DrawSphere(onlyMCVertex, 0.11f);
            }

            Gizmos.color = Color.green;

            foreach(Vector3 onlyFastVertex in onlyFastVertices) {
                if(UnityEngine.Random.Range(0, 2) == 1) Gizmos.DrawSphere(onlyFastVertex, 0.11f);
            }

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

        public static int GetEdge3D(int x, int y, int z, int res, int edgeNum) {
            return (3 * ((x * res * res) + (y * res) + z)) + edgeNum;
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

        // probably incorrect - need to do testing
        public static int MCEdgeToEdgeNum(int x, int y, int z, int res, int mcEdge) {
            if(mcEdge == 4) {
                return GetEdge3D(x, y, z, res, 0);
            }
            if(mcEdge == 9) {
                return GetEdge3D(x, y, z, res, 1);
            }
            if(mcEdge == 5) {
                return GetEdge3D(x, y, z, res, 2);
            }

            if(mcEdge == 8) {
                return GetEdge3D(x - 1, y, z, res, 1);
            }
            if(mcEdge == 7) {
                return GetEdge3D(x - 1, y, z, res, 2);
            }

            if(mcEdge == 6) {
                return GetEdge3D(x, y, z - 1, res, 0);
            }
            if(mcEdge == 10) {
                return GetEdge3D(x, y, z - 1, res, 1);
            }

            if(mcEdge == 0) {
                return GetEdge3D(x, y - 1, z, res, 0);
            }
            if(mcEdge == 1) {
                return GetEdge3D(x, y - 1, z, res, 2);
            }

            if(mcEdge == 3) {
                return GetEdge3D(x - 1, y - 1, z, res, 2); 
            }
            if(mcEdge == 2) {
                return GetEdge3D(x, y - 1, z + 1, res, 0); 
            }
            if(mcEdge == 11) {
                return GetEdge3D(x -1, y, z + 1, res, 1);
            }
            Debug.Assert(false);
            return -1;
        }

        public static readonly int[,] EdgeOffsets = {
            {0, -1, 0}, {-1, 0, 0}, {0, 0, -1}
        };


    }

    public class Edge {
        public Vector3 point1;
        public Vector3 point2;
        public Vector3 isoVertex;
    }
}
