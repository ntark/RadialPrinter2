using Microsoft.AspNetCore.Mvc;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.SkiaSharp;
using OxyPlot;
using System;
using System.ComponentModel;
using System.Text.RegularExpressions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace RadialPrinter.Util
{
    public class GCodeUtil
    {
        public static async Task<string> NormalizeIntoFile(string filePath)
        {
            var resPath = await Normalize(filePath);

            return resPath;
        }

        public static async Task<string> Normalize(string filePath)
        {
            string[] gcodeLines = await File.ReadAllLinesAsync(filePath);

            var coordinates = new List<(int, decimal, decimal)>();

            Regex regex = new Regex(@"^G([01])\sX([0-9\.]+)\sY([0-9\.]+)");

            foreach (var line in gcodeLines)
            {
                Match match = regex.Match(line);
                if (match.Success)
                {
                    var cut = int.Parse(match.Groups[1].Value);
                    var x = decimal.Parse(match.Groups[2].Value);
                    var y = decimal.Parse(match.Groups[3].Value);
                    coordinates.Add((cut, x, y));
                }
            }

            if (coordinates.Count == 0)
            {
                throw new Exception("empty lines");
            }

            decimal maxX = decimal.MinValue;
            decimal minX = decimal.MaxValue;
            decimal maxY = decimal.MinValue;
            decimal minY = decimal.MaxValue;

            foreach (var coord in coordinates)
            {
                if (coord.Item2 > maxX) maxX = coord.Item2;
                if (coord.Item2 < minX) minX = coord.Item2;
                if (coord.Item3 > maxY) maxY = coord.Item3;
                if (coord.Item3 < minY) minY = coord.Item3;
            }

            var scalarX = (maxX - minX) / 2m;
            var scalarY = (maxY - minY) / 2m;

            var scalar = Math.Max(scalarX, scalarY);

            var dX = scalarX / scalar;
            var dY = scalarY / scalar;

            coordinates = coordinates.Select(c => (c.Item1, (c.Item2 - minX) / scalar - dX, (c.Item3 - minY) / scalar - dY)).ToList();

            var resPath = FileHelper.GetRandomFilePath(Path.GetDirectoryName(filePath), ".ngcode");

            var fileText = string.Join("", coordinates.Select(l => $"G{l.Item1} X{l.Item2} Y{l.Item3}{Environment.NewLine}").ToList());

            await File.WriteAllTextAsync(resPath, fileText);

            return resPath;
        }

        public static async Task<string> RadToXy(string filePath, int radialSteps = -3500, int angleSteps = 27800)
        {
            string[] rcodeLines = await File.ReadAllLinesAsync(filePath);

            var radialPoints = new List<RadialPoint>();

            Regex regex = new Regex(@"^R([0-1])\s([0-9\-]+)\s([0-9\-]+)");

            foreach (var line in rcodeLines)
            {
                Match match = regex.Match(line);
                if (match.Success)
                {
                    var mode = int.Parse(match.Groups[1].Value);
                    var r = int.Parse(match.Groups[2].Value);
                    var a = int.Parse(match.Groups[3].Value);
                    radialPoints.Add(new RadialPoint(mode, r, a));
                }
            }

            var orthoPoints = new List<Point>();
            
            foreach (var radialPoint in radialPoints)
            {
                var mode = radialPoint.Mode;
                var r = (double)radialPoint.R / radialSteps;
                var a = (double)radialPoint.A / angleSteps * 2.0 * Math.PI;

                decimal x = (decimal)(r * Math.Cos(a));
                decimal y = (decimal)(r * Math.Sin(a));

                orthoPoints.Add(new Point(mode, x, y));
            }

            var resPath = FileHelper.GetRandomFilePath(Path.GetDirectoryName(filePath), ".gcode");

            var fileText = string.Join("", orthoPoints.Select(p => $"G{p.Mode} X{p.X} Y{p.Y}{Environment.NewLine}").ToList());

            await File.WriteAllTextAsync(resPath, fileText);

            return resPath;
        }
        

        public static async Task<string> XyToRad(string filePath, double maxDistance = 0.1, int radialSteps = -3500, int angleSteps = 27800)
        {
            string[] gcodeLines = await File.ReadAllLinesAsync(filePath);

            var points = new List<Point>() { new Point(0, 0, 0) };

            Regex regex = new Regex(@"^G([01])\sX([0-9\-\.]+)\sY([0-9\-\.]+)");

            foreach (var line in gcodeLines)
            {
                Match match = regex.Match(line);
                if (match.Success)
                {
                    var cut = int.Parse(match.Groups[1].Value);
                    var x = decimal.Parse(match.Groups[2].Value);
                    var y = decimal.Parse(match.Groups[3].Value);
                    points.Add(new Point(cut, x, y));
                }
            }

            var filledPoints = new List<Point>();

            Point? prevPoint = null;
            foreach (var point in points)
            {
                if (prevPoint == null)
                {
                    prevPoint = point;
                    continue;
                }

                Point p1 = prevPoint;
                Point p2 = point;

                prevPoint = point;

                int mode = p2.Mode;
                double distance = p1.DistanceTo(p2);

                if (mode == 0 || distance <= maxDistance)
                {
                    filledPoints.Add(p2);
                    continue;
                }

                int numOfPoints = (int)Math.Ceiling(distance / maxDistance);
                decimal dx = (p2.X - p1.X) / numOfPoints;
                decimal dy = (p2.Y - p1.Y) / numOfPoints;

                for (int j = 1; j <= numOfPoints; j++)
                {
                    decimal newX = p1.X + j * dx;
                    decimal newY = p1.Y + j * dy;
                    filledPoints.Add(new Point(mode, newX, newY));
                }
            }

            var radialPoints = new List<RadialPoint>();

            foreach (Point point in filledPoints)
            {
                int r = (int)(Math.Sqrt(Math.Pow((double)point.X, 2) + Math.Pow((double)point.Y, 2)) * radialSteps);
                int angle = (int)(Math.Atan2((double)point.Y, (double)point.X) * angleSteps / (2 * Math.PI));
                angle = angle < 0 ? angleSteps + angle : angle;

                radialPoints.Add(new RadialPoint(point.Mode, r, angle));
            }

            var radialNoJumpPoints = new List<RadialPoint>();

            RadialPoint? prevRadPoint = null;
            var angleThresholdMin = angleSteps * 0.45;
            var angleThresholdMax = angleSteps * 0.55;
            var currentOffset = 0;

            foreach (var point in radialPoints)
            {
                if (prevRadPoint == null)
                {
                    radialNoJumpPoints.Add(point);
                    prevRadPoint = point;
                    continue;
                }

                if(prevRadPoint.A > angleThresholdMax && point.A < angleThresholdMin) 
                {
                    currentOffset += angleSteps;
                }
                if(prevRadPoint.A < angleThresholdMin && point.A > angleThresholdMax)
                {
                    currentOffset -= angleSteps;
                }

                radialNoJumpPoints.Add(new RadialPoint(point.Mode, point.R, point.A + currentOffset));

                prevRadPoint = point;
            }

            var resPath = FileHelper.GetRandomFilePath(Path.GetDirectoryName(filePath), ".rgcode");

            var fileText = string.Join("", radialNoJumpPoints.Select(r => $"R{r.Mode} {r.R} {r.A}{Environment.NewLine}").ToList());

            await File.WriteAllTextAsync(resPath, fileText);

            return resPath;
        }

        public static async Task<string> GCodePreview(string filePath)
        {
            string[] gcodeLines = await File.ReadAllLinesAsync(filePath);
            var points = new List<Point>();

            Regex regex = new Regex(@"^G([01])\sX([0-9\-\.]+)\sY([0-9\-\.]+)");

            foreach (var line in gcodeLines)
            {
                Match match = regex.Match(line);
                if (match.Success)
                {
                    var cut = int.Parse(match.Groups[1].Value);
                    var x = decimal.Parse(match.Groups[2].Value);
                    var y = decimal.Parse(match.Groups[3].Value);
                    points.Add(new Point(cut, x, y));
                }
            }

            var plotModel = new PlotModel { Title = "G-code Preview" };

            plotModel.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Title = "X" });
            plotModel.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Title = "Y" });

            var lineSeries = new LineSeries { LineStyle = LineStyle.Solid, Color = OxyColors.Blue };
            var moveSeries = new LineSeries { LineStyle = LineStyle.Solid, Color = OxyColors.Yellow };

            Point? prevPoint = null;
            foreach (var point in points)
            {
                if (prevPoint == null)
                {
                    prevPoint = point;
                    continue;
                }

                if(point.Mode == 1)
                {
                    lineSeries.Points.Add(new DataPoint((double)prevPoint.X, (double)prevPoint.Y));
                    lineSeries.Points.Add(new DataPoint((double)point.X, (double)point.Y));
                }
                else
                {
                    moveSeries.Points.Add(new DataPoint((double)prevPoint.X, (double)prevPoint.Y));
                    moveSeries.Points.Add(new DataPoint((double)point.X, (double)point.Y));
                }

                prevPoint = point;
            }

            plotModel.Series.Add(lineSeries);
            plotModel.Series.Add(moveSeries);

            int width = 1000;
            int height = 1000;

            var exporter = new PngExporter { Width = width, Height = height };

            var resPath = FileHelper.GetRandomFilePath(Path.GetDirectoryName(filePath), ".png");

            using (var stream = new MemoryStream())
            {
                exporter.Export(plotModel, stream);
                stream.Seek(0, SeekOrigin.Begin);

                using (var image = Image.Load<Rgb24>(stream))
                {
                    await image.SaveAsync(resPath);
                }
            }

            return resPath;
        }

        public class RadialPoint(int mode, int r, int a)
        {
            public int Mode { get; set; } = mode;
            public int R { get; set; } = r;
            public int A { get; set; } = a;
        }

        public class Point
        {
            public int Mode { get; }
            public decimal X { get; }
            public decimal Y { get; }

            public Point(int mode, decimal x, decimal y)
            {
                Mode = mode;
                X = x;
                Y = y;
            }

            public double DistanceTo(Point other)
            {
                return Math.Sqrt(Math.Pow((double)(X - other.X), 2) + Math.Pow((double)(Y - other.Y), 2));
            }
        }
    }
}
