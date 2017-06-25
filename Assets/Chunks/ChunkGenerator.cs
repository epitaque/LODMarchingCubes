using System.Diagnostics;
using System.Collections.Generic;
using System.Collections;
using SE;
using UnityEngine;

namespace Chunks {
public static class ChunkGenerator {
	public static ChunkJobResult CreateChunk(ChunkJob Job) {
		ChunkJobResult result = new ChunkJobResult();

		try {
			Stopwatch s = new Stopwatch();
			s.Start();
			SE.OpenSimplexNoise noise = new SE.OpenSimplexNoise(1);

			result.Error = null;
			result.OriginalJob = Job;
			result.DebugPrint = "";


			ExtractionInput input = new ExtractionInput();
			input.Isovalue = 0;
			input.LODSides = Job.LOD;
			input.Resolution = new Util.Vector3i(Job.Resolution, Job.Resolution, Job.Resolution);
			input.Size = new Vector3(Job.CellSize, Job.CellSize, Job.CellSize);

			int numTimesSampled = 0;

			input.Sample = (float x, float y, float z) => {
				numTimesSampled++;
				float res = sample(noise, x + Job.Min.x, y + Job.Min.y, z + Job.Min.z);
				return res;
			};

			ExtractionResult ExResult = SurfaceExtractor.ExtractSurface(input);
			result.Result = ExResult;



			s.Stop();	
			result.ProcessingTime = s.ElapsedMilliseconds;

		}
		catch (System.Exception exc) {
			result.Error = "Error in thread " + Job.ThreadID + ": " + exc.Message + ", Stacktrace: " + exc.StackTrace;
		}
		return result;
	}


	private static float sample(SE.OpenSimplexNoise noise, float x, float y, float z) {	
		float r = 0.2f;
		float f = 0.03f;
		float ms = 0.009f;
		float result = 2.0f - y;
		result += (float)noise.Evaluate(x*r, y*r, z*r) * 1.6f;
		result += (float)noise.Evaluate(x*f, y*f, z*f) * 20f;
		result += (float)noise.Evaluate(x*f, y*f, z*ms) * 50f;

		return result;
	} 
}
}