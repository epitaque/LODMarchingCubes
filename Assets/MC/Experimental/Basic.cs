using UnityEngine;

namespace MarchingCubes {
    public static class MCBasics {
        public delegate float Sample(Vector3 position);

        public static Sample defaultSample = (Vector3 position) =>
            Mathf.Sin(position.x) * Mathf.Sin(position.y) * Mathf.Sin(position.z);
    }
}
