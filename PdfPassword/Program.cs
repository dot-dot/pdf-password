using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using iTextSharp.text.exceptions;
using iTextSharp.text.pdf;

namespace PdfPassword
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args == null || args.Length == 0) return;

            var pdfPath = args[0];

            var stopwatch = new Stopwatch();

            if (!File.Exists(pdfPath))
            {
                Console.WriteLine("404! 404! abort!");
                return;
            }

            if (!IsPasswordProtected(pdfPath))
            {
                Console.WriteLine("Doesn't appear to be password protected...");
                return;
            }

            stopwatch.Start();

            Find(pdfPath);
            
            stopwatch.Stop();

            Console.WriteLine("Completed in.... {0} minutes {1} seconds", stopwatch.Elapsed.Minutes,stopwatch.Elapsed.Seconds);

            Console.WriteLine("Completed in.... {0} seconds", stopwatch.Elapsed.TotalSeconds);
        }


        public static void Find(string pdfFullname)
        {
            for (int y = 1940; y < 2001; y++)
            {
                for (int m = 1; m < 13; m++)
                {
                    for (int d = 1; d < 31; d++)
                    {
                        var password = string.Format("{0}{1}{2}", d.ToString("00"), m.ToString("00"), y);

                        if (IsPasswordValid(pdfFullname, GetBytes(password)))
                        {
                            Console.WriteLine("Correct password is: {0}", password);
                            return;
                        }
                    }
                }
            }          
        }

        public static void FindParallel(string pdfFullname)
        {
            Parallel.For(1940, 2001, (y, firstState) => Parallel.For(1, 13, (m, secondState) => Parallel.For(1, 32, (d, state) =>
            {
                //var password = string.Format("{0}{1}{2}", d.ToString("00"), DateTimeFormatInfo.CurrentInfo.GetAbbreviatedMonthName(m), y);
                var password = string.Format("{0}{1}{2}", d.ToString("00"), m.ToString("00"), y);

                if (IsPasswordValid(pdfFullname, GetBytes(password)))
                {
                    Console.WriteLine("Correct password is: {0}", password);
                    firstState.Break();
                    secondState.Break();
                    state.Break();

                }
            })));      
        }

        public static bool IsPasswordProtected(string pdfFullname)
        {
            try
            {
                var pdfReader = new PdfReader(pdfFullname);
                return false;
            }
            catch (BadPasswordException)
            {
                return true;
            }
        }


        public static bool IsPasswordValid(string pdfFullname, byte[] password)
        {
            try
            {
                var pdfReader = new PdfReader(pdfFullname, password);
                return true;
            }
            catch (BadPasswordException)
            {
                return false;
            }
        }

        private static byte[] GetBytes(string str)
        {
            return Encoding.ASCII.GetBytes(str);           
        }
    }
}
