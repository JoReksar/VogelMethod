using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using ConsoleTables;

namespace ConsoleApp5
{
    internal sealed class VogelMatrix
    {
        private abstract class VogelMatrixBaseIndexResolver
        {
            protected VogelMatrix Owner { get; }
            protected int Index { get; }
            protected VogelMatrixBaseIndexResolver(VogelMatrix owner, int index)
            {
                Owner = owner;
                Index = index;
            }

            public abstract (int rowIndex, int columnIndex) Resolve();
        }
        
        private class RowIndexResolver : VogelMatrixBaseIndexResolver
        {
            public RowIndexResolver(VogelMatrix owner, int rowIndex) : base(owner, rowIndex) { }

            private int GetColumnTarget()
            {
                var row = Owner._matrixValue.GetRow(Index);
                
                int? currentColumnIndex = null;
                var currentTariffValue = -1;
                
                for (var columnIndex = 0; columnIndex < Owner.ColumnsCount; columnIndex++)
                {
                    if (row[columnIndex].IsClosed)
                        continue;
                    
                    if (currentColumnIndex is null)
                    {
                        currentColumnIndex = columnIndex;
                        currentTariffValue = row[columnIndex].Tariff;
                        continue;
                    }

                    if (row[columnIndex].Tariff > currentTariffValue) 
                        continue;
                    
                    currentColumnIndex = columnIndex;
                    currentTariffValue = row[columnIndex].Tariff;
                }

                return currentColumnIndex.Value;
            }
            
            public override (int rowIndex, int columnIndex) Resolve()
            {
                var columnIndex = GetColumnTarget();
                
                return (Index, columnIndex);
            }
        }

        private class ColumnIndexResolver : VogelMatrixBaseIndexResolver
        {
            public ColumnIndexResolver(VogelMatrix owner, int columnIndex) : base(owner, columnIndex) { }

            private int GetRowTarget()
            {
                var column = Owner._matrixValue.GetColumn(Index);
                
                int? currentRowIndex = null;
                var currentTariffValue = 0;
                
                for (var rowIndex = 0; rowIndex < Owner.RowsCount; rowIndex++)
                {
                    if (column[rowIndex].IsClosed)
                        continue;
                    
                    if (currentRowIndex is null)
                    {
                        currentRowIndex = rowIndex;
                        currentTariffValue = column[rowIndex].Tariff;
                        continue;
                    }
                    
                    if (column[rowIndex].Tariff > currentTariffValue) 
                        continue;
                    
                    currentRowIndex = rowIndex;
                    currentTariffValue = column[rowIndex].Tariff;
                }

                return currentRowIndex.Value;
            }
            
            public override (int rowIndex, int columnIndex) Resolve()
            {
                var rowIndex = GetRowTarget();
                
                return (rowIndex, Index);
            }
        }

        private readonly VogelElement[,] _matrixValue;
        private readonly decimal[] _resources;
        private readonly decimal[] _consumers;

        private int?[] _diffTariffsResourcesArray;
        private int?[] _diffTariffsConsumersArray;

        private readonly int _stepNumber;

        public int RowsCount { get; }
        public int ColumnsCount { get; }
        public bool IsCompleted { get; }

        public VogelMatrix(VogelElement[,] matrixValue, decimal[] resources, decimal[] consumers)
            : this(matrixValue, resources, consumers, 0)
        {
            if (!(resources.Sum() >= consumers.Sum()))
                throw new ArgumentException("Количество ресурсов меньше запроса покупателей");
        }
        private VogelMatrix(VogelElement[,] matrixValue, decimal[] resources, decimal[] consumers, int stepNumber)
        {
            _matrixValue = matrixValue;
            _resources = resources;
            _consumers = consumers;

            RowsCount = _matrixValue.GetLength(0);
            ColumnsCount = _matrixValue.GetLength(1);
            IsCompleted = _matrixValue.Cast<VogelElement>().All(element => element.IsClosed);

            _stepNumber = stepNumber;

            FillDiffTariffsConsumers();
            FillDiffTariffsResources();
        }

        private void CloseColumn(VogelElement[,] targetMatrix, int columnIndex)
        {
            for (var rowIndex = 0; rowIndex < RowsCount; rowIndex++)
                targetMatrix[rowIndex, columnIndex].SetAsClosed();
        }

        private void CloseRow(VogelElement[,] targetMatrix, int rowIndex)
        {
            for (var columnIndex = 0; columnIndex < ColumnsCount; columnIndex++)
                targetMatrix[rowIndex, columnIndex].SetAsClosed();
        }

        public decimal GetResult()
            => _matrixValue.Cast<VogelElement>().Sum(element => element.Value * element.Tariff);
        
        
        public VogelMatrix GetNextStepMatrix()
        {
            if (IsCompleted)
            {
                PrintFinalResult();
                return null;
            }
            
            return CreateNextStepMatrix();
        }

