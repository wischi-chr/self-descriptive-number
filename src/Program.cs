using System;
using System.Collections.Generic;
using System.Linq;

namespace TheSelfDescriptiveNumber
{
    class Program
    {
        private readonly static Random rnd = new Random();

        static void Main(string[] args)
        {
            //it is not allowed to pass multiple aguments: show help and exit.
            if (args.Length >= 2)
            {
                ShowUsage();
                return;
            }

            //if one argument is passed then...
            if (args.Length == 1)
            {
                //split
                var parts = args[0].Split('=');

                //if there wasn't exactly one equal sign -> show help and exit.
                if (parts.Length != 2)
                {
                    ShowUsage();
                    return;
                }

                //parse Base (N)
                int B = 0;
                if (!int.TryParse(parts[1], out B) || B < 2 || B > 39)
                {
                    ShowUsage();
                    return;
                }

                //decide what to do based on the argument
                switch (parts[0].Trim())
                {
                    case "--brute-force": FindAllSolutionsWithConsoleOutput(B); break;
                    case "--quick-solve": FindSingleSolutionWithQuicksMode(B); break;
                    default:
                        ShowUsage();
                        return;
                }
            }
            else
            {
                //without arguments show a little demo ^^
                ShowBase2To39Solutions();
            }

            Console.WriteLine();
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }

        /// <summary>
        /// Help screen / Usage
        /// </summary>
        static void ShowUsage()
        {
            Console.WriteLine("Possible Arguments (only one Argument allowed):");
            Console.WriteLine("(No spaces before and after the equal sign)");
            Console.WriteLine("N......Base (2-39)");
            Console.WriteLine();
            Console.WriteLine("   --brute-force=[N]");
            Console.WriteLine("   --quick-solve=[N]");
        }

        /// <summary>
        /// Little demo: shows every (known) solution
        /// </summary>
        static void ShowBase2To39Solutions()
        {
            for (int i = 2; i < 40; i++)
            {
                var solutions = FindSolutionsAutoselect(i).ToArray();
                var res = solutions.Length < 1 ? "None" : string.Join(", ", solutions);
                Console.WriteLine($"Solution(s) for base {i,2}: {res}");
            }
        }

        /// <summary>
        /// Finds (all) solutions for a given base and automatically selects the method how to find it/them
        /// </summary>
        /// <param name="Base"></param>
        /// <returns></returns>
        static IEnumerable<string> FindSolutionsAutoselect(int Base)
        {
            //Switch to heuristic for bases >= 8 because "FindAllValidSolutions" (brute-force) would be too slow.
            if (Base < 8) return FindAllValidSolutions(Base);
            else return FindHeuristicSolution(Base);
        }

        static void FindSingleSolutionWithQuicksMode(int Base)
        {
            var digits = new int[Base];
            var steps = HeuristicNumberSearch(digits);
            Console.WriteLine($"Solution in {steps} step(s) for base {Base}: {GetNumber(digits)}");
        }

        static void FindAllSolutionsWithConsoleOutput(int Base)
        {
            var start = DateTime.Now;
            var last = DateTime.MinValue;
            var refreshRate = TimeSpan.FromSeconds(2);

            Action<double> printProgress = (p) => PrintProgress(p, refreshRate, start, ref last);

            Console.WriteLine($"====== Test base {Base}");
            var found = FindAllValidSolutions(Base, printProgress).ToList();
            Console.WriteLine();
            Console.WriteLine("Matching numbers: ");

            if (found.Count < 1) Console.WriteLine("   No results found.");
            else Console.WriteLine(string.Join(Environment.NewLine, found.Select(s => "   " + s).ToArray()));

            Console.WriteLine();
            Console.WriteLine();
        }

        static void PrintProgress(double Progress, TimeSpan RefreshRate, DateTime Start, ref DateTime LastPrint)
        {
            if ((DateTime.Now - LastPrint) < RefreshRate) return;
            LastPrint = DateTime.Now;
            var diff = DateTime.Now - Start;
            var est = new TimeSpan((long)((double)diff.Ticks / Progress * (1 - Progress)));
            Console.WriteLine($"   Progress: {Math.Round((double)Progress * 100, 2),6:0.00}% (EST: {est.GetReadableTimespan()})");
        }

