using iText;
using iText.Kernel.Exceptions;
using iText.Kernel.Pdf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace PdfPassword
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args == null || args.Length == 0) return;

            var pdfPath = args[0];
            
            var destinationPath = (args.Length > 1) ? args[1] : null ;

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
            
            var password = Find(pdfPath);
            
            stopwatch.Stop();

            Console.WriteLine("Completed in.... {0} minutes {1} seconds", stopwatch.Elapsed.Minutes,stopwatch.Elapsed.Seconds);

            Console.WriteLine("Completed in.... {0} seconds", stopwatch.Elapsed.TotalSeconds);


            if (!string.IsNullOrEmpty(destinationPath))
            {
                SavePdf(pdfPath, password, destinationPath);
            }
        }


        public static string Find(string pdfFullname)
        {
            for (int y = 1940; y < 2001; y++)
            {
                for (int m = 1; m < 13; m++)
                {
                    for (int d = 1; d < 31; d++)
                    {
                        var password = string.Format("{0}{1}{2}", d.ToString("00"), m.ToString("00"), y);

                        //if (IsPasswordValid(pdfFullname, password))
                        if (IsPasswordValid(pdfFullname, Encoding.UTF8.GetBytes(password)))
                        {
                            Console.WriteLine("Correct password is: {0}", password);
                            return password;
                        }
                    }
                }
            }

            return null;

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

                using (PdfDocument document = new PdfDocument(new PdfReader(pdfFullname)))
                {

                    int numberOfPdfObject = document.GetNumberOfPdfObjects();

                    for (int i = 1; i <= numberOfPdfObject; i++)
                    {
                        var obj = document.GetPdfObject(i);
                        if (obj != null && obj.IsStream())
                        {
                            byte[] b;
                            try
                            {

                                // Get decoded stream bytes.
                                b = ((PdfStream)obj).GetBytes();
                            }
                            catch (PdfException)
                            {

                                return true;
                            } 
                        }
                    }
                }
                
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
                using (PdfDocument document = 
                        new PdfDocument(new PdfReader(pdfFullname, 
                                                    new ReaderProperties().SetPassword(password)).SetUnethicalReading(true))
                      )
                {

                    return document.GetReader().IsOpenedWithFullPermission();
                }                
            }
            catch (BadPasswordException)
            {
                return false;
            }
        }

        public static bool IsPasswordValid(string pdfFullname, string password)
        {
            try
            {

                var userPassword = string.Empty;

                using (PdfDocument document = 
                            new PdfDocument(new PdfReader(pdfFullname, 
                                new ReaderProperties().SetPassword(Encoding.UTF8.GetBytes(password))).SetUnethicalReading(true)
                            ))
                {
                    byte[] computeUserPassword = document.GetReader().ComputeUserPassword();

                    // The result of user password computation logic can be null in case of
                    // AES256 password encryption or non password encryption algorithm
                    userPassword = computeUserPassword == null ? null : Encoding.UTF8.GetString(computeUserPassword);
                    Console.Out.WriteLine(password);
                }


                return (!string.IsNullOrEmpty(userPassword));

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

        private static void SavePdf(string source, string password, string destination)
        {

            FileInfo file = new FileInfo(destination);
            file.Directory.Create();
            
            var pdfReader = new PdfReader(source, new ReaderProperties().SetPassword(Encoding.UTF8.GetBytes(password))).SetUnethicalReading(true);

            var pdfWriter = new PdfWriter(destination);

            var pdfDoc = new PdfDocument(pdfReader, pdfWriter);
            pdfDoc.Close();
        }
    }
}
