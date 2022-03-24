using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Newtonsoft.Json;

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
            args = new string[]{ @"C:\Users\oem\Desktop\bam\pc00a.bam" };
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
                Exit();
            }
            string path = $@"{Path.GetDirectoryName(InputFile)}\{Path.GetFileNameWithoutExtension(InputFile)}_extracted"; // deletes the extension from the filename and adds _extracted

            
            if (args.Length > 1)
                path = args[1];


            if (File.Exists(InputFile))
            {
                var BAMFile = new Bam(InputFile);

                Console.WriteLine($"Extracting: {InputFile} to: {path}");

                if (Directory.Exists(path))
                    Directory.Delete(path, true);

                Directory.CreateDirectory(path);

                using (var writer = new StreamWriter(path + @"\Header.json")) //Write header values to a json
                    writer.Write(JsonConvert.SerializeObject(BAMFile.HeaderValues, Formatting.Indented));

                if (BAMFile.EplCount > 0)
                {
                    string zeros = "";
                    
                    for (int i = 0; i < BAMFile.EplCount.ToString().Length - 1; i++)
                    {
                        zeros += "0";
                    }

                    Directory.CreateDirectory(path + @"\Epl");
                    for (int i = 0; i < BAMFile.EplChunks.Count; i++)
                    {

                        var epl = BAMFile.EplChunks[i];
                        File.WriteAllBytes(path + $@"\Epl\{zeros}{i}.epl",epl.EplData);

                        using (var writer = new StreamWriter(path + $@"\Epl\{zeros}{i}A.json")) //Write unknown chunk values to a json
                            writer.Write(JsonConvert.SerializeObject(BAMFile.EplChunks[i].Values, Formatting.Indented));

                        using (var writer = new StreamWriter(path + $@"\Epl\{zeros}{i}B.json")) //Write unknown epl values to a json
                            writer.Write(JsonConvert.SerializeObject(BAMFile.EplChunks[i].EplValues, Formatting.Indented));
                    }
                }

                if (BAMFile.MtnChunks.Count > 0)
                {
                    Directory.CreateDirectory(path + @"\Mtn");

                    for (int i = 0; i < BAMFile.MtnChunks.Count; i++)
                    {
                        MtnChunk Mtn = BAMFile.MtnChunks[i];

                        using (var writer = new StreamWriter(path + $@"\Mtn\{i}.json"))
                            writer.Write(JsonConvert.SerializeObject(Mtn.MgdValues, Formatting.Indented));

                        
                        
                    }
                }

                if (BAMFile.MdlChunk != null)
                    File.WriteAllBytes(path + $@"\Mdl.cgfx", BAMFile.MdlChunk.ModelData);


                Console.WriteLine("Bam File Extracted");

                Exit();
            }
            else //if (!Directory.Exists(InputFile))
            {
                Console.WriteLine($"\n{InputFile} does not exist");
                Exit();
            }
        }
    }
}
