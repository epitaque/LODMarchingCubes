﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using SE;

public class Controller : MonoBehaviour {
	static SE.OpenSimplexNoise noise = new SE.OpenSimplexNoise(324);

	public GameObject MeshPrefab;
	public GameObject ConsoleObject;

	private GameObject CurrentMesh;
	private bool IsRunning = false;


	Sample[] sampleFunctions = {
		(float x, float y, float z, float worldSize) => { // Torus
			float r1 = worldSize / 4.0f;
			float r2 = worldSize / 10.0f;
			float q_x = Mathf.Abs(Mathf.Sqrt(x * x + y * y)) - r1;
			float len = Mathf.Sqrt(q_x * q_x + z * z);
			return len - r2;
		},
		(float x, float y, float z, float worldSize) => {
			return (float)(noise.Evaluate(((double)x + 5.5d) * 0.05, ((double)y + 5.5d) * 0.05, ((double)z + 5.5d) * 0.05) * 127d);
		},
		(float x, float y, float z, float worldSize) => {
			float result = y - 10f;

			result += (float)(noise.Evaluate(((double)x + 5.5d) * 0.1, ((double)y + 5.5d) * 0.1, ((double)z + 5.5d) * 0.1) * 127d) * 0.1f;

			return result;
		}
	};

	float SurfaceD_torus_z(float x, float y, float z, float worldSize) {
		float r1 = worldSize / 4.0f;
		float r2 = worldSize / 10.0f;
		float q_x = Mathf.Abs(Mathf.Sqrt(x * x + y * y)) - r1;
		float len = Mathf.Sqrt(q_x * q_x + z * z);
		return len - r2;

	}

	public delegate float Sample(float x, float y, float z, float worldSize);

	int res = 64;

	// Use this for initialization
	void Start () {
		IsRunning = true;
		//System.Random random = new System.Random(5);
		//GenerateMesh(2);
		ConsoleObject.GetComponent<Console>().SetRegenerateFn(GenerateMesh);
	}
	
	void GenerateMesh(int sampleFn) {
		int res1 = res + 1;
		sbyte[][][][] data = new sbyte[res1][][][];

		float f = 0.01f;
		float nx, ny, nz;

		for(int x = 0; x < res1; x++) {
			data[x] = new sbyte[res1][][];
			for(int y = 0; y < res1; y++) {
				data[x][y] = new sbyte[res1][];
				for(int z = 0; z < res1; z++) {
					data[x][y][z] = new sbyte[4];
					//data[x][y][z] = (sbyte)((random.NextDouble() -0.5d) * 2d * 127d); 
					//data[x][y][z] = (sbyte)(noise.Evaluate(((double)x + 5.5d) * r, ((double)y + 5.5d) * r, ((double)z + 5.5d) * r) * 127d);
					//data[x][y][z] = (sbyte)((Mathf.Abs(SurfaceD_torus_z(x - res1/2, y - res1/2, z - res1/2, res1)) < 8 ? -16 * SurfaceD_torus_z(x - res1/2, y - res1/2, z - res1/2, res1) : SurfaceD_torus_z(x - res1/2, y - res1/2, z - res1/2, res1)));
					

					nx = (float)x - ((float)res1)/2f;
					ny = (float)y - ((float)res1)/2f;
					nz = (float)z - ((float)res1)/2f;

					data[x][y][z][0] = (sbyte)(Mathf.Clamp(-8f * sampleFunctions[sampleFn](nx, ny, nz, res1), -127, 127));

					float dx = sampleFunctions[sampleFn](nx+f, ny, nz, (float)res1) - sampleFunctions[sampleFn](nx-f, ny, nz, (float)res1);
					float dy = sampleFunctions[sampleFn](nx, ny+f, nz, (float)res1) - sampleFunctions[sampleFn](nx, ny-f, nz, (float)res1);
					float dz = sampleFunctions[sampleFn](nx, ny, nz+f, (float)res1) - sampleFunctions[sampleFn](nx, ny, nz-f, (float)res1);

					float total = (dx*dx) + (dy*dy) + (dz*dz);
					total = Mathf.Sqrt(total);

					dx /= total;
					dy /= total;
					dz /= total;

					dx *= 127;
					dy *= 127;
					dz *= 127;

					data[x][y][z][1] = (sbyte)dx;
					data[x][y][z][2] = (sbyte)dy;
					data[x][y][z][3] = (sbyte)dz;
				} 
			}
		}

		System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
		sw.Start();
		SE.MCMesh m = SE.MarchingCubes.PolygonizeArea(new Vector3(0, 0, 0), 16f, res, data);
		sw.Stop();

		Debug.Log(res + "^3 terrain took " + sw.ElapsedMilliseconds + " ms.");
		ConsoleObject.GetComponent<Console>().PrintString(res + "^3 terrain took " + sw.ElapsedMilliseconds + " ms. " + m.Vertices.Count + " vertices and " + (m.Triangles.Length / 3) + " triangles.");


		Mesh(m);

	}

	// Update is called once per frame
	void Update () {
		
	}

	void OnDrawGizmos() {
		if(IsRunning) {
			SE.MarchingCubes.DrawGizmos();
		}
	}

	void Mesh(MCMesh m) {
		Object.Destroy(CurrentMesh);

		GameObject clone = Object.Instantiate(MeshPrefab, new Vector3(0, 0, 0), Quaternion.identity);
		clone.name = "Test mesh";
		

		MeshFilter mf = clone.GetComponent<MeshFilter>();
		UnityEngine.Mesh m2 = new Mesh();
		m2.SetVertices(m.Vertices);
		m2.SetNormals(m.Normals);
		m2.triangles = m.Triangles;
		mf.mesh = m2;
		//m2.RecalculateNormals();

		CurrentMesh = clone;
	}
}
