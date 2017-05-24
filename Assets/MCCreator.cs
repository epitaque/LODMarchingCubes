using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MCCreator : MonoBehaviour {
	public GameObject MeshPrefab;

	private List<GameObject> Meshes;

	private List<ExtractionResult> results;

	// Use this for initialization
	void Start () {
		Meshes = new List<GameObject>();
		results = new List<ExtractionResult>();

		int lods = 4;
		int startingSize = 32;
		int runningOffset = 0;

		for(int i = 0; i < lods; i++) {
			ExtractionInput input = new ExtractionInput();
			input.Isovalue = 0;
			input.Resolution = new Util.Vector3i(2, 2, (int)((float)startingSize * (1f/Mathf.Pow((float)i, 2f)));
			int size = (int)Mathf.Pow(2, i);
			input.Size = new Util.Vector3i(size, 1, size);
			runningOffset += size - 1;
			Vector3 off = new Vector3(runningOffset, 0, 0);
			input.Sample = (float x, float y, float z) => Util.Sample(x + off.x, y + off.y, z + off.z);;

			results.Add(SurfaceExtractor.ExtractSurface(input));
			results[results.Count - 1].offset = off;
		}

		foreach(ExtractionResult r in results) {
			CreateMesh(r);
		}
	}
	
	void CreateMesh(ExtractionResult r) {
		GameObject isosurfaceMesh = Instantiate(MeshPrefab, r.offset, Quaternion.identity);
		Meshes.Add(isosurfaceMesh);

		Material mat = isosurfaceMesh.GetComponent<Renderer>().materials[0];
		MeshFilter mf = isosurfaceMesh.GetComponent<MeshFilter>();
		MeshCollider mc = isosurfaceMesh.GetComponent<MeshCollider>();

		mf.mesh.vertices = r.m.vertices;
		mf.mesh.triangles = r.m.triangles;
		mc.sharedMesh = mf.mesh;
		//if(m.normals != null) mf.mesh.normals = m.normals;
		mf.mesh.RecalculateNormals();
		mf.mesh.RecalculateBounds();
	}

	// Update is called once per frame
	void OnDrawGizmos() {
		DrawGridCells();
	}

	void DrawGridCells() {
		foreach(ExtractionResult r in results) {
			foreach(Util.GridCell c in r.cells) {
				DrawGridCell(c, r.offset);
			}
		}
	}

	void DrawGridCell(Util.GridCell c, Vector3 offset) {
		Gizmos.color = Color.gray;
		for(int i = 0; i < c.points.Length; i++) {
			Gizmos.DrawCube(c.points[i].position + offset, 0.1f * Vector3.one);
		}
		for(int i = 0; i < 12; i++) {
			Gizmos.DrawLine(c.points[edges[i,0]].position + offset, c.points[edges[i,1]].position + offset);
		}
	}

	// [edgeNum] = [corner1, corner2]
	public static readonly int[,] edges = {
		{4, 7}, {0, 3}, {5, 6}, {1, 2}, {4, 5}, {0, 1}, {7, 6}, {3, 2}, {0, 4}, {5, 1}, {7, 3}, {2, 6}
	};
}
/*
Vertex and Edge Index Map
		
        7-------6------6
       /.             /|
      10.           11 |
     /  0           /  2
    /   .          /   |     ^ Y
   3-------7------2    |     |
   |    4 . . 4 . |. . 5     --> X
   |   .          |   /		 \/ -Z
   1  8           3  9
   | .            | /
   |.             |/
   0-------5------1
*/