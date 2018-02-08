using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OldController : MonoBehaviour {
	private bool Running = false;
	private SE.Octree.Wrapper Wrapper;
	public GameObject MeshPrefab;
	public GameObject Viewer;
	public GameObject ConsoleObject;
	public float WorldSize = 512f;
	public int MaxDepth = 13;
	public int Resolution = 16;
	public bool MeshEveryFrame = false;
	public bool UpdateEveryFrame = false;
	public bool AsynchronousOctreeUpdate = false;
	public bool AsynchronousMeshUpdate = false;

	void Start () {
		Wrapper = new SE.Octree.Wrapper(this.gameObject.GetComponent<Transform>(), MeshPrefab, WorldSize, MaxDepth, Resolution, ConsoleObject.GetComponent<Console>(), MeshEveryFrame, AsynchronousOctreeUpdate, AsynchronousMeshUpdate);
		Running = true;
	}
	
	// Update is called once per frame
	void Update () {
		//Debug.Log("Update loop");
		if(Running) {
			if(Input.GetKeyDown(KeyCode.R)) {
				Wrapper.Mesh();
			}
			if(Input.GetKeyDown(KeyCode.F)) {
				Wrapper.Update(Viewer.GetComponent<Transform>().position, true);
			}
			else if(UpdateEveryFrame) {
				Wrapper.Update(Viewer.GetComponent<Transform>().position, false);
			}
			if(Input.GetKeyDown(KeyCode.C)) {
				Wrapper.MakeConforming();
			}
		}
	}

	void OnDrawGizmos() {
		if(Running) Wrapper.DrawGizmos();
	}
}
