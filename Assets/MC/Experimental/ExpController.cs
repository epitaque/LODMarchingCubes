using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using SE;
using Transvoxel.VolumeData;
using Transvoxel.Math;

public class ExpController : MonoBehaviour {
	public GameObject MeshPrefab;

	// Use this for initialization
	void Start () {
		int res = 32;

		SE.OpenSimplexNoise noise = new SE.OpenSimplexNoise();

		// fill an IVolumeData
		int res1 = res + 1;

		sbyte[][][] data = new sbyte[res1][][];

		double r = 0.05;


		for(int x = 0; x < res1; x++) {
			data[x] = new sbyte[res1][];
			for(int y = 0; y < res1; y++) {
				data[x][y] = new sbyte[res1];
				for(int z = 0; z < res1; z++) {
					data[x][y][z] = (sbyte) (noise.Evaluate((double)x * r, (double)y * r, (double)z * r) * 127d);
				} 
			}
		}

		System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
		sw.Start();
		MarchingCubes.MCMesh m = MarchingCubes.FastMC.PolygonizeArea(new Vector3(0, 0, 0), 16f, res, data);
		sw.Stop();

		Debug.Log(res + "^3 terrain took " + sw.ElapsedMilliseconds + " ms.");

		Mesh(m);
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	void Mesh(MarchingCubes.MCMesh m) {
		GameObject clone = Object.Instantiate(MeshPrefab, new Vector3(0, 0, 0), Quaternion.identity);
		clone.name = "Test mesh";
		

		MeshFilter mf = clone.GetComponent<MeshFilter>();
		UnityEngine.Mesh m2 = new Mesh();
		m2.SetVertices(m.Vertices);
		m2.triangles = m.Triangles;
		mf.mesh = m2;
		m2.RecalculateNormals();
	}
}
