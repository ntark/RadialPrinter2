using Microsoft.AspNetCore.Mvc;
using System;
using System.ComponentModel;
using System.Text.RegularExpressions;

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

            if(coordinates.Count == 0)
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
        
        public static async Task<string> XyToRad(string filePath, double maxDistance = 0.1, int radialSteps = -4000, int angleSteps = 27800)
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

            var radialPoints = new List<(int, int, int)>();

            foreach (Point point in filledPoints)
            {
                int r = (int)(Math.Sqrt(Math.Pow((double)point.X, 2) + Math.Pow((double)point.Y, 2)) * radialSteps);
                int angle = (int)(Math.Atan2((double)point.Y, (double)point.X) * angleSteps / (2 * Math.PI));

                radialPoints.Add((point.Mode, r, angle));
            }

            var resPath = FileHelper.GetRandomFilePath(Path.GetDirectoryName(filePath), ".rgcode");

            var fileText = string.Join("", radialPoints.Select(r => $"R{r.Item1} {r.Item2} {r.Item3}{Environment.NewLine}").ToList());

            await File.WriteAllTextAsync(resPath, fileText);

            return resPath;
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
