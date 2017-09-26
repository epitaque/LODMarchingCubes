using UnityEngine;
using Util;

public static class UtilFuncs {
    public static SE.OpenSimplexNoise s = new SE.OpenSimplexNoise(2);
    public static SE.OpenSimplexNoise s2 = new SE.OpenSimplexNoise(5);

    public delegate void GameObjectModifier(GameObject obj);

    public delegate float Sampler(float x, float y, float z);

    public static float Sample(float x, float y, float z) {
        float r = 0.023f;

        //float valueAtXY = (float)s.Evaluate((double)x * r, (double)z * r) * 12f;

        float result = Sample2D(x * r, z * r) * 10f - y;
        //float result = (float)s.Evaluate((double)x * r, (double)y * r, (double)z * r);
        return result;
    }
    public static System.Func<float, float, float> noise1 = (anx, any) => (float)s.Evaluate((double)anx, (double)any)/2f + 0.5f;
    public static System.Func<float, float, float> noise2 = (anx, any) => (float)s2.Evaluate((double)anx, (double)any)/2f + 0.5f;

    public static float Sample2D(float x, float y) {
        
        float nx = x - 0.5f;
        float ny = y - 0.5f;
        var e = (1.00 * noise1( 1 * nx,  1 * ny)
            + 0.50 * noise1( 2 * nx,  2 * ny)
            + 0.25 * noise1( 4 * nx,  4 * ny)
            + 0.13 * noise1( 8 * nx,  8 * ny)
            + 0.06 * noise1(16 * nx, 16 * ny)
            + 0.03 * noise1(32 * nx, 32 * ny));
        e /= (1.00+0.50+0.25+0.13+0.06+0.03);
        e = Mathf.Pow((float)e, 4.24f);
        var m = (1.00 * noise2( 1 * nx,  1 * ny)
            + 0.75 * noise2( 2 * nx,  2 * ny)
            + 0.33 * noise2( 4 * nx,  4 * ny)
            + 0.33 * noise2( 8 * nx,  8 * ny)
            + 0.33 * noise2(16 * nx, 16 * ny)
            + 0.50 * noise2(32 * nx, 32 * ny));
        m /= (1.00+0.75+0.33+0.33+0.33+0.50);

        return (float)m;
            /* draw biome(e, m) at x,y */
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
    public static Color SinColor(float value) {
        float frequency = 0.3f;
        float red   = Mathf.Sin(frequency*value + 0) * 0.5f + 0.5f;
        float green = Mathf.Sin(frequency*value + 2) * 0.5f + 0.5f;
        float blue  = Mathf.Sin(frequency*value + 4) * 0.5f + 0.5f;
        return new Color(red, green, blue);
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