        /// <summary>
        /// Finds every possible 
        /// This process can take some time (took about 5min on my machine for base 10 ^^)
        /// </summary>
        static IEnumerable<string> FindAllValidSolutions(int Base, Action<double> Progress = null)
        {
            int[] digits = new int[Base];
            long counter = 0;

            do
            {

                if (IsNumberCorrect(digits))
                {
                    var num = GetNumber(digits);
                    yield return num;
                }

                if (Progress != null)
                {
                    counter++;
                    if (counter >= 100000)
                    {
                        counter = 0;
                        Progress(GetProgress(digits));
                    }
                }
            } while (!IncrememtDigits(digits));


        }

        /// <summary>
        /// Looks for a solution by iterative digit correction
        /// </summary>
        /// <param name="Base"></param>
        /// <returns></returns>
        static IEnumerable<string> FindHeuristicSolution(int Base)
        {
            var digits = new int[Base];
            var results = new List<string>();

            if (HeuristicNumberSearch(digits) >= 0)
                results.Add(GetNumber(digits));

            return results;
        }

        static int HeuristicNumberSearch(int[] digits, int MaxSteps = 10000)
        {
            var steps = 0;
            while (HeuristicDigitSearch(digits) && steps < MaxSteps) steps++;
            if (!IsNumberCorrect(digits)) return -1;
            return steps;
        }

        static bool HeuristicDigitSearch(int[] digits)
        {
            var wrongDigits = GetWrongDigits(digits).ToArray();
            if (wrongDigits.Length < 1) return false;

            var fixDigit = wrongDigits[rnd.Next(wrongDigits.Length)];
            var newDigit = digits.Where(d => d == fixDigit).Count();

            //If the new value is equal to the old value, than use a "mutation" (random digit)
            if (newDigit == digits[fixDigit]) newDigit = rnd.Next(digits.Length);
            digits[fixDigit] = newDigit;

            return true;
        }

        static double GetProgress(int[] digits)
        {
            var len = digits.Length;
            var progress = 0.0;
            for (int i = 0; i < len; i++)
                progress += (double)digits[i] / len / Math.Pow(len, i);
            return progress;
        }

        /// <summary>
        /// Inrement the number by one (+overflow handling)
        /// </summary>
        /// <param name="digits"></param>
        /// <returns></returns>
        static bool IncrememtDigits(int[] digits)
        {
            if (digits.Length == 0) return true;
            bool overflow = false;
            int pos = digits.Length - 1;
            int maxDigit = digits.Length - 1;
            do
            {
                overflow = false;
                var dig = digits[pos];
                dig++;
                if (dig > maxDigit)
                {
                    overflow = true;
                    digits[pos] = 0;
                }
                else
                    digits[pos] = dig;

                if (overflow) pos--;
            } while (overflow && pos >= 0);
            return overflow;
        }

        /// <summary>
        /// Tests if the number represented by this digits matches the requirements
        /// </summary>
        /// <remarks>
        /// Alternative code: "return !GetWrongDigits(digits).Any();"
        /// would be cleaner IMO but it is slower by a factor of ~2
        /// </remarks>
        /// <param name="digits"></param>
        /// <returns></returns>
        static bool IsNumberCorrect(int[] digits)
        {
            var len = digits.Length;
            var sums = new int[len];
            for (int i = 0; i < len; i++)
            {
                if (digits[i] >= len) return false;
                sums[digits[i]]++;
            }

            for (int i = 0; i < len; i++)
                if (digits[i] != sums[i]) return false;

            return true;
        }

        static IEnumerable<int> GetWrongDigits(int[] digits)
        {
            var len = digits.Length;
            var sums = new int[len];
            bool[] returned = new bool[len];

            for (int i = 0; i < len; i++)
            {
                if (digits[i] >= len)
                {
                    returned[i] = true;
                    yield return i;
                }
                else
                {
                    sums[digits[i]]++;
                }
            }

            for (int i = 0; i < len; i++)
                if (!returned[i])
                    if (digits[i] != sums[i])
                        yield return i;
        }

        /// <summary>
        /// Generates a string that represents a given number (digit-array)
        /// </summary>
        /// <param name="Digits"></param>
        /// <returns></returns>
        static string GetNumber(int[] Digits)
        {
            return new string(Digits.Select(GetDigit).ToArray());
        }

        static char GetDigit(int value)
        {
            if (value < 0) return '?';
            if (value <= 9) return (char)('0' + value);
            if (value <= 35) return (char)('A' + (value - 10));
            return '?';
        }
    }
}
