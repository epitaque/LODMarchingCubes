using System.Collections.Generic;
using UnityEngine;

namespace MarchingCubes {
    public static class FastMC {
		public static readonly int[,] Offsets = {
            {0, 0, 0}, {1, 0, 0}, {1, 1, 0}, {0, 1, 0},
            {0, 0, 1}, {1, 0, 1}, {1, 1, 1}, {0, 1, 1}
        };	

        static Vector3Int arrLoc;

        public static MCMesh PolygonizeArea(Vector3 min, float size, int resolution, sbyte[][][] data) {
			MCMesh m = new MCMesh();

            List<Vector3> Vertices = new List<Vector3>();
            List<int> Triangles = new List<int>();

            int ntriang, i;
            Vector3f VertList = new Vector3f[12];

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


                        if(caseCode == 0 || caseCode == 255) continue;

                        if ((Tables.edgeTable[caseCode] & 1) == 1)
                            //Vertices.Add(Lerp(densities[0], densities[1], x, y, z, x + 1, y, z));
                            //vertlist[0] = UtilFuncs.Lerp(isovalue,cell.points[0],cell.points[1]);
                        if ((Tables.edgeTable[caseCode] & 2) == 2)
                            Vertices.Add(Lerp(densities[1], densities[2], x + 1, y, z, x + 1, y + 1, z));
                            //vertlist[1] = UtilFuncs.Lerp(isovalue,cell.points[1],cell.points[2]);
                        if ((Tables.edgeTable[caseCode] & 4) == 4)
                            Vertices.Add(Lerp(densities[2], densities[3], x + 1, y + 1, z, x, y + 1, z));
                            //vertlist[2] = UtilFuncs.Lerp(isovalue,cell.points[2],cell.points[3]);
                        if ((Tables.edgeTable[caseCode] & 8) == 8)
                            Vertices.Add(Lerp(densities[3], densities[0], x, y + 1, z, x, y, z));
                            //vertlist[3] = UtilFuncs.Lerp(isovalue,cell.points[3],cell.points[0]);
                        if ((Tables.edgeTable[caseCode] & 16) == 16)
                            Vertices.Add(Lerp(densities[4], densities[5], x, y, z + 1, x + 1, y, z + 1));
                            //vertlist[4] = UtilFuncs.Lerp(isovalue,cell.points[4],cell.points[5]);
                        if ((Tables.edgeTable[caseCode] & 32) == 32)
                            Vertices.Add(Lerp(densities[5], densities[6], x + 1, y, z + 1, x + 1, y + 1, z + 1));
                            //vertlist[5] = UtilFuncs.Lerp(isovalue,cell.points[5],cell.points[6]);
                        if ((Tables.edgeTable[caseCode] & 64) == 64)
                            Vertices.Add(Lerp(densities[6], densities[7], x + 1, y + 1, z + 1, x, y + 1, z + 1));
                            //vertlist[6] = UtilFuncs.Lerp(isovalue,cell.points[6],cell.points[7]);
                        if ((Tables.edgeTable[caseCode] & 128) == 128)
                            Vertices.Add(Lerp(densities[7], densities[4], x, y + 1, z + 1, x, y, z + 1));
                            //vertlist[7] = UtilFuncs.Lerp(isovalue,cell.points[7],cell.points[4]);
                        if ((Tables.edgeTable[caseCode] & 256) == 256)
                            Vertices.Add(Lerp(densities[0], densities[4], x, y, z, x, y, z + 1));
                            //vertlist[8] = UtilFuncs.Lerp(isovalue,cell.points[0],cell.points[4]);
                        if ((Tables.edgeTable[caseCode] & 512) == 512)
                            Vertices.Add(Lerp(densities[1], densities[5], x + 1, y, z, x + 1, y, z + 1));
                            //vertlist[9] = UtilFuncs.Lerp(isovalue,cell.points[1],cell.points[5]);
                        if ((Tables.edgeTable[caseCode] & 1024) == 1024)
                            Vertices.Add(Lerp(densities[2], densities[6], x + 1, y + 1, z, x + 1, y + 1, z + 1));
                            //vertlist[10] = UtilFuncs.Lerp(isovalue,cell.points[2],cell.points[6]);
                        if ((Tables.edgeTable[caseCode] & 2048) == 2048)
                            Vertices.Add(Lerp(densities[3], densities[7], x, y + 1, z, x, y + 1, z + 1));
                            //vertlist[11] = UtilFuncs.Lerp(isovalue,cell.points[3],cell.points[7]);


                        /*ntriang = 0;
                        for (i=0; Tables.triTable[caseCode][i] != -1; i += 3 ) {
                            triangles[ntriang].p[0] = vertlist[Tables.triTable[cubeindex][i  ]];
                            triangles[ntriang].p[1] = vertlist[Tables.triTable[cubeindex][i+1]];
                            triangles[ntriang].p[2] = vertlist[Tables.triTable[cubeindex][i+2]];
                            ntriang++;
                        }*/

                        for (i = 0; Tables.triTable[caseCode][i] !=-1; i++) {
                            Vertices.Add(vertlist[triTable[cubeindex][i]]);
                        }


                        //m.Vertices.Add(new Vector3(x, y, z));
					}
				}
			}

            /*int[] Triangles = new int[Vertices.Count];
            for(int i = 0; i < Triangles.Length; i++) {
                Triangles[i] = i;
            }*/

            m.Vertices = Vertices;
            m.Triangles = Triangles;

			return m;
        }

        public static Vector3 Lerp(float density1, float density2, float x1, float y1, float z1, float x2, float y2, float z2) {
            if(density1 < 0.00001f || density1 > -0.00001f) {
                return new Vector3(x1, y1, z1);
            }
            if(density2 < 0.00001f || density2 > -0.00001f) {
                return new Vector3(x2, y2, z2);
            }
            if(Mathf.Abs(density1 - density2) < 0.00001f) {
                return new Vector3(x2, y2, z2);
            }

            float mu = (density1) / (density1 - density2); 

            return new Vector3(x1 + mu * (x2 - x1), y1 + mu * (y2 - y1), z1 + mu * (z2 - z1));
        }
    }
}
