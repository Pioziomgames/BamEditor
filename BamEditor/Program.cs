using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace BamEditor
{
    internal class Program
    {
        public static void Exit()
        {
            Console.WriteLine("\nPress Any Key to Quit");
            Console.ReadKey();
            System.Environment.Exit(0);
        }

        static void Main(string[] args)
        {
            string InputFile = "";

            if (args.Length > 0)
                InputFile = args[0];
            else
            { 
                Console.WriteLine($"BamEditor v0.3.1\n" +
                    $"Extracts BAM files found in Persona Q and Etrian Odyssey games\n" +
                    $"Usage:\n" +
                    $"       BamEditor.exe InputFile (optional)OutputFolder\n");// +
                    //$"       BamEditor.exe InputFolder (optional)OutputFile");
            }
            string path = $@"{Path.GetDirectoryName(InputFile)}\{Path.GetFileNameWithoutExtension(InputFile)}_extracted"; // deletes the extension from the filename and adds _extracted

            
            if (args.Length > 1)
                path = args[1];


            if (File.Exists(InputFile))
            {
                var BAMFile = new Bam(path);
            }
            else //if (!Directory.Exists(InputFile))
            {
                Console.WriteLine($"\n{InputFile} does not exist");
                Exit();
            }
        }
    }
}
