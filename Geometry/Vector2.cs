using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeneticTrees
{
	struct Vector2
	{
		public Vector2(double x, double z)
		{
			X = x;
			Z = z;
		}
		public double X;
		public double Z;
		static public Vector2 Zero = new Vector2(0, 0);

		static public Vector2 operator *(Vector2 vector, double scalar)
		{
			return new Vector2(vector.X * scalar, vector.Z * scalar);
		}
		static public Vector2 operator /(Vector2 vector, double scalar)
		{
			return new Vector2(vector.X / scalar, vector.Z / scalar);
		}
		static public Vector2 operator +(Vector2 a, Vector2 b)
		{
			return new Vector2(a.X + b.X, a.Z + b.Z);
		}
		static public Vector2 operator -(Vector2 a, Vector2 b)
		{
			return new Vector2(a.X - b.X, a.Z - b.Z);
		}
		public double LengthSquared()
		{
			return X * X + Z * Z;
		}
		public double Length()
		{
			return Math.Sqrt(X * X + Z * Z);
		}
		public double Dot(Vector3 b)
		{
			return X * b.X + Z * b.Z;
		}
		public double Dot(Vector2 b)
		{
			return X * b.X + Z * b.Z;
		}
		public Vector2 Normalized()
		{
			return this / Length();
		}
	}
}
