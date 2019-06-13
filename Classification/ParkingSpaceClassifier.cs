using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.IO;
using Neural.NET;

namespace ParkingProject.Classification
{
	class ParkingSpaceClassifier
	{
		public ParkingSpaceClassifier()
		{
			//int dataWidth = 4;
			//int dataHeight = 4;
			//int inputSize = dataWidth * dataHeight;
			//Net = new Network(inputSize, new[] { 20, 10 }, 1);

			//NetworkTrainer trainer = new NetworkTrainer(Net);
			////--------------------------------------------------
			//// Training data
			//List<Tuple<Bitmap, Bitmap>> training = new List<Tuple<Bitmap, Bitmap>>();
			//int trainingSize = 1000;
			//for (int i = 0; i < trainingSize;i++)
			//	training.Add(GenerateLabeled(dataWidth, dataHeight));
			////--------------------------------------------------
			//// Test data
			//List<Tuple<Bitmap, Bitmap>> test = new List<Tuple<Bitmap, Bitmap>>();
			//int testSize = 200;
			//for (int i = 0; i < testSize; i++)
			//	test.Add(GenerateLabeled(dataWidth, dataHeight));

			//Error = trainer.StochasticGradientDescent(trainingSize, training.Select(t => Flatten(t)).ToArray(), 1000, 100, .05, test.Select(t => Flatten(t)).ToArray());

			if (File.Exists("./data/MNIST_weights.dat"))
			{
				Load();
			}
			else
			{
				Net = new Network(784, new[] { 100, 50 }, 10);
			}

			Iterations = 1000;
			NetworkTrainer trainer = new NetworkTrainer(Net);
			Tuple<double[], double[]>[] _testData = GetTestData();
			Tuple<double[], double[]>[] _trainingData = GetTrainingData();
			Error = trainer.StochasticGradientDescent(10000, _trainingData, Iterations, 100, .05, _testData);
		}
		public int Iterations { get; set; }
		Network Net { get; set; }
		Random RNG { get; set; } = new Random();
		public IEnumerable<Tuple<int, double?>> Error { get; set; }
		public void Save()
		{
			using (var stream = File.OpenWrite("./data/MNIST_weights.dat"))
			{
				var formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
				formatter.Serialize(stream, Net);
			}
		}
		public void Load()
		{
			using (var stream = File.OpenRead("./data/MNIST_weights.dat"))
			{
				var formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
				Net = (Network)formatter.Deserialize(stream);
			}
		}
		Tuple<Bitmap,Bitmap> GenerateLabeled(int width, int height)
		{
			Bitmap data = new Bitmap(width, height);
			int sum = 0;
			for (int x = 0; x < width; x++)
			{
				for (int y = 0; y < height; y++)
				{
					if (RNG.NextDouble() < 0.5)
					{
						data.SetPixel(x, y, Color.White);
						sum++;
					}
				}
			}
			Bitmap label = new Bitmap(1,1);
			if (sum > width * height / 2)
			{
				label.SetPixel(0, 0, Color.White);
			}
			return new Tuple<Bitmap, Bitmap>(data, label);
		}
		double[] Flatten(Bitmap bitmap)
		{
			double[] data = new double[bitmap.Width * bitmap.Height];
			int i = 0;
			for (int x = 0; x < bitmap.Width; x++)
			{
				for (int y = 0; y < bitmap.Height; y++)
				{
					data[i] = (bitmap.GetPixel(x, y).R + bitmap.GetPixel(x, y).G + bitmap.GetPixel(x, y).B) / 382.0;
					i++;
				}
			}
			return data;
		}
		Tuple<double[],double[]> Flatten(Tuple<Bitmap,Bitmap> labeled)
		{
			return new Tuple<double[], double[]>(Flatten(labeled.Item1), Flatten(labeled.Item2));
		}
		double[][] ImportLabel(string path)
		{
			
			using (BinaryReader reader = new BinaryReader(File.Open(path, FileMode.Open)))
			{
				int magic = reader.ReadInt32BE();
				int size = reader.ReadInt32BE();
				double[][] labels = new double[size][];
				for (int i = 0; i < size; i++)
				{
					var label = new double[10];
					byte value = reader.ReadByte();
					label[value] = 1.0;
					labels[i] = label;
				}
				return labels;
			}
		}
		double[][] ImportImages(string path)
		{
			using (BinaryReader reader = new BinaryReader(File.Open(path, FileMode.Open),Encoding.Unicode))
			{
				int magic = reader.ReadInt32BE();
				int size = reader.ReadInt32BE();
				int width = reader.ReadInt32BE();
				int height = reader.ReadInt32BE();
				double[][] images = new double[size][];
				for (int i = 0; i < size; i++)
				{
					double[] image = new double[width * height];
					for (int pixelIndex = 0; pixelIndex < image.Length; pixelIndex++)
					{
						image[pixelIndex] = (double)reader.ReadByte() / 255.0;
					}
					images[i] = image;
				}
				return images;
			}
		}
		Tuple<double[], double[]>[] ZipData(double[][] images, double[][] labels)
		{
			return images.Zip(labels,(i,j) => new Tuple<double[], double[]>(i,j)).ToArray();
		}
		Tuple<double[], double[]>[] GetTestData()
		{
			return ZipData(ImportImages("./data/t10k-images.idx3-ubyte"), ImportLabel("./data/t10k-labels.idx1-ubyte"));
		}
		Tuple<double[], double[]>[] GetTrainingData()
		{
			return ZipData(ImportImages("./data/train-images.idx3-ubyte"), ImportLabel("./data/train-labels.idx1-ubyte"));
		}
	}
}
