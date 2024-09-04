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
            var normalizedLines = await Normalize(filePath);

            var resPath = FileHelper.GetRandomFilePath(Path.GetDirectoryName(filePath), ".ngcode");

            var fileText = string.Join("", normalizedLines.Select(l => $"G{l.Item1} X{l.Item2} Y{l.Item3}{Environment.NewLine}").ToList());

            await File.WriteAllTextAsync(resPath, fileText);

            return resPath;
        }

        public static async Task<List<(int, decimal, decimal)>> Normalize(string filePath)
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
                return new List<(int, decimal, decimal)>();
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

            return coordinates;
        }
    }
}