        private VogelMatrix CreateNextStepMatrix()
        {
            var targetElementInfo = GetIndexResolver().Resolve();

            var nextStepMatrix = DeepCloneVogelElements();
            var nextStepResources = (decimal[])_resources.Clone();
            
            var sumOfResourcesInColumn = _matrixValue
                .GetColumn(targetElementInfo.columnIndex)
                .Select(vogelElement => vogelElement.Value)
                .Sum();
                
            var howMuchLeft = _consumers[targetElementInfo.columnIndex] - sumOfResourcesInColumn;
            var howMuchStored = _resources[targetElementInfo.rowIndex];
            
            if (howMuchStored > howMuchLeft)
            {
                nextStepMatrix[targetElementInfo.rowIndex, targetElementInfo.columnIndex] += howMuchLeft;
                nextStepResources[targetElementInfo.rowIndex] -= howMuchLeft;
                
                CloseColumn(nextStepMatrix, targetElementInfo.columnIndex);
            }
            else if (howMuchStored == howMuchLeft)
            {
                nextStepMatrix[targetElementInfo.rowIndex, targetElementInfo.columnIndex] += howMuchLeft;
                nextStepResources[targetElementInfo.rowIndex] = 0;
                
                CloseColumn(nextStepMatrix, targetElementInfo.columnIndex);
                CloseRow(nextStepMatrix, targetElementInfo.rowIndex);
            }
            else
            {
                nextStepMatrix[targetElementInfo.rowIndex, targetElementInfo.columnIndex] += howMuchStored;
                nextStepResources[targetElementInfo.rowIndex] = 0;
                CloseRow(nextStepMatrix, targetElementInfo.rowIndex);
            }
            
            return new VogelMatrix(nextStepMatrix, nextStepResources, _consumers, _stepNumber + 1);
        }

        private void FillDiffTariffsConsumers()
        {
            _diffTariffsConsumersArray = new int?[ColumnsCount];

            for (var columnIndex = 0; columnIndex < _diffTariffsConsumersArray.Length; columnIndex++)
            {
                _diffTariffsConsumersArray[columnIndex] = GetDiff(GetTwoMaximumTariffByColumnNumber(columnIndex));
            }
                
        }

        private void FillDiffTariffsResources()
        {
            _diffTariffsResourcesArray = new int?[RowsCount];
            
            for (var rowIndex = 0; rowIndex < _diffTariffsResourcesArray.Length; rowIndex++)
                _diffTariffsResourcesArray[rowIndex] = GetDiff(GetTwoMinimusTariffByRowNumber(rowIndex));
        }


        private (int?, int?) GetTwoMaximumTariffByColumnNumber(int columnIndex)
        {
            var column = _matrixValue.GetColumn(columnIndex);

            int? firstMinimum = null;
            int? secondMinimum = null;
            foreach (var columnElement in column)
            {
                if (columnElement is null || columnElement.IsClosed)
                    continue;

                if (firstMinimum is null)
                {
                    firstMinimum = columnElement.Tariff;
                    continue;
                }
                
                if (firstMinimum > columnElement.Tariff)
                {
                    secondMinimum = firstMinimum;
                    firstMinimum = columnElement.Tariff;
                    continue;
                }
                
                if (secondMinimum > columnElement.Tariff || secondMinimum is null)
                    secondMinimum = columnElement.Tariff;
            }

            return (firstMinimum, secondMinimum);
        }

