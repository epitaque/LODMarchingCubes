using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OldController : MonoBehaviour {
	bool Running = false;
	SE.Octree.Wrapper Wrapper;
	public GameObject MeshPrefab;
	public GameObject Viewer;
	public GameObject ConsoleObject;
	public float WorldSize = 512f;
	public int MaxDepth = 13;

	void Start () {
		Wrapper = new SE.Octree.Wrapper(this.gameObject.GetComponent<Transform>(), MeshPrefab, WorldSize, MaxDepth, ConsoleObject.GetComponent<Console>());
		Running = true;
	}
	
	// Update is called once per frame
	void Update () {
		if(Running) {
			if(Input.GetKeyDown(KeyCode.R)) {
				Wrapper.Mesh();
			}
			if(Input.GetKeyDown(KeyCode.F)) {
				Wrapper.Update(Viewer.GetComponent<Transform>().position);
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
