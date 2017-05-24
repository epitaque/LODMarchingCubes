using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MCCreator : MonoBehaviour {
	public GameObject MeshPrefab;

	private List<GameObject> Meshes;
	private List<Vector3> sampledPoints;

	// Use this for initialization
	void Start () {
		Meshes = new List<GameObject>();

		Mesh smallMesh = SurfaceExtractor.ExtractSurface(Util.Sample, 0, 2, 1, 1, 1);
		Mesh largeMesh = SurfaceExtractor.ExtractSurface((float x, float y, float z) => Util.Sample(x + 2, y, z),
		 0, 2, 2, 1, 2);

		sampledPoints = new List<Vector3>();
		sampledPoints.AddRange(smallMesh.normals);

		sampledPoints.AddRange(largeMesh.normals);

		CreateMesh(smallMesh, new Vector3(0, 0, 0));
		CreateMesh(largeMesh, new Vector3(2, 0, 0));
	}
	
	void CreateMesh(Mesh m, Vector3 offset) {
		GameObject isosurfaceMesh = Instantiate(MeshPrefab, offset, Quaternion.identity);
		Meshes.Add(isosurfaceMesh);

		Material mat = isosurfaceMesh.GetComponent<Renderer>().materials[0];
		MeshFilter mf = isosurfaceMesh.GetComponent<MeshFilter>();
		MeshCollider mc = isosurfaceMesh.GetComponent<MeshCollider>();

		mf.mesh.vertices = m.vertices;
		mf.mesh.triangles = m.triangles;
		mc.sharedMesh = mf.mesh;
		//if(m.normals != null) mf.mesh.normals = m.normals;
		mf.mesh.RecalculateNormals();
		mf.mesh.RecalculateBounds();
	}

	// Update is called once per frame
	void OnDrawGizmos() {
		DrawCorners();
	}

	void DrawCorners() {
		foreach(Vector3 v in sampledPoints) {
			Gizmos.DrawCube(v, Vector3.one * 0.1f);
		}
	}
}
