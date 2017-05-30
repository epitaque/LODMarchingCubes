using System;
using UnityEngine;

namespace SE.QEF
{
	public class QEFData
	{
		public float ata_00, ata_01, ata_02, ata_11, ata_12, ata_22;
		public float atb_x, atb_y, atb_z;
		public float btb;
		public float massPoint_x, massPoint_y, massPoint_z;
		public int numPoints;

		public QEFData()
		{
			Clear();
		}

		public QEFData(float ata_00, float ata_01,
				  float ata_02, float ata_11, float ata_12,
				  float ata_22, float atb_x, float atb_y,
				  float atb_z, float btb, float massPoint_x,
				  float massPoint_y, float massPoint_z,
				  int numPoints)
		{
			Set(ata_00, ata_01, ata_02, ata_11, ata_12, ata_22, atb_x, atb_y,
				  atb_z, btb, massPoint_x, massPoint_y, massPoint_z, numPoints);
		}

		public void Add(ref QEFData rhs)
		{
			ata_00 += rhs.ata_00;
			ata_01 += rhs.ata_01;
			ata_02 += rhs.ata_02;
			ata_11 += rhs.ata_11;
			ata_12 += rhs.ata_12;
			ata_22 += rhs.ata_22;
			atb_x += rhs.atb_x;
			atb_y += rhs.atb_y;
			atb_z += rhs.atb_z;
			btb += rhs.btb;
			massPoint_x += rhs.massPoint_x;
			massPoint_y += rhs.massPoint_y;
			massPoint_z += rhs.massPoint_z;
			if (rhs.numPoints == 0)
			{
			}
			numPoints += rhs.numPoints;
		}

		public void Clear()
		{
			Set(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
		}

		public void Set(float ata_00, float ata_01,
				  float ata_02, float ata_11, float ata_12,
				  float ata_22, float atb_x, float atb_y,
				  float atb_z, float btb, float massPoint_x,
				  float massPoint_y, float massPoint_z,
				  int numPoints)
		{
			this.ata_00 = ata_00;
			this.ata_01 = ata_01;
			this.ata_02 = ata_02;
			this.ata_11 = ata_11;
			this.ata_12 = ata_12;
			this.ata_22 = ata_22;
			this.atb_x = atb_x;
			this.atb_y = atb_y;
			this.atb_z = atb_z;
			this.btb = btb;
			this.massPoint_x = massPoint_x;
			this.massPoint_y = massPoint_y;
			this.massPoint_z = massPoint_z;
			this.numPoints = numPoints;
		}

		public void Set(ref QEFData rhs)
		{
			Set(rhs.ata_00, rhs.ata_01, rhs.ata_02, rhs.ata_11, rhs.ata_12,
				  rhs.ata_22, rhs.atb_x, rhs.atb_y, rhs.atb_z, rhs.btb,
				  rhs.massPoint_x, rhs.massPoint_y, rhs.massPoint_z,
				  rhs.numPoints);
		}
	}
	public class QEFSolver
	{
		public QEFData data;
		public SMat3 ata;
		public Vector3 atb, x;
		public Vector3 MassPoint { get; set; }
		public bool hasSolution;
		public float last_error;

		public QEFSolver()
		{
			data = new QEFData();
			ata = new SMat3();
			atb = new Vector3();
			MassPoint = new Vector3();
			x = new Vector3();
			hasSolution = false;
		}

		public void Add(Vector3 p, Vector3 n)
		{
			this.hasSolution = false;
			n.Normalize();
			this.data.ata_00 += n.x * n.x;
			this.data.ata_01 += n.x * n.y;
			this.data.ata_02 += n.x * n.z;
			this.data.ata_11 += n.y * n.y;
			this.data.ata_12 += n.y * n.z;
			this.data.ata_22 += n.z * n.z;
			float dot = n.x * p.x + n.y * p.y + n.z * p.z;
			this.data.atb_x += dot * n.x;
			this.data.atb_y += dot * n.y;
			this.data.atb_z += dot * n.z;
			this.data.btb += dot * dot;
			this.data.massPoint_x += p.x;
			this.data.massPoint_y += p.y;
			this.data.massPoint_z += p.z;
			++this.data.numPoints;
		}

		public void Add(ref QEFData rhs)
		{
			hasSolution = false;
			data.Add(ref rhs);
		}

		public float GetError()
		{
			//if (!hasSolution)
			//	return 65535;
			return GetError(x);
		}

		public float GetError(Vector3 pos)
		{
			if (!this.hasSolution)
			{
				this.SetAta();
				this.SetAtb();
			}

			Vector3 atax = this.ata.Vmul(pos);
			last_error = Vector3.Dot(pos, atax) - 2.0f * Vector3.Dot(pos, this.atb) + this.data.btb;
			//last_error *= last_error;
			if (float.IsNaN(last_error))
				last_error = 10000;

			/*float total = 0;
			for (int i = 0; i < this.data.numPoints; i++)
			{
				Vector3 d = pos - Intersections[i];
				float dot = Normals[i].x * d.x + Normals[i].y * d.y + Normals[i].z * d.z;
				total += dot * dot;
			}
			last_error = total;*/

			//Debug.Assert(last_error >= 0.0f);
			return last_error;
		}

		public void Reset()
		{
		}

		public Vector3 Solve(float svd_tol, int svd_sweeps, float pinv_tol)
		{
			if (this.data.numPoints == 0)
			{
				throw new Exception("...");
			}

			MassPoint = new Vector3(this.data.massPoint_x, this.data.massPoint_y,
								this.data.massPoint_z);
			MassPoint /= (float)this.data.numPoints;
			this.SetAta();
			this.SetAtb();
			Vector3 tmpv = ata.Vmul(MassPoint);
			atb = atb - tmpv;
			x = Vector3.zero;
			float result = SVD.SolveSymmetric(this.ata, this.atb, ref this.x, svd_tol, svd_sweeps, pinv_tol);
			if (float.IsNaN(result))
			{
				x = MassPoint;

			}
			else
				x += MassPoint;
			last_error = result;
			UnityEngine.Debug.Assert(result >= 0.0f);
			this.SetAtb();
			this.hasSolution = true;

			return x;
		}

		private void SetAta()
		{
			ata.SetSymmetric(this.data.ata_00, this.data.ata_01,
							   this.data.ata_02, this.data.ata_11, this.data.ata_12,
							   this.data.ata_22);
		}

		private void SetAtb()
		{
			atb = new Vector3(this.data.atb_x, this.data.atb_y, this.data.atb_z);
		}
	}
}
