using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace TibcoLog
{
    class Program
    {
        
        static void Main(string[] args)
        {
            bool ok = true;
            string inputFile = "";
            string searchString = "";

            Console.WriteLine("Tibco log analyzer v.{0}", typeof(Program).Assembly.GetName().Version);
            
            foreach (String par in args)
                {
                    if (par.StartsWith("-f", StringComparison.InvariantCultureIgnoreCase))
                    {
                        try
                        {
                            inputFile = par.Substring(3);
                        }
                        catch { } 
                    }
                    if (par.StartsWith("-s", StringComparison.InvariantCultureIgnoreCase))
                    {
                        try
                        {
                            searchString = par.Substring(3);
                        }
                        catch { }
                    }

                }

            if ((args.Length < 2) ||(inputFile.Length*searchString.Length==0))
            {
                ok = false;
                Console.WriteLine("[E] niepoprawna liczba lub błędne wartości parametrów");
                Console.WriteLine("-f:nazwa pliku do przeszukania");
                Console.WriteLine("-s:szukana fraza [nie regexp]");
            }

            if (!File.Exists(inputFile))
            {
                Console.WriteLine("[E] wskazany plik nie istnieje");
                ok = false;
            }
        }
        
    }
}
