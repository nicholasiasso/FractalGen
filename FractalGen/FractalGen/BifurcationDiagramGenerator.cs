using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace FractalGen
{
    public class BifurcationDiagramGenerator
    {
        // Image Settings
        string outputPath = "C:\\Temp\\BifurcationDiagram.png";

        // Generation Settings
        int size;
        int verticalDiffusion;
        int columnsPerPixel;
        int preiterations;
        double lambdaMin;
        double lambdaMax;

        // Performance Settings
        int concurrentTasks;

        public BifurcationDiagramGenerator(
            string outputFilepath,
            int imageSize = 1000,
            int verticalDiffusion = 1000,
            int columnsPerPixel = 3,
            int preIterations = 1000,
            double lambdaMin = 3.4d,
            double lambdaMax = 4.0,
            int concurrentTasks = 16)
        {
            this.outputPath = outputFilepath;
            this.size = imageSize;
            this.verticalDiffusion = verticalDiffusion;
            this.columnsPerPixel = columnsPerPixel;
            this.preiterations = preIterations;
            this.lambdaMin = lambdaMin;
            this.lambdaMax = lambdaMax;
            this.concurrentTasks = concurrentTasks;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
        public async Task RenderBifurcationDiagram()
        {
            int width = this.size;
            int height = this.size;
            int pointsPerColumn = height * verticalDiffusion;
            int maxPointsPerPixel = 10 * verticalDiffusion * columnsPerPixel;

            // Initialize PixelPoints
            List<List<int>> pixelPoints = new List<List<int>>(width);
            for (int x = 0; x < width; x++)
            {
                pixelPoints.Add(new List<int>(height));
                for (int y = 0; y < height; y++)
                {
                    pixelPoints[x].Add(0);
                }
            }

            var lambdaStep = (lambdaMax - lambdaMin) / (width * columnsPerPixel);
            var currentLambda = lambdaMin;
            var currentColumn = 0;

            Task[] tasks = new Task[concurrentTasks];
            // Prepopulate Tasks
            for (int i = 0; i < concurrentTasks; i++)
            {
                tasks[i] = WritePointsForPixelColumn(currentColumn++, currentLambda, lambdaStep, columnsPerPixel, preiterations, pointsPerColumn, pixelPoints);
                currentLambda += lambdaStep * columnsPerPixel;
            }

            // Keep Task Queue Filled
            while(currentColumn < width)
            {
                Task completedTask = await Task.WhenAny(tasks);
                var completedIndex = Array.IndexOf(tasks, completedTask);
                tasks[completedIndex] = WritePointsForPixelColumn(currentColumn++, currentLambda, lambdaStep, columnsPerPixel, preiterations, pointsPerColumn, pixelPoints);
                currentLambda += lambdaStep * columnsPerPixel;
            }

            // Wait for remainint Tasks to complete
            await Task.WhenAll(tasks);

            Console.WriteLine("Creating PNG...");

            Bitmap bmp = new Bitmap(
                width: width,
                height: height);
            for (int i = 0; i < width * height; i++)
            {
                bmp.SetPixel(i % width, i / width, Color.Black);
            }

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    int points = Math.Min(pixelPoints[x][height - y - 1], maxPointsPerPixel);
                    if (points == 0)
                    {
                        continue;
                    }

                    var intensity = (points * 255) / maxPointsPerPixel;
                    var argb = ((uint)intensity << 16) + ((uint)intensity << 8) + (uint)intensity;
                    bmp.SetPixel(x, y, Color.FromArgb(Color.Black.ToArgb() + unchecked((int)argb)));
                }
            }

            bmp.Save(outputPath, ImageFormat.Png);
        }

        private Task WritePointsForPixelColumn(int column, double lambda, double lambdaStep, int steps, int preiterations, int iterations, List<List<int>> points)
        {
            return Task.Run(
                () =>
                {
                    Console.WriteLine($"Calculating X:{column}, Lambda:{lambda}");
                    for (int step = 0; step < steps; step++)
                    {
                        double currentLambda = lambda + step * lambdaStep;

                        // Pre-iterate
                        double x = 0.5d;
                        for (int i = 0; i < preiterations; i++)
                        {
                            x = LogMap(x, currentLambda);
                        }

                        // Write Points
                        for (int i = 0; i < iterations; i++)
                        {
                            x = LogMap(x, currentLambda);
                            int yCoord = (int)Remap(x, 0, 1, 0, this.size);
                            points[column][yCoord]++;
                        }
                    }
                });
        }

        private static double LogMap(double x, double lambda)
        {
            return lambda * x * (1d - x);
        }

        private static double Remap(double value, double fromMin, double fromMax, double toMin, double toMax)
        {
            double slope = (toMax - toMin) / (fromMax - fromMin);
            return toMin + slope * (value - fromMin);
        }
    }
}
