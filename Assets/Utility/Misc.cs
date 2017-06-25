using UnityEngine;
using Util;

public static class UtilFuncs {
    public static SE.OpenSimplexNoise s = new SE.OpenSimplexNoise(2);

    public delegate float Sampler(float x, float y, float z);

    public static float Sample(float x, float y, float z) {
        float r = 0.14f;
        //float result = 0.5f - y;
        float result = (float)s.Evaluate((double)x * r, (double)y * r, (double)z * r);
        return result;
    }

    public static Vector3 Lerp(float isolevel, Point point1, Point point2) {
        if (Mathf.Abs(isolevel-point1.density) < 0.00001)
            return(point1.position);
        if (Mathf.Abs(isolevel-point2.density) < 0.00001)
            return(point2.position);
        if (Mathf.Abs(point1.density-point2.density) < 0.00001)
            return(point2.position);
        float mu = (isolevel - point1.density) / (point2.density - point1.density); 
        return point1.position + mu * (point2.position - point1.position); 
    }

}

namespace Util {
    public struct NoiseInfo {
        public Vector3 offset;
        public float frequency;
    }

    public struct Vector3i {
        public int x;
        public int y;
        public int z;

        public Vector3i(int x, int y, int z) { 
            this.x = x; this.y = y; this.z = z; 
        }
        public int getDimensionSigned(int dim) {
            switch(dim) {
                case 0: return -x;
                case 1: return x;
                case 2: return -y;
                case 3: return y;
                case 4: return -z;
                case 5: return z;
            }
            return -1;
        }
        public int getDimension(int dim) {
            switch(dim) {
                case 0: return x;
                case 1: return y;
                case 2: return z;
            }
            return -1;
        }
        public void setDimension(int dim, int val) {
            switch(dim) {
                case 0: x = val; break;
                case 1: y = val; break;
                case 2: z = val; break;
            }
        }
    }
    public struct GridCell {
        public Point[] points;
        public GridCell Clone() {
            GridCell c = new GridCell();
            c.points = new Point[points.Length];
            for(int i = 0; i < points.Length; i++) {
                c.points[i] = points[i];
            }
            return c;
        }
    }

    public struct Point {
        public Vector3 position;
        public float density;    
    }
}