        private (int?, int?) GetTwoMinimusTariffByRowNumber(int rowIndex)
        {
            var row = _matrixValue.GetRow(rowIndex);

            int? firstMinimum = null;
            int? secondMinimum = null;

            foreach (var rowElement in row)
            {
                if (rowElement is null || rowElement.IsClosed)
                    continue;

                if (firstMinimum is null)
                {
                    firstMinimum = rowElement.Tariff;
                    continue;
                }

                if (firstMinimum > rowElement.Tariff)
                {
                    secondMinimum = firstMinimum;
                    firstMinimum = rowElement.Tariff;
                    continue;
                }

                if (secondMinimum > rowElement.Tariff || secondMinimum is null)
                    secondMinimum = rowElement.Tariff;
            }
            
            return (firstMinimum, secondMinimum);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int? GetDiff((int?, int?) values)
        {
            if (values.Item1 is null)
                return values.Item2;
            
            return values.Item2 is null ? values.Item1 : Math.Abs(values.Item1.Value - values.Item2.Value);
        }

        // Переставил местами, чтобы получить такой же результат, как на сайте
        private VogelMatrixBaseIndexResolver GetIndexResolver()
        {
            var isRow = true;
            
            var currentValue = -1;
            var indexMaxDiffTariff = -1;

            for (var columnIndex = 0; columnIndex < _diffTariffsConsumersArray.Length; columnIndex++)
                if (currentValue < _diffTariffsConsumersArray[columnIndex])
                {
                    currentValue = _diffTariffsConsumersArray[columnIndex].Value;
                    indexMaxDiffTariff = columnIndex;
                    isRow = false;
                }

            for (var rowIndex = 0; rowIndex < _diffTariffsResourcesArray.Length; rowIndex++)
                if (currentValue < _diffTariffsResourcesArray[rowIndex])
                {
                    currentValue = _diffTariffsResourcesArray[rowIndex].Value;
                    indexMaxDiffTariff = rowIndex;
                    isRow = true;
                }
                    
            return isRow ? (VogelMatrixBaseIndexResolver)new RowIndexResolver(this, indexMaxDiffTariff) : 
                                                  new ColumnIndexResolver(this, indexMaxDiffTariff);
        }

        private VogelElement[,] DeepCloneVogelElements()
        {
            var vogelElementsClone = new VogelElement[RowsCount, ColumnsCount];
            for (var rowIndex = 0; rowIndex < RowsCount; rowIndex++)
                for (var columnIndex = 0; columnIndex < ColumnsCount; columnIndex++)
                    vogelElementsClone[rowIndex, columnIndex] = _matrixValue[rowIndex, columnIndex].Clone();
            
            return vogelElementsClone;
        }

        public void PrintToConsole()
        {
            var outputRowsCount = RowsCount + 1;
            var outputColumnsCount = ColumnsCount + 1;
            object[,] output = new object[outputRowsCount + 1, outputColumnsCount + 1];

            for (int rowIndex = 1; rowIndex < outputRowsCount; rowIndex++)
                for (int columnIndex = 1; columnIndex < outputColumnsCount; columnIndex++)
                {
                    var element = _matrixValue[rowIndex - 1, columnIndex - 1];
                    output[rowIndex, columnIndex] = $"{element} {(element.IsClosed ? "(closed)" : string.Empty)}";
                }
                    
            output[0, 0] = "res/cons";
            output[0, outputColumnsCount] = "Diff res";
            output[outputRowsCount, 0] = "Diff cons";

            for (int columnIndex = 1; columnIndex < outputColumnsCount; columnIndex++)
            {
                output[0, columnIndex] = _consumers[columnIndex - 1];

                var diffTariffConsumerElement = _diffTariffsConsumersArray[columnIndex - 1];
                output[outputRowsCount, columnIndex] = diffTariffConsumerElement.HasValue ?
                    diffTariffConsumerElement.ToString() : "*";
            }
                
            for (int rowIndex = 1; rowIndex < outputRowsCount; rowIndex++)
            {
                output[rowIndex, 0] = _resources[rowIndex - 1];

                var diffTariffResourceElement = _diffTariffsResourcesArray[rowIndex - 1];
                output[rowIndex, outputColumnsCount] = diffTariffResourceElement.HasValue ?
                    diffTariffResourceElement.ToString() : "*";
            }
            
            var table = new ConsoleTable { Columns = output.GetRow(0) };

            for (int i = 1; i < outputRowsCount + 1; i++)
                table.AddRow(output.GetRow(i));
            Console.WriteLine($"Шаг {_stepNumber}");
            table.Write(Format.Alternative);
        }

        private void PrintFinalResult()
        {
            var builder = new StringBuilder();
            var list = new List<string>();
            builder.Append("Итого: ");
            for (int rowIndex = 0; rowIndex < RowsCount; rowIndex++)
                for (int columnIndex = 0; columnIndex < ColumnsCount; columnIndex++)
                {
                    var element = _matrixValue[rowIndex, columnIndex];

                    if (element.Value == 0m)
                        continue;

                    list.Add($"({element.Tariff} * {element.Value})"); 
                }

            builder.AppendJoin(" + ", list);
            builder.Append($" = {GetResult()}");
            Console.WriteLine(builder.ToString());
        }
    }

    internal static class VogelExtension
    {
        public static T[] GetColumn<T>(this T[,] twoDimensionalArray, int indexColumn)
        {
            var rowsCount = twoDimensionalArray.GetLength(0);
            var columnElements = new T[rowsCount];

            for (var indexRow = 0; indexRow < rowsCount; indexRow++)
                columnElements[indexRow] = twoDimensionalArray[indexRow, indexColumn];

            return columnElements;
        }
        public static T[] GetRow<T>(this T[,] twoDimensionalArray, int indexRow)
        {
            var columnsCount = twoDimensionalArray.GetLength(1);
            var rowElements = new T[columnsCount];

            for (var indexColumn = 0; indexColumn < columnsCount; indexColumn++)
                rowElements[indexColumn] = twoDimensionalArray[indexRow, indexColumn];

            return rowElements;
        }
    }
}
