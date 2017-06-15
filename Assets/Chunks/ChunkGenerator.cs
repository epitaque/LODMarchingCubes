using System.Diagnostics;
using System.Collections.Generic;
using System.Collections;
using SE;
using UnityEngine;

namespace Chunks {
public static class ChunkGenerator {
	public static ChunkJobResult CreateChunk(ChunkJob Job) {
		Stopwatch s = new Stopwatch();
		s.Start();
		SE.OpenSimplexNoise noise = new SE.OpenSimplexNoise(0);
		ChunkJobResult result = new ChunkJobResult();

		result.OriginalJob = Job;

		ExtractionInput input = new ExtractionInput();
		input.Isovalue = 0;
		input.LODSides = Job.LOD;
		input.Resolution = new Util.Vector3i(Job.Resolution, Job.Resolution, Job.Resolution);
		input.Sample = (float x, float y, float z) => sample(noise, x, y, z);

		ExtractionResult ExResult = SurfaceExtractor.ExtractSurface(input);
		result.Result = ExResult;

		s.Stop();	
		result.ProcessingTime = s.ElapsedMilliseconds;
		return result;
	}


	private static float sample(SE.OpenSimplexNoise noise, float x, float y, float z) {
		float r = 0.3f;
		float f = 0.03f;
		float result = 10.0f - x;
		result += (float)noise.Evaluate(x*r, y*r, z*r) * 0.1f;
		result += (float)noise.Evaluate(x*f, y*f, z*f) * 20f;
		return result;
	} 
}
}