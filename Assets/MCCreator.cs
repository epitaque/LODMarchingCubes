using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Chunks;

public class MCCreator : MonoBehaviour {
	public GameObject MeshPrefab;
	public GameObject Camera;

	public int LevelsOfDetail = 3;
	public float MinimumCellSize = 1;
	public int Resolution = 16;
	public int MaximumNumberChunksToPreparePerFrame = 1;

	public GameObject QuadTest;

	public ComputeShader NoiseShader;

	private ChunkMachine Machine;

	bool running = false;


	// Use this for initialization
	void Start () {
		UnityEngine.Debug.Log("C# Version: " + System.Environment.Version);

		Machine = new ChunkMachine(LevelsOfDetail, MinimumCellSize, MaximumNumberChunksToPreparePerFrame, Resolution, NoiseShader, MeshPrefab, gameObject);
		running = true;

		RunComputeShaderTest();
	}
	
	void RunComputeShaderTest () {
		ComputeShader shader = NoiseShader;

		int kernelHandle = shader.FindKernel("CSMain");
		RenderTexture tex = new RenderTexture(256,256,0);
		tex.dimension = UnityEngine.Rendering.TextureDimension.Tex3D; tex.volumeDepth = 256;
		tex.enableRandomWrite = true;
		
		tex.filterMode = FilterMode.Bilinear;
		tex.wrapMode = TextureWrapMode.Mirror;
		tex.Create();

		int size = 256;
		int threadsPerAxis = 4;

		int threadGroups = size/threadsPerAxis;

		Util.NoiseInfo data = new Util.NoiseInfo();
		data.offset = new Vector3(2000, 4213.4f, 32.3423f);
		data.frequency = 0.05f;

		Util.NoiseInfo[] arrdata = new Util.NoiseInfo[1];
		arrdata[0] = data;

		ComputeBuffer buffer = new ComputeBuffer(1, 16);
		buffer.SetData(arrdata);

		shader.SetBuffer(kernelHandle, "dataBuffer", buffer);
		shader.SetTexture(kernelHandle, "Result", tex);
		shader.Dispatch(kernelHandle, threadGroups, threadGroups, threadGroups);

		QuadTest.GetComponent<Renderer>().materials[0].SetTexture("_MainTex", tex);

	}

	void Update() {
		if(running) {
			Vector3 pPos = Camera.GetComponent<Transform>().position;

			Machine.Update(pPos);
		}
	}
}