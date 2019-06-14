using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ParkingProject.Geometry;
namespace ParkingProject
{
	public partial class Form1 : Form
	{
		public Form1()
		{
			InitializeComponent();
			//panel1.BackgroundImage = Transform(Empty,);
			//Matrix best = MinimizeDifference(Scale(empty,0.5), Scale(empty2,0.5));
			//var after = Difference(Crop(empty, cropPercent), Crop(Transform(empty2, best), cropPercent));
			Baseline = Scale(Bitmap.FromFile("./data/baseline.jpg") as Bitmap, 256);
			Masks.Add(Difference(Baseline, Scale(Bitmap.FromFile("./data/mask_left.jpg") as Bitmap, 256)));
			Masks.Add(Difference(Baseline, Scale(Bitmap.FromFile("./data/mask_middle.jpg") as Bitmap, 256)));
			Masks.Add(Difference(Baseline, Scale(Bitmap.FromFile("./data/mask_right.jpg") as Bitmap, 256)));

			List<Bitmap> source = new List<Bitmap>();
			source.Add(Scale(Bitmap.FromFile("./data/car_left.jpg") as Bitmap, 256));
			source.Add(Scale(Bitmap.FromFile("./data/car_middle.jpg") as Bitmap, 256));
			source.Add(Scale(Bitmap.FromFile("./data/car_right.jpg") as Bitmap, 256));

			foreach (Bitmap s in source)
			{
				flowLayoutPanel1.Controls.Add(CreatePanel(s));

				int matchIndex = FindMatchingMask(s);
				if (matchIndex != -1)
				{
					var mask = Masks[matchIndex];
					flowLayoutPanel1.Controls.Add(CreatePanel(mask));
				}
			}
			//Matrix inverse;
			//if (new Matrix(new double[,]
			//	{
			//		{.15344,-.80841,125},
			//		{-0.00921,0.1,3 },
			//		{-0.00014,-.00538,1 }
			//	}).Inverse(out inverse))
			//{
			//	flowLayoutPanel1.Controls.Add(CreatePanel(Transform(Baseline, inverse)));
			//}
			//var test = Scale(Bitmap.FromFile("./data/empty.bmp") as Bitmap, 256);
			//var baseline = Scale(Bitmap.FromFile("./data/occupied.bmp") as Bitmap, 256);

			//Matrix result = MinimizeDifference(Scale(baseline, 64), Scale(test, 64));

			//flowLayoutPanel1.Controls.Add(CreatePanel(Difference(baseline, Transform(test, result))));

			//Matrix product = inverse * test;
			//backgroundWorker1.DoWork += BackgroundWorker1_DoWork;
			//backgroundWorker1.ProgressChanged += BackgroundWorker1_ProgressChanged;
			//backgroundWorker1.RunWorkerAsync();
			var baseline = Scale(Bitmap.FromFile("./data/Feed/20190613_215917.jpg") as Bitmap, 256);
			flowLayoutPanel1.Controls.Add(CreatePanel(baseline));
			var heightMap = baseline.GreenShift();
			var points = heightMap.FindIslands(4, 10);
			var temp = points[2];
			points.Remove(temp);
			points.Add(temp);
			Matrix a = new Matrix(new double[,]
			{
				{ points[0].X,points[1].X,points[2].X},
				{points[0].Y,points[1].Y,points[2].Y },
				{1,1,1 }
			});
			Matrix aInverse;
			if (a.Inverse(out aInverse))
			{
				Vector3 co = aInverse * new Vector3(points[3].X, points[3].Y, 1);
				a.ScaleColumn(0, co.X);
				a.ScaleColumn(1, co.Y);
				a.ScaleColumn(2, co.Z);
			}
			a.Inverse(out aInverse);

			var destPoints = new List<Point>
			{
				new Point(0,0),
				new Point(0, heightMap.Height-1),
				new Point(heightMap.Width-1,heightMap.Height-1),
				new Point(heightMap.Width-1,0),
			};
			Matrix b = new Matrix(new double[,]
			{
				{ destPoints[0].X,destPoints[1].X,destPoints[2].X},
				{destPoints[0].Y,destPoints[1].Y,destPoints[2].Y },
				{1,1,1 }
			});
			Matrix bInverse;
			if (b.Inverse(out bInverse))
			{
				Vector3 co = bInverse * new Vector3(destPoints[3].X, destPoints[3].Y, 1);
				b.ScaleColumn(0, co.X);
				b.ScaleColumn(1, co.Y);
				b.ScaleColumn(2, co.Z);
			}
			Matrix c = b * aInverse;
			Matrix transform;
			c.Inverse(out transform);
			Bitmap map = new Bitmap(heightMap.Width, heightMap.Height);
			map.Fill(Color.Blue);
			flowLayoutPanel1.Controls.Add(CreatePanel(Transform(baseline, transform)));
		}

