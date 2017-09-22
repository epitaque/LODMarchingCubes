using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Chunks;

public class MCCreator : MonoBehaviour {
	public GameObject ChunkPrefab;
	public GameObject Camera;

	public int LevelsOfDetail = 3;
	public float MinimumCellSize = 1;
	public int Resolution = 16;
	public int MaximumNumberChunksToPreparePerFrame = 1;

	public GameObject QuadTest;

	public ComputeShader NoiseShader;

	private ChunkMachine Machine;

	bool Running = false;

	private ObjectPool Pool;

	public float LastPrintUpdate;

	public SE.Octree.Node octreeNode;

	// Use this for initialization
	void Start () {

		octreeNode = SE.Octree.Octree.Create();
		Running = true;

		/*int MaxChunkObjects = 8;
		MaxChunkObjects += LevelsOfDetail * 56;
		MaxChunkObjects *= 2;

		Pool = ObjectPool.Instance;
		Pool.AddToPool(ChunkPrefab, MaxChunkObjects, gameObject.GetComponent<Transform>());

		Machine = new ChunkMachine(LevelsOfDetail, MinimumCellSize, MaximumNumberChunksToPreparePerFrame, Resolution, NoiseShader, ChunkPrefab, gameObject.GetComponent<Transform>());


		Running = true;*/

		//RunComputeShaderTest();
	}
	
	public void ChunkObjectInitializer(GameObject chunkObject) {
		UnityEngine.Mesh m = new Mesh();
		m.vertices = new Vector3[36864]; // 36864 = max number of vertices for 16^3 marching cubes mesh
		m.normals = new Vector3[36864];
		chunkObject.GetComponent<MeshFilter>().mesh = m;
	}

	void RunComputeShaderTest () {
		ComputeShader shader = NoiseShader;

		int kernelHandle = shader.FindKernel("CSMain");

		int numTests = 1;

		RenderTexture[] textures = new RenderTexture[64];

		int size = 256;
		int threadsPerAxis = 4;

		int threadGroups = size/threadsPerAxis;

		Vector3 offset = new Vector3(0, 0, 0);

		for(int i = 0; i < textures.Length; i++) {
			RenderTexture tex = new RenderTexture(256,256,0);
			tex.dimension = UnityEngine.Rendering.TextureDimension.Tex3D; 
			tex.volumeDepth = 256;
			tex.enableRandomWrite = true;
			
			tex.filterMode = FilterMode.Bilinear;
			tex.wrapMode = TextureWrapMode.Mirror;
			tex.Create();

			Util.NoiseInfo data = new Util.NoiseInfo();
			data.offset = new Vector3(0, 0, 0);
			data.frequency = (float)i / 32f;

			Util.NoiseInfo[] arrdata = new Util.NoiseInfo[1];
			arrdata[0] = data;

			ComputeBuffer buffer = new ComputeBuffer(1, 16);
			buffer.SetData(arrdata);

			shader.SetBuffer(kernelHandle, "dataBuffer", buffer);
			shader.SetTexture(kernelHandle, "Result", tex);
			shader.Dispatch(kernelHandle, threadGroups, threadGroups, threadGroups);


			GameObject newObj = Instantiate(QuadTest, offset, Quaternion.identity);
			newObj.transform.SetParent(gameObject.transform);

			newObj.GetComponent<Renderer>().materials[0].SetTexture("_MainTex", tex);

			newObj.name = "QuadTest " + i;

			textures[i] = tex;
			offset.x += 1.05f;
		}







	}

	void Update() {
		if(Running) {
			Vector3 pPos = Camera.GetComponent<Transform>().position;

			/*Machine.Update(pPos);

			if(UnityEngine.Time.time - LastPrintUpdate > 5) {
				LastPrintUpdate = UnityEngine.Time.time;
				UnityEngine.Debug.Log("NumObjectsInstnatiated: " + ObjectPool.numObjectsInstantiated + ", destroyed: " + ObjectPool.numObjectsDestroyed); 
			}*/

			SE.Octree.Octree.Adapt(octreeNode, pPos / 64f, 15);
		}
	}

	void OnDrawGizmos() {
		if(Running) {
			SE.Octree.Octree.DrawGizmos(octreeNode);
		}
	}
}