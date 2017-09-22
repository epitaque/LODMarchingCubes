using UnityEngine;
using System.Collections.Generic;

namespace Chunks {
	public class GPUChunkNoiseGenerator {
		private bool Processing;

		private ComputeShader CShader;		

		private int TextureSize;
		private int ThreadsPerAxis;
		private int ThreadGroups;
		private int FloatArrSize;

		private RenderTexture[] UsedTextures;
		private float[][] Datas;

		public GPUChunkNoiseGenerator(ComputeShader computeShader) {
			this.CShader = computeShader;
			ThreadGroups = TextureSize / ThreadsPerAxis;
			FloatArrSize = TextureSize * TextureSize * TextureSize;
		}

		public bool StartGeneratingNoise(NoiseGenerationRequest NoiseRequest) {
			if(Processing) return false;

			Datas = new float[NoiseRequest.Lods][];
			UsedTextures = new RenderTexture[NoiseRequest.Lods];
			int kernelHandle = CShader.FindKernel("CSMain");

			for(int i = 0; i < NoiseRequest.Lods; i++) {
				Datas[i] = new float[65*65*65];
				RenderTexture tex = new RenderTexture(65, 65, 24);
				tex.dimension = UnityEngine.Rendering.TextureDimension.Tex3D; 
				tex.volumeDepth = 65;
				tex.enableRandomWrite = true;

				Util.NoiseInfo data = new Util.NoiseInfo();


				// LOD1 -32, -32

				// LOD2 -64, -64

				float LODOffset = Mathf.Pow(2, 5 + i);

				data.offset = new Vector3(-LODOffset, -LODOffset, -LODOffset) + NoiseRequest.Center;
				data.frequency = 1.0f / (float)i;

				Util.NoiseInfo[] arrdata = new Util.NoiseInfo[1];
				arrdata[0] = data;

				ComputeBuffer buffer = new ComputeBuffer(1, 16);
				buffer.SetData(arrdata);

				CShader.SetBuffer(kernelHandle, "dataBuffer", buffer);
				CShader.SetTexture(kernelHandle, "Result", tex);
				CShader.Dispatch(kernelHandle, 16, 16, 16);


				//AsyncTextureReader.RequestTextureData(tex.GetNativeTexturePtr());

				AsyncTextureReader.RequestTexture3DDataWPtr(tex.GetNativeTexturePtr());
			}

			return true;
		}

		private void FinishQueuing() {
		}
	}
}