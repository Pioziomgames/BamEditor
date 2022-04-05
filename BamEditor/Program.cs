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
            
            string InputFile = "";

            if (args.Length > 0)
                InputFile = args[0];
            else
            { 
                Console.WriteLine($"BamEditor v0.6.1\n" +
                    $"Extracts BAM files found in Persona Q and som 3DS Etrian Odyssey games\n" +
                    $"Usage:\n" +
                    $"       BamEditor.exe InputFile (optional)OutputFolder\n"+
                    $"       BamEditor.exe InputFolder (optional)OutputFile");
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
                        zeros += "0";

                    Directory.CreateDirectory(path + @"\Epl");
                    for (int i = 0; i < BAMFile.EplChunks.Count; i++)
                    {

                        var epl = BAMFile.EplChunks[i];
                        File.WriteAllBytes(path + $@"\Epl\{zeros}{i}.epl",epl.EplData);

                        using (var writer = new StreamWriter(path + $@"\Epl\{zeros}{i}.json")) //Write the unknown chunk value to a json
                            writer.Write(JsonConvert.SerializeObject(BAMFile.EplChunks[i].EplValue, Formatting.Indented));

                        using (var writer = new StreamWriter(path + $@"\Epl\{zeros}{i}ID.json")) //Write epl id values to a json 
                            writer.Write(JsonConvert.SerializeObject(BAMFile.EplChunks[i].Ids, Formatting.Indented)); //(most likely something similar to per3helper id)
                    }
                }

                if (BAMFile.MtnChunks.Count > 0)
                {
                    string zeros = "";
                    for (int i = 0; i < BAMFile.MtnChunks.Count.ToString().Length - 1; i++)
                        zeros += "0";

                    Directory.CreateDirectory(path + @"\Mtn");

                    for (int i = 0; i < BAMFile.MtnChunks.Count; i++)
                    {
                        MtnChunk Mtn = BAMFile.MtnChunks[i];

                        using (var writer = new StreamWriter(path + $@"\Mtn\{zeros}{i}A.json"))
                            writer.Write(JsonConvert.SerializeObject(Mtn.MgdValues, Formatting.Indented));
                        
                        using (var writer = new StreamWriter(path + $@"\Mtn\{zeros}{i}B.json"))
                            writer.Write(JsonConvert.SerializeObject(Mtn.MgdValuesB, Formatting.Indented));
                        
                        
                    }
                }

                if (BAMFile.MdlChunk != null)
                    File.WriteAllBytes(path + $@"\{Path.GetFileNameWithoutExtension(InputFile)}.cgfx", BAMFile.MdlChunk.ModelData);


                Console.WriteLine("Bam File Extracted");

            }
            else if (!Directory.Exists(InputFile))
            {
                Console.WriteLine($"\n{InputFile} does not exist");
                Exit();
            }
            else
            {
                Bam CreatedBam = new Bam();

                string[] cgfxFiles = Directory.GetFiles(InputFile,"*.cgfx",SearchOption.TopDirectoryOnly);

                if (cgfxFiles.Length > 1)
                {
                    Console.WriteLine($"\nWARNING: More than one cgfx file\nEmbedding only: {Path.GetFileName(cgfxFiles[0])}");
                }
                else if (cgfxFiles.Length == 0)
                {
                    throw new Exception("No cgfx files found");
                }

                byte[] cgfx = File.ReadAllBytes(cgfxFiles[0]);

                CreatedBam.MdlChunk = new MdlChunk(cgfx);

                List<EplChunk> EplList = new List<EplChunk>();
                if (Directory.Exists(InputFile + "\\Epl"))
                {
                    
                    string[] Epls = Directory.GetFiles(InputFile + "\\Epl","*.json");
                    int EplCount = Epls.Length/2;
                    for (int i = 0; i < EplCount; i++)
                    {
                        EplChunk eplChunk = new EplChunk();
                        eplChunk.EplData = File.ReadAllBytes($"{InputFile}\\Epl\\{i}.epl");

                        using (StreamReader reader = new StreamReader($"{InputFile}\\Epl\\{i}ID.json"))
                        {
                            string IdsJson = reader.ReadToEnd();
                            ushort[] Ids = JsonConvert.DeserializeObject<ushort[]>(IdsJson);
                            eplChunk.Ids = Ids;
                        }

                        using (StreamReader reader = new StreamReader($"{InputFile}\\Epl\\{i}.json"))
                        {
                            string IdsJson = reader.ReadToEnd();
                            int unk = JsonConvert.DeserializeObject<int>(IdsJson);
                            eplChunk.EplValue = unk;
                        }
                        EplList.Add(eplChunk);

                    }
                    
                }
                CreatedBam.EplChunks = EplList;

                if (Directory.Exists(InputFile + "\\Mtn"))
                {
                    List<MtnChunk> Mtns = new List<MtnChunk>();
                    string[] Jsons = Directory.GetFiles(InputFile + "\\Mtn","*.json");

                    int Count= Jsons.Length/2;

                    for (int i =0; i < Count; i++)
                    {
                        MtnChunk Mtn = new MtnChunk();

                        using (StreamReader reader = new StreamReader($"{InputFile}\\Mtn\\{i}A.json"))
                        {
                            string unkJson = reader.ReadToEnd();
                            List<int> unk = JsonConvert.DeserializeObject<List<int>>(unkJson);
                            Mtn.MgdValues = unk;
                        }

                        using (StreamReader reader = new StreamReader($"{InputFile}\\Mtn\\{i}B.json"))
                        {
                            string unkJson = reader.ReadToEnd();
                            List<int> unk = JsonConvert.DeserializeObject<List<int>>(unkJson);
                            Mtn.MgdValuesB = unk;
                        }

                        Mtns.Add(Mtn);
                    }
                    CreatedBam.MtnChunks = Mtns;
                }
                else
                {
                    CreatedBam.MtnChunks = new List<MtnChunk>();
                }


                if (File.Exists(InputFile + "\\Header.json"))
                {
                    using (StreamReader reader = new StreamReader($"{InputFile}\\Header.json"))
                    {
                        string HeaderJson = reader.ReadToEnd();
                        List<int> headerValues = JsonConvert.DeserializeObject<List<int>>(HeaderJson);
                        CreatedBam.HeaderValues = headerValues;
                    }
                }
                else
                {
                    throw new Exception("header.json does not exist");
                }

                CreatedBam.Save($"{InputFile}.bam");
            }
        }
    }
}
