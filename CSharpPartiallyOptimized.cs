using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ConsoleApplication1
{
    /// <summary>
    /// Программа фильтрации 10 000 000 строк
    /// </summary>
    internal class ProgramOpt
    {
        /// <summary>
        /// Размер массива
        /// </summary>
        private const long ArraySize = 10000000;

        /// <summary>
        /// Генератор случайных чисел
        /// </summary>
        private static readonly Random Rand = new Random();

        private static int minValMoney;
        private static int maxValMoney;

        private static int minValCode;
        private static int maxValCode;

        /// <summary>
        /// Генерация случайной строки
        /// </summary>
        private static CashAccountRow GenerateRow()
        {
            var cashAccountRow = new CashAccountRow
                {
                    Age = Convert.ToByte(Rand.Next(100)), 
                    AmountOfMoney = Rand.Next(1000000),
                    Code = Rand.Next(1000000), 
                    Gender = Convert.ToBoolean(Rand.Next(2)), 
                    Height = Rand.Next(300)
                };
            return cashAccountRow;
        }

        private static void Main()
        {
            var initialData = new CashAccountRow[ArraySize];
            for (var i = 0; i < ArraySize; i++)
            {
                initialData[i] = GenerateRow();
            }

            Console.WriteLine("Generated rows: {0} \n", ArraySize);

            var rangeFilters = new List<ConditionWithSelectivity>();


            // gender filter
            if (Rand.Next(2) <= 1)
            {
                var val = Convert.ToBoolean(Rand.Next(2));
                rangeFilters.Add(new ConditionWithSelectivity(data => data.Where(row => val == row.Gender).ToArray(), 1 / 2d));
            }

            // height filter
            if (Rand.Next(2) <= 1)
            {
                var minVal = Rand.Next(100);
                var maxVal = minVal + 50;
                rangeFilters.Add(new ConditionWithSelectivity(data => data.Where(row => minVal <= row.Height && row.Height <= maxVal).ToArray(), (maxVal - minVal) / 300d));
            }

            // AmountOfMoney filter
            if (Rand.Next(2) <= 1)
            {
                minValMoney = Rand.Next(30000);
                maxValMoney = minValMoney + 5000;
                rangeFilters.Add(new ConditionWithSelectivity(AmountOfMoneyFilter, (maxValMoney - minValMoney) / 1000000d));
            }

            // age filter
            if (Rand.Next(2) <= 1)
            {
                var minVal = Rand.Next(50);
                var maxVal = minVal + 25;
                rangeFilters.Add(new ConditionWithSelectivity(data => data.Where(row => minVal <= row.Age && row.Age <= maxVal).ToArray(), (maxVal - minVal) / 100d));
            }

            // code filter
            if (Rand.Next(2) <= 1)
            {
                minValCode = Rand.Next(30000);
                maxValCode = minValCode + 5;
                rangeFilters.Add(new ConditionWithSelectivity(CodeFilter, (maxValCode - minValCode) / 1000000d));
            }

            // code filter
//            rangeFilters.Add(new ConditionWithSelectivity(data => data.Where(row => row.Gender), 1 / 1000000d));
//            rangeFilters.Add(new ConditionWithSelectivity(data => data.Where(row => true), 0 / 1000000d));

            Console.WriteLine("C#-Searching...");

            DateTime start = DateTime.Now;
            var cashAccountRowsFilters = Search(initialData, rangeFilters);
            Console.WriteLine("1 stage {0} seconds.", (DateTime.Now - start).TotalSeconds);
            var result = Evaluate(cashAccountRowsFilters);
            Console.WriteLine("C#-search took {0} seconds.", (DateTime.Now - start).TotalSeconds);
            Console.WriteLine("Found rows: {0} \n", result.Count());
        }

        private static CashAccountRow[] CodeFilter(CashAccountRow[] data)
        {
            var sw = new Stopwatch();

            var list = new List<CashAccountRow>();
            unsafe
            {
                fixed (CashAccountRow* dataPtr = data)
                {
                    var length = data.Length;
                    CashAccountRow* endPointer = dataPtr + length;
                    
                    // note: очень долгий цикл !!!
                    sw.Start();
                    for (CashAccountRow* itemPointer = dataPtr; itemPointer < endPointer; ++itemPointer)
                    {
                        if (minValCode <= (*itemPointer).Code && (*itemPointer).Code <= maxValCode)
                        {
                            list.Add(*itemPointer);
                        }
                    }

                    sw.Stop();
                    Console.WriteLine("Затрачена на поиск по Code {0}", sw.Elapsed.TotalSeconds);
                }
            }

            var cashAccountRows = list.ToArray();
//            Console.WriteLine("Найдено классическим методом {0}", data.Where(row => minValCode <= row.Code && row.Code <= maxValCode).ToArray().Length);
//            Console.WriteLine("Найдено оптимизированным методом {0}", cashAccountRows.Length);
            return cashAccountRows;

//            return data.Where(row => minVal <= row.Code && row.Code <= maxVal).ToArray();
        }

        private static CashAccountRow[] AmountOfMoneyFilter(CashAccountRow[] data)
        {
            Stopwatch sw = new Stopwatch();

            var list = new List<CashAccountRow>();
            unsafe
            {
                fixed (CashAccountRow* dataPtr = data)
                {
                    var length = data.Length;
                    CashAccountRow* endPointer = dataPtr + length;
                    sw.Start();
                    for (CashAccountRow* itemPointer = dataPtr; itemPointer <= endPointer; ++itemPointer)
                    {
                        var code = (*itemPointer).AmountOfMoney;
                        if (minValMoney <= code && code <= maxValMoney)
                        {
                            list.Add(*itemPointer);
                        }
                    }

                    sw.Stop();
                    Console.WriteLine("Затрачена на поиск по AmountOfMoney {0}", sw.Elapsed.TotalSeconds);
                }
            }

            return list.ToArray();

//            return data.Where(row => minVal <= row.Code && row.Code <= maxVal).ToArray();
        }

        private static IEnumerable<CashAccountRow> Evaluate(IEnumerable<CashAccountRow> cashAccountRowsFilters)
        {
            return cashAccountRowsFilters.ToArray();
        }

        /// <summary>
        /// Поиск в массиве
        /// </summary>
        private static IEnumerable<CashAccountRow> Search(CashAccountRow[] fullData, IEnumerable<ConditionWithSelectivity> rangeFilters)
        {
            return
                rangeFilters
                    .OrderBy(condition => condition.Selectivity)
                    .Aggregate(fullData, (current, selectivity) => selectivity.Filter(current));
        }

        public struct CashAccountRow
        {
            /// <summary>
            /// Значение из диапазона 0 - 1000000
            /// </summary>
            public int Code { get; set; }

            /// <summary>
            /// Значение из диапазона 0 - 100
            /// </summary>
            public byte Age { get; set; }

            /// <summary>
            /// Значение из диапазона 0 - 1000000
            /// </summary>
            public int AmountOfMoney { get; set; }

            /// <summary>
            /// Значение из диапазона 0 - 1
            /// </summary>
            public bool Gender { get; set; }

            /// <summary>
            /// Значение из диапазона 0 - 300
            /// </summary>
            public int Height { get; set; }
        }

        public class ConditionWithSelectivity
        {
            private readonly Func<CashAccountRow[], CashAccountRow[]> filter;

            public ConditionWithSelectivity(Func<CashAccountRow[], CashAccountRow[]> filter, double selectivity)
            {
                this.filter = filter;
                Selectivity = selectivity;
            }

            public double Selectivity { get; protected set; }

            public CashAccountRow[] Filter(CashAccountRow[] data)
            {
                return filter(data).ToArray();
            }
        }
    }
}
