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
		    //Debug.Log(data[0][8][64]);


            int x2;
            int y2;
            int z2;

			for(int x = 0; x < resolution; x++) {
				for(int y = 0; y < resolution; y++) {
					for(int z = 0; z < resolution; z++) {
                        byte caseCode = 0;
                        //byte n = 1;
                        //arrLoc.Set(x, y, z);

                        for(int i = 0; i < 8; i++) {
                            x2 = x + Offsets[i,0];
                            y2 = y + Offsets[i,1];
                            z2 = z + Offsets[i,2];
                            //arrLoc += Offsets[i];
                            //Vector3Int combined = Offsets[i] + new Vector3Int(x, y, z);
                            //Debug.Log("trying to access " + combined);
                            //if(data[x + Offsets[i].x][y + Offsets[i].y][z + Offsets[i].z] < 0) caseCode |= n; 
                        }
                        //n *= 2;

                        /*caseCode = 0;
                        if (grid.val[0] < isolevel) caseCode |= 1;
                        if (grid.val[1] < isolevel) caseCode |= 2;
                        if (grid.val[2] < isolevel) caseCode |= 4;
                        if (grid.val[3] < isolevel) caseCode |= 8;
                        if (grid.val[4] < isolevel) caseCode |= 16;
                        if (grid.val[5] < isolevel) caseCode |= 32;
                        if (grid.val[6] < isolevel) caseCode |= 64;
                        if (grid.val[7] < isolevel) caseCode |= 128;*/


                        if(caseCode == 0 || caseCode == 255) continue;

                        //m.Vertices.Add(new Vector3(x, y, z));
					}
				}
			}

			return m;
        }		
    }
}
