using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine;


// index3d(x, y, z) x*dim^2 + y*dim + z
// if(onMin)
// getEdge3d(x, y, z, w) (x*dim^2 + y*dim + z) * 3 + w


namespace SE {
    public static class MarchingCubes {

        // LOD byte
        // -x +x -y +z -z +z

        public static MCMesh PolygonizeArea(Vector3 min, float size, byte LOD, int resolution, sbyte[][][][] data) {
            MCMesh m = new MCMesh();

            int res1 = resolution + 1;

            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<int> triangles = new List<int>();

            ushort[] edges = new ushort[res1 * res1 * res1 * 3];

            CreateVertices(edges, vertices, normals, res1, data);
            Triangulate(edges, triangles, resolution, data);

            m.Vertices = vertices;
            m.Triangles = triangles.ToArray();
            m.Normals = normals;

            return m;
        }

        public static void CreateVertices(ushort[] edges, List<Vector3> vertices, List<Vector3> normals, int res1, sbyte[][][][] data) {
            int edgeNum = 0;
            ushort vertNum = 0;
            sbyte density1, density2;

            int res1_3 = res1 * 3;
            int res1_2_3 = res1 * res1 * 3;

            for(int x = 0; x < res1; x++) {
                for(int y = 0; y < res1; y++) {
                    for(int z = 0; z < res1; z++, edgeNum += 3) {
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
                        if(y >= 1) {
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
                        if(x >= 1) {
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
                        if(z >= 1) {
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
        public static void Triangulate(ushort[] edges, List<int> triangles, int resolution, sbyte[][][][] data) {
            sbyte[] densities = new sbyte[8];
            int i, j;
            int mcEdge;

            int res1 = resolution + 1;
            int res1_2 = res1 * res1;
            
            int t1, t2, t3;

            for(int x = 0; x < resolution; x++) {
				for(int y = 0; y < resolution; y++) {
					for(int z = 0; z < resolution; z++) {
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

        public static void DrawGizmos() {
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


    }

    public class Edge {
        public Vector3 point1;
        public Vector3 point2;
        public Vector3 isoVertex;
    }
}
