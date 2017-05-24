using UnityEngine;

public static class Util {
    public static SE.OpenSimplexNoise s = new SE.OpenSimplexNoise(5);

    public delegate float Sampler(float x, float y, float z);

    public static float Sample(float x, float y, float z) {
        float r = 0.4f;
        float result = (float)s.Evaluate((double)x * r, (double)y * r, (double)z * r);
        return result;
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

    public struct Vector3i {
        public int x;
        public int y;
        public int z;

        public Vector3i(int x, int y, int z) { this.x = x; this.y = y; this.z = z; }
    }
}