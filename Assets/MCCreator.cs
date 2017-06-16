using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Chunks;

public class MCCreator : MonoBehaviour {
	public GameObject MeshPrefab;
	public GameObject Camera;

	public int LevelsOfDetail = 3;
	public int NumberOfThreads = 2;
	public float MinimumCellSize = 1;
	public int Resolution = 16;
	private ChunkMachine Machine;

	bool running = false;

	// Use this for initialization
	void Start () {
		Machine = new ChunkMachine(LevelsOfDetail, NumberOfThreads, MinimumCellSize, Resolution, MeshPrefab);
		running = true;
	}
	
	void Update() {
		if(running) {
			Vector3 pPos = Camera.GetComponent<Transform>().position;
			Machine.Update(pPos);
		}
	}
}