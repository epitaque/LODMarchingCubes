using System.Collections.Generic;
using UnityEngine;

namespace SE {
	public static class UVGenerator {

		public static List<Vector2> GenerateUVs(Vector3[] vertices, Vector3[] normals) {
			List<Vector2> uvs = new List<Vector2>();
			for(int i = 0; i < vertices.Length; i++) {
				Vector3 vertex = vertices[i];
				Vector3 normal = normals[i];

				Vector2 uv;
				uv = CustomSphericalProjection(vertex, normal);

				uvs.Add(uv);
			}
			return uvs;
		}

		public static Vector2 CustomSphericalProjection(Vector3 vertex, Vector3 normal) {
				Vector2 uv = new Vector2();

				float weight_ux = 0f;
				if(normal.x > 0f) weight_ux = normal.x;
				float weight_uy = 0f;
				if(normal.y > 0f) weight_uy = normal.y;
				float weight_uz = 0f;
				if(normal.z > 0f) weight_uz = normal.z;

				float weight_vx = 0f;
				if(normal.x < 0f) weight_vx = Mathf.Abs(normal.x);
				float weight_vy = 0f;
				if(normal.y < 0f) weight_vy = Mathf.Abs(normal.y);
				float weight_vz = 0f;
				if(normal.z < 0f) weight_vz = Mathf.Abs(normal.z);

				uv.x = (weight_ux * vertex.x) + (weight_uy * vertex.y) + (weight_uz * vertex.z);
				uv.y = (weight_vx * vertex.x) + (weight_vy * vertex.y) + (weight_vz * vertex.z);
				return uv;
		}

		public static Vector2 CustomCubicProjection(Vector3 vertex, Vector3 normal) {
				Vector2 uv = new Vector2();

				if(Mathf.Abs(normal.x) > Mathf.Abs(normal.y) && Mathf.Abs(normal.x) > Mathf.Abs(normal.z)) {
					uv.x = vertex.y;
					uv.y = vertex.z;
				}
				else if(Mathf.Abs(normal.y) > Mathf.Abs(normal.z)) {
					uv.x = vertex.x;
					uv.y = vertex.z;
				}
				else {
					uv.x = vertex.x;
					uv.y = vertex.y;
				}			
				return uv;
		}

		public static Vector2 CubicProjection(Vector3 vertex, Vector3 normal) {
			Vector3 reflectedVector = (2f * Vector3.Dot(normal, vertex)) * (normal - vertex);

			float rx = reflectedVector.x;
			float ry = reflectedVector.y;
			float rz = reflectedVector.z;

			if((rx >= ry) && (rx  >= rz)) 
			{ 
				float sc = -rz; 
				float tc = -ry; 
				float ma = Mathf.Abs(rx);  //absolute value
				float s = ((sc/ma) + 1) / 2; 
				float t = ((tc/ma) + 1) / 2; 

				return new Vector2(s, t);
			} 

			if((rx <= ry) && (rx  <= rz)) 
			{ 
				float sc = +rz; 
				float tc = -ry; 
				float ma = Mathf.Abs(rx); 
				float s = ((sc/ma) + 1) / 2; 
				float t = ((tc/ma) + 1) / 2; 

				return new Vector2(s, t);
			} 

			if((ry >= rz) && (ry >= rx)) 
			{ 
				float sc = +rx; 
				float tc = +rz; 
				float ma = Mathf.Abs(ry); 
				float s = ((sc/ma) + 1) / 2; 
				float t = ((tc/ma) + 1) / 2; 

				return new Vector2(s, t);
			} 

			if((ry <= rz) && (ry <= rx)) 
			{ 
				float sc = +rx; 
				float tc = -rz; 
				float ma = Mathf.Abs(ry); 
				float s = ((sc/ma) + 1) / 2; 
				float t = ((tc/ma) + 1) / 2; 

				return new Vector2(s, t);
			} 

			if((rz >= ry) && (rz >= rx)) 
			{ 
				float sc = +rx; 
				float tc = -ry; 
				float ma = Mathf.Abs(rz); 
				float s = ((sc/ma) + 1) / 2; 
				float t = ((tc/ma) + 1) / 2; 

				return new Vector2(s, t);
			} 

			if((rz <= ry) && (rz <= rx)) 
			{ 
				float sc = -rx; 
				float tc = -ry; 
				float ma = Mathf.Abs(rz); 
				float s = ((sc/ma) + 1) / 2; 
				float t = ((tc/ma) + 1) / 2; 

				return new Vector2(s, t);
			} 

			return new Vector2(vertex.x, vertex.z);
		}

		public static Vector2 SphericalProjection(Vector3 vertex, Vector3 normal) {
			Vector2 uv = new Vector2();

			float r = Mathf.Sqrt(Mathf.Pow(normal.x, 2) + Mathf.Pow(normal.y, 2) + Mathf.Pow(normal.z, 2));
			float theta = Mathf.Atan(normal.y / normal.x);
			float azimuth = Mathf.Acos(normal.z / r);

			uv.x = theta;
			uv.y = azimuth;
			return uv;
		}

	}
}
