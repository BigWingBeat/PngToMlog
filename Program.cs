using System.Globalization;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace PngToMlog
{
	struct Rect(int x1, int y1, int x2, int y2)
	{
		public int x1 = x1;
		public int y1 = y1;
		public int x2 = x2;
		public int y2 = y2;
	}

	enum ScalingMode
	{
		Scale,
		Letterbox,
		Crop
	}

	enum ParseResult
	{
		Success,
		Failure,
		FlagNotPresent
	}

	abstract class Flag<T>
	{
		public required HashSet<string> keys;
		public required string help;

		public abstract ParseResult Parse(string[] args, out T? value);

		public void PrintHelp()
		{
			Console.WriteLine();
			Console.WriteLine($"\t{string.Join(' ', keys)}");
			Console.WriteLine($"\t\t{help}");
		}
	}

	class ParsedFlag<T> : Flag<T> where T : IParsable<T>
	{
		public override ParseResult Parse(string[] args, out T? value)
		{
			bool found = false;
			foreach (string key in args)
			{
				if (found)
				{
					// It's the next arg, parse it to the flag value
					if (T.TryParse(key, CultureInfo.InvariantCulture, out value))
					{
						return ParseResult.Success;
					}
					else
					{
						return ParseResult.Failure;
					}
				}
				else if (keys.Contains(key))
				{
					// Found flag key, next arg will be flag value
					found = true;
				}

			}

			// Flag not present
			value = default;
			return ParseResult.FlagNotPresent;
		}
	}

	class BoolFlag : Flag<bool>
	{
		public override ParseResult Parse(string[] args, out bool value)
		{
			foreach (string key in args)
			{
				if (keys.Contains(key))
				{
					value = true;
					return ParseResult.Success;
				}
			}
			value = false;
			return ParseResult.FlagNotPresent;
		}
	}

	class EnumFlag<T> : Flag<T> where T : struct
	{
		public override ParseResult Parse(string[] args, out T value)
		{
			bool found = false;
			foreach (string key in args)
			{
				if (found)
				{
					// It's the next arg, parse it to the flag value
					if (Enum.TryParse(key, true, out value))
					{
						return ParseResult.Success;
					}
					else
					{
						return ParseResult.Failure;
					}
				}
				else if (keys.Contains(key))
				{
					// Found flag key, next arg will be flag value
					found = true;
				}

			}

			// Flag not present
			value = default;
			return ParseResult.FlagNotPresent;
		}
	}


	class Program
	{
		static BoolFlag help = new()
		{
			keys = ["-h", "--help"],
			help = "Display this help message",
		};

		static BoolFlag version = new()
		{
			keys = ["-v", "--version"],
			help = "Display program version"
		};

		static BoolFlag small = new()
		{
			keys = ["-s", "--small"],
			help = "Output for Small Logic Display (80x80 resolution)"
		};

		static BoolFlag large = new()
		{
			keys = ["-l", "--large"],
			help = "Output for Large Logic Display (176x176 resolution)"
		};

		static ParsedFlag<int> ipp = new()
		{
			keys = ["-i", "-ipp"],
			help = "Set instructions-per-processor. Defaults to 990 if unset"
		};

		static EnumFlag<ScalingMode> mode = new()
		{
			keys = ["-m", "--mode"],
			help = """
Set scaling mode for the image: Possible options are:

		scale
			Scales the image on each axis to exactly fit the display resolution
			Will result in visible squashing / stretching for images that are
			significantly larger / smaller than the display on each axis

		letterbox
			Scale the entire image uniformly until it fits within the display resolution
			Will result in blank bars above & below the image for landscape images,
			or either side of the image for portrait images.
			Images with an extreme aspect ratio will be very thin and likely hard to make out

		crop
			Do not scale the image in any way.
			Images larger than the display will be cut off at the sides,
			and images smaller than the display will result in blank bars

		If no mode is specified, it defaults to 'scale'
"""
		};

		public static void Main(string[] args)
		{
			if (args.Length == 0 || help.Parse(args, out _) == ParseResult.Success)
			{
				Console.WriteLine("PngToMlog version 3, BigWingBeat fork");
				Console.WriteLine();
				Console.WriteLine("Program to convert images into mlog commands");
				Console.WriteLine();
				Console.WriteLine("Usage: pngtomlog <flags> <image path>");
				Console.WriteLine();
				Console.WriteLine("Available flags:");
				help.PrintHelp();
				version.PrintHelp();
				small.PrintHelp();
				large.PrintHelp();
				ipp.PrintHelp();
				mode.PrintHelp();
				return;
			}

			if (version.Parse(args, out _) == ParseResult.Success)
			{
				Console.WriteLine("PngToMlog version 3, BigWingBeat fork");
				return;
			}

			int displayResolution = 0;

			if (small.Parse(args, out bool smallSpecified) == ParseResult.Success)
			{
				displayResolution = 80;
			}

			if (large.Parse(args, out bool largeSpecified) == ParseResult.Success)
			{
				displayResolution = 176;
			}

			if (smallSpecified && largeSpecified)
			{
				Console.WriteLine("Error: --small and --large flags are mutually exclusive");
				return;
			}

			if (!smallSpecified && !largeSpecified)
			{
				Console.WriteLine("Error: You must specify either --small or --large");
				return;
			}

			switch (ipp.Parse(args, out int instructionsPerProcessor))
			{
				case ParseResult.Failure:
					Console.WriteLine("Error: Invalid argument passed to --ipp");
					return;
				case ParseResult.FlagNotPresent:
					Console.WriteLine("Defaulting to 990 ipp");
					instructionsPerProcessor = 990;
					break;
			}

			switch (mode.Parse(args, out ScalingMode scalingMode))
			{
				case ParseResult.Failure:
					Console.WriteLine("Error: Invalid argument passed to --mode");
					return;
				case ParseResult.FlagNotPresent:
					Console.WriteLine("Defaulting to 'scale' scaling mode");
					scalingMode = ScalingMode.Scale;
					break;
			}

			string filePath = args.Last();

			// Read file and parse into image first, to fail early if the file doesn't exist or is malformed
			Image<Rgba32> image = Image.Load<Rgba32>(File.ReadAllBytes(filePath));

			// Create image
			Run(image, displayResolution, scalingMode, instructionsPerProcessor);
		}

		public static void Run(Image<Rgba32> image, float displayResolution, ScalingMode mode, int instructionsPerProcessor)
		{
			string outText = "";

			//Clipboard.SetText(outText);
			//color 0 0 0 255
			//draw rect 0 0 1 1 0 0
			//drawflush display1

			//draw color r g b 255 0 0
			//draw rect x y 1 1 0 0
			//drawflush display1

			//set width and height vars
			int width = image.Width;
			int height = image.Height;

			float xScale = 0;
			float yScale = 0;

			switch (mode)
			{
				case ScalingMode.Scale:
					xScale = displayResolution / width;
					yScale = displayResolution / height;
					break;
				case ScalingMode.Letterbox:
					float scale = displayResolution / Math.Max(width, height);
					xScale = scale;
					yScale = scale;
					break;
				case ScalingMode.Crop:
					xScale = 1;
					yScale = 1;
					break;
			}

			//resize image and flip it vertically to compensate for differences in how lists and displays work
			image.Mutate(
				ctx => ctx.Flip(FlipMode.Vertical)
			);

			//init vars
			int processorCounter = 0;
			int i = 0;
			int i1 = 0;

			List<string> colors = [];
			HashSet<Point> usedCoords = [];
			HashSet<Point> usedImagePoints = [];
			Dictionary<string, List<Rect>> imageCoords = [];

			// create list of colors
			for (int _ = 0; _ < image.Width; _++)
			{
				for (int __ = 0; __ < image.Height; __++)
				{
					colors.Add(image[_, __].ToString());
				}
			}

			//deduplicate colors
			colors = colors.Distinct().ToList();

			//construct dictionary, mapping colors to lists. each list contains lists defining top left and bottom right corners of the rectangle
			for (int _ = 0; _ < colors.Count; _++)
			{
				imageCoords.Add(colors[_],
					[]
				);
			}


			// Scan for rectangles and add them to the final rectangle list
			for (int x = 0; x < width; x++)
			{
				for (int y = 0; y < height; y++)
				{
					if (!usedImagePoints.Contains(new Point(x, y)))
					{
						string currentColor = image[x, y].ToString();
						int firsty = y;
						int newx = x;
						List<int> xs = [];

						while (newx + 1 < width && image[newx + 1, y].ToString() == currentColor)
						{
							newx++;
						}

						xs.Add(newx);
						newx = x;

						while (y + 1 < height && image[x, y + 1].ToString() == currentColor)
						{
							y++;

							while (newx + 1 < width && image[newx + 1, y].ToString() == currentColor)
							{
								newx++;
							}
							xs.Add(newx);
							newx = x;
						}

						imageCoords[currentColor].Add(new Rect(x, firsty, xs.Min(), y));

						for (int x1 = x; x1 <= xs.Min(); x1++)
						{
							for (int y1 = firsty; y1 <= y; y1++)
							{
								usedImagePoints.Add(new Point(x1, y1));
							}
						}
					}
				}
			}

			// iterate over each color
			foreach (KeyValuePair<string, List<Rect>> entryBuf in imageCoords)
			{
				//deduplicate just in case
				KeyValuePair<string, List<Rect>> entry = new(entryBuf.Key, entryBuf.Value.Distinct().ToList());

				//initialize section of color
				string[] color = entry.Key.Replace("Rgba32(", "").Replace(")", "").Replace(" ", "").Split(',', ' ');
				outText += string.Format(@"draw color {0} {1} {2} {3} 0 0
", color[0], color[1], color[2], color[3]);
				i1++;
				i++;

				//iterate over rects
				foreach (Rect coord in entry.Value)
				{
					//get XYs for calculations
					int x1 = coord.x1;
					int y1 = coord.y1;
					int x2 = coord.x2;
					int y2 = coord.y2;

					if (true /*!usedCoords.Contains(new Point(coord[0], coord[1]))*/)
					{
						//usedCoords.Add(new Point(coord[0], coord[1]));

						// add the rect
						outText += string.Format(@"draw rect {0} {1} {2} {3} 0 0
", (int)float.Round(x1 * xScale), (int)float.Round(y1 * yScale), (int)float.Round((x2 - x1 + 1) * xScale), (int)float.Round((y2 - y1 + 1) * yScale));

						// increase counters
						i1 += 1;
						i += 1;

						// if enough draw commands have been sent, add a draw buffer so we dont lose data, and set the color again
						if (i >= 250)
						{
							i = 0;
							outText += @"drawflush display1
";
							outText += string.Format(@"draw color {0} {1} {2} {3} 0 0
", color[0], color[1], color[2], color[3]);
							i1++;
							i1++;
						}

						//if the instructions per processor is reached then set the clipboard and prepare for next processor
						if (i1 > instructionsPerProcessor)
						{
							i1 = 0;
							outText += string.Format(@"drawflush display1
");

							File.WriteAllText($"{processorCounter}.txt", outText);
							Console.WriteLine($"Written to {processorCounter}.txt");
							processorCounter++;

							outText = "";
							outText += string.Format(@"draw color {0} {1} {2} {3} 0 0
", color[0], color[1], color[2], color[3]);
						}
					}
				}
			}

			//when we reach the end, draw what we have and then exit
			outText += @"drawflush display1
";

			File.WriteAllText($"{processorCounter}.txt", outText);
			Console.WriteLine($"Written to {processorCounter}.txt");
			Console.WriteLine("Done");
			Console.WriteLine();
			Console.WriteLine("Files named 0.txt, 1.txt etc. contain mlog instructions for each processor");
		}
	}
}