		private void BackgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
		{
			progressBar1.Value = e.ProgressPercentage;
			flowLayoutPanel1.Controls.Add(e.UserState as Label);
		}

		private void BackgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
		{
			Classification.ParkingSpaceClassifier classifier = new Classification.ParkingSpaceClassifier();
			int i = 0;
			foreach (var error in classifier.Error)
			{
				Label label = new Label();
				label.Text = $"Epoch {error.Item1}: {error.Item2}%";
				i++;
				backgroundWorker1.ReportProgress((int)(100 * (double)i / (double)classifier.Iterations), label);
				classifier.Save();
			}
			
		}

		private Panel CreatePanel(Bitmap source)
		{
			Panel panel = new Panel();
			panel.Width = source.Width;
			panel.Height = source.Height;
			panel.BackgroundImage = source;
			return panel;
		}
		private Bitmap Baseline { get; set; }
		private List<Bitmap> Masks { get; set; } = new List<Bitmap>();
		private const double cropPercent = 1;
		private int FindMatchingMask(Bitmap source)
		{
			var diff = Difference(Baseline, source);
			int bestOverlapIndex = -1;
			double bestMatch = 0;
			for (int i = 0; i < Masks.Count; i++)
			{
				var mask = Masks[i];
				double match = (double)Overlap(mask, diff) / (double)Sum(mask);
				if (match > bestMatch)
				{
					bestOverlapIndex = i;
					bestMatch = match;
				}

			}
			return bestOverlapIndex;
		}
		private Bitmap Difference(Bitmap a, Bitmap b)
		{
			Bitmap result = new Bitmap(a.Width, a.Height);
			for (int x = 0; x < result.Width; x++)
			{
				for (int y = 0; y < result.Height; y++)
				{
					Color colorA = a.GetPixel(x, y);
					Color colorB = b.GetPixel(x, y);
					result.SetPixel(x, y, Color.FromArgb(Math.Abs(colorA.R - colorB.R),Math.Abs(colorA.G - colorB.G),Math.Abs(colorA.B - colorB.B)));
					
				}
			}
			return result;
		}
		private Bitmap Fill(int width, int height, Color color)
		{
			Bitmap result = new Bitmap(width,height);
			for (int x = 0; x < result.Width; x++)
			{
				for (int y = 0; y < result.Height; y++)
				{
					result.SetPixel(x, y, color);
				}
			}
			return result;
		}
		private int Overlap(Bitmap a, Bitmap b)
		{
			int overlap = 0;
			for (int x = 0; x < a.Width; x++)
			{
				for (int y = 0; y < a.Height; y++)
				{
					var pixelA = a.GetPixel(x, y);
					var pixelB = b.GetPixel(x, y);
					overlap += Math.Min(pixelA.R, pixelB.R) + Math.Min(pixelA.G, pixelB.G) + Math.Min(pixelA.B, pixelB.B);
				}
			}
			return overlap;
		}
		private Bitmap Crop(Bitmap source,double percent)
		{
			int width = (int)(source.Width * percent);
			int height = (int)(source.Height * percent);
			return source.Clone(new Rectangle((source.Width - width) / 2, (source.Height - height) / 2, width, height),source.PixelFormat);
		}
		private Bitmap Scale(Bitmap source, double scale)
		{
			Bitmap result = new Bitmap((int)(source.Width * scale), (int)(source.Height * scale));
			for (int x = 0; x < result.Width; x++)
			{
				for (int y = 0; y < result.Height; y++)
				{
					result.SetPixel(x, y, source.GetPixel((int)Math.Max(0, Math.Min(source.Width,x / scale)), (int)Math.Max(0, Math.Min(source.Height, y / scale))));
				}
			}
			return result;
		}
		private Bitmap Scale(Bitmap source, int width)
		{
			double scale = (double)width / (double)source.Width;
			return Scale(source, scale);
		}
		private int Sum(Bitmap source)
		{
			int sum = 0;
			for (int x = 0; x < source.Width; x++)
			{
				for (int y = 0; y < source.Height; y++)
				{
					var pixel = source.GetPixel(x, y);
					sum += pixel.R + pixel.G + pixel.B;
				}
			}
			return sum;
		}
		private Bitmap Transform(Bitmap source, Matrix matrix)
		{
			Bitmap result = new Bitmap(source.Width, source.Height);
			for (int x = 0; x < result.Width; x++)
			{
				for (int y = 0; y < result.Height; y++)
				{
					// Sample source
					Vector3 sourcePoint = matrix * new Vector3((double)x, (double)y ,1);
					int sx = (int)Math.Round(sourcePoint.X / sourcePoint.Z);
					int sy = (int)Math.Round(sourcePoint.Y / sourcePoint.Z);
					if (sx >= 0 && sx < source.Width && sy >= 0 && sy < source.Height)
						result.SetPixel(x, y, source.GetPixel(sx, sy));
				}
			}
			//for (int x = 0; x < result.Width; x++)
			//{
			//	for (int y = 0; y < result.Height; y++)
			//	{
			//		// Sample source
			//		Vector3 sourcePoint = matrix * new Vector3(x, y, 0);
			//		int sx = (int)Math.Round(sourcePoint.X);
			//		int sy = (int)Math.Round(sourcePoint.Y);
			//		if (sx >= 0 && sx < source.Width && sy >= 0 && sy < source.Height)
			//			result.SetPixel(x, y, source.GetPixel(sx, sy));
			//	}
			//}
			return result;
		}
		private Matrix MinimizeDifference(Bitmap reference, Bitmap b)
		{
			const int iterations = 25;
			double[] deviations = new double[] { 10, -10 };
			
			Bitmap referenceCropped = Crop(reference, cropPercent);
			Matrix best = Matrix.Identity;
			int bestSum = Int32.MaxValue;
			for (int i = 0; i < iterations; i++)
			{
				int oldBest = bestSum;
				for (int x = 0; x < 3; x++)
				{
					for (int y = 0; y < 3; y++)
					{
						if (!(x == 2 && y == 2))
						{
							foreach (double deviation in deviations)
							{
								Matrix test = new Matrix(best);
								test.Set(x, y, test.Get(x, y) + deviation);
								Bitmap bPrime = Crop(Transform(b, test), cropPercent);
								int sum = Sum(Difference(referenceCropped, bPrime));
								if (sum <= bestSum)
								{
									best = test;
									bestSum = sum;
								}
							}
						}
					}
				}
				if (oldBest == bestSum)
				{
					for (int d = 0; d < deviations.Length; d++)
					{
						deviations[d] *= 0.5;
					}
				}
				//else
				//{
				//	for (int d = 0; d < deviations.Length; d++)
				//	{
				//		deviations[d] *= 2;
				//	}
				//}
			}
			return best;
		}
		private void Posturize(Bitmap source, int threshold = 382)
		{
			for (int x = 0; x < source.Width; x++)
			{
				for (int y = 0; y < source.Height; y++)
				{
					var pixel = source.GetPixel(x, y);
					if (pixel.R + pixel.G + pixel.B > threshold)
					{
						source.SetPixel(x, y, Color.White);
					} else
					{
						source.SetPixel(x, y, Color.Black);
					}
				}
			}
		}
		private int Clamp(int i,int max)
		{
			return Math.Max(0, Math.Min(i, max));
		}
		private Bitmap Melt(Bitmap source, int iterations)
		{
			Bitmap result = new Bitmap(source);
			for (int i = 0; i < iterations; i++)
			{
				Bitmap temp = new Bitmap(result);
				for (int x = 0; x < source.Width; x++)
				{
					for (int y = 0; y < source.Height; y++)
					{
						if (result.GetPixel(x, y).R > 0)
						{
							int neighbors = 0;
							if (result.GetPixel(Clamp(x - 1, result.Width-1), y).R > 0)
								neighbors++;
							if (result.GetPixel(Clamp(x + 1, result.Width-1), y).R > 0)
								neighbors++;
							if (result.GetPixel(x, Clamp(y - 1, result.Height-1)).R > 0)
								neighbors++;
							if (result.GetPixel(x, Clamp(y + 1, result.Height-1)).R > 0)
								neighbors++;

							if (neighbors < 4)
							{
								temp.SetPixel(x, y, Color.Black);
							}
						}
					}
				}
				result = temp;
			}
			return result;
		}
		Bitmap Graph(IEnumerable<double> points, int width, int height)
		{
			double max = points.Max();
			double min = points.Min();
			Bitmap result = new Bitmap(width,height);
			for (int x = 0; x < result.Width; x++)
			{
				double value = points.ElementAt((int)(((double)x / result.Width) * points.Count()));
				value = ((value - min) / (max - min)) * height;
				result.SetPixel(x, result.Height - Math.Max(1,Math.Min(result.Height,(int)value)), Color.Green);
			}
			return result;
		}
	}
}
