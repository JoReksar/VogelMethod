using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using ConsoleApp5;

namespace ConsoleApp5
{
    internal class Program
    {
        private const string PathToTariffs = "../../../Resources/Tariffs.txt";
        private const string PathToResources = "../../../Resources/Resources.txt";
        private const string PathToConsumers = "../../../Resources/Consumers.txt";
        
        static void Main(string[] args)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var matrix = GetTariffFromFile(PathToTariffs);
            var resources = GetOneDimArrayFromFile(PathToResources);
            var consumers = GetOneDimArrayFromFile(PathToConsumers);

            RunCasino(new VogelMatrix(matrix, resources, consumers));

            stopwatch.Stop();
            Console.WriteLine($"Time {stopwatch.ElapsedMilliseconds}");
            Console.ReadKey();
        }

        private static void RunCasino(VogelMatrix vogelMatrix)
        {
            while (true)
            {
                if (vogelMatrix is null) 
                    return;

                vogelMatrix.PrintToConsole();
                var nextVogelMatrix = vogelMatrix.GetNextStepMatrix();
                vogelMatrix = nextVogelMatrix;
            }
        }

        private static VogelElement[,] GetTariffFromFile(string pathToFile)
        {
            var elements = File.ReadAllLines(pathToFile);

            var rowCount = elements.Length;
            var columnCount = elements[0].Split(' ').Length;

            var matrix = new VogelElement[rowCount, columnCount];
            for (int i = 0; i < rowCount; i++)
            {
                var splitedRow = elements[i].Split(' ');
                for (int j = 0; j < columnCount; j++)
                {
                    matrix[i, j] = new VogelElement(int.Parse(splitedRow[j]), 0m);
                }
            }

            return matrix;
        }

        private static decimal[] GetOneDimArrayFromFile(string pathToFile)
        {
            var elements = File.ReadAllText(pathToFile).Split(' ');
            
            var oneDimArray = new decimal[elements.Length];
            for (int i = 0; i < elements.Length; i++)
                oneDimArray[i] = decimal.Parse(elements[i]);

            return oneDimArray;
        }


        static VogelElement[,] Parse(string data)
        {
            var elems = data.Split('\n');

            var rowCount = elems.Length;
            var columnCount = elems[0].Split(' ').Length;

            var matrix = new VogelElement[rowCount, columnCount];
            for (int i = 0; i < rowCount; i++)
            {
                var splitedRow = elems[i].Split(' ');
                for (int j = 0; j < columnCount; j++)
                {
                    matrix[i, j] = new VogelElement(int.Parse(splitedRow[j]), 0m);
                }
            }
            return matrix;
        }
    }
}
