using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace BamEditor
{
    public class GeneralStuff
    {
        public static long Align(long number, int numberToAlignTo)
        {
            return (number + (numberToAlignTo - 1)) & ~(numberToAlignTo - 1);
        }
    }
    
    public class Bam
    {
        public int EplCount { get { return EplChunks.Count; } }
        public int HeaderSize { get; set; }

        public int EplOffset { get; set; }
        public int ModelDataOffset { get; set; }
        public int AdditionalDataOffset { get; set; }

        public List<int> HeaderValues { get; set; }

        public List<MtnChunk> MtnChunks { get; set; }
        public List<EplChunk> EplChunks { get; set; }
        public MdlChunk MdlChunk { get; set; }
        public bool ABCH { get; set; }
        public Bam()
        {
            ABCH = false;
        }
        public Bam(string Path)
        {
            using (BinaryReader reader = new BinaryReader(File.OpenRead(Path)))
                Read(reader);
        }
        public Bam(BinaryReader reader)
        {
            Read(reader);
        }
        public void Save(string Path)
        {
            using (BinaryWriter writer = new BinaryWriter(File.OpenWrite(Path)))
            {
                Write(writer);
                writer.Flush();
                writer.Close();
            }
        }
        public void Save(BinaryWriter writer)
        {
            Write(writer);
        }
        private void Write(BinaryWriter writer)
        {
            long FileStart = writer.BaseStream.Position;
            if (ABCH)
                writer.Write("ABCH".ToCharArray());
            else
                writer.Write("ATBC".ToCharArray());
            writer.Write(0);
            writer.Write(EplChunks.Count);
            writer.Write(HeaderValues.Count);

            if (HeaderValues.Count == 0 && EplChunks.Count == 0 && MtnChunks.Count == 0)
            {
                writer.Write(0);
                writer.Write(128);
                writer.Seek(104, SeekOrigin.Current);
                MdlChunk.Save(writer);

                writer.BaseStream.Position += GeneralStuff.Align(writer.BaseStream.Position - FileStart, 256) - (writer.BaseStream.Position - FileStart);
                uint size = (uint)(writer.BaseStream.Position - FileStart);
                writer.BaseStream.Position = FileStart + 4;
                writer.Write(size);
                writer.BaseStream.SetLength(FileStart+size);
                return;
            }


            long mtnSignOffset = (HeaderValues.Count * 4) + 32;
            long eplOffset = mtnSignOffset;
            for (int i=0; i < MtnChunks.Count;i++)
                eplOffset += MtnChunks[i].ChunkSize;

            long mdlSignOffset = eplOffset;
            for (int i = 0; i < EplChunks.Count; i++)
                mdlSignOffset += EplChunks[i].ChunkSize;

            mdlSignOffset = GeneralStuff.Align(mdlSignOffset, 256);

            if (MtnChunks.Count == 0)
                mtnSignOffset = 0;
            //if (EplChunks.Count == 0)
            //    eplOffset = 0;

            writer.Write((int)eplOffset);
           
            writer.Write((int)mdlSignOffset);
            writer.Write((int)mtnSignOffset);

            writer.Write(0); //reserved

            for (int i=0; i < HeaderValues.Count;i++)
                writer.Write(HeaderValues[i]);

            long MtnOffset = 0;
            for (int i= 0; i < MtnChunks.Count;i++)
            {
                MtnOffset += MtnChunks[i].ChunkSize;
                MtnChunks[i].EndOffset = (int)MtnOffset;
                MtnChunks[i].Save(writer);
            }
                

            for (int i=0; i < EplChunks.Count;i++)
                EplChunks[i].Save(writer);

            writer.BaseStream.Position = FileStart + mdlSignOffset;

            MdlChunk.Save(writer);

            writer.BaseStream.Position += GeneralStuff.Align(writer.BaseStream.Position - FileStart, 256) - (writer.BaseStream.Position - FileStart);

            uint Filesize = (uint)(writer.BaseStream.Position-FileStart);
            writer.BaseStream.Position = FileStart + 4;
            writer.Write(Filesize);
            writer.BaseStream.SetLength(FileStart+Filesize);
        }

        private void Read(BinaryReader reader)
        {
            string MAGIC = new string(reader.ReadChars(4));
            if (MAGIC == "ATBC")
                ABCH = false;
            else if (MAGIC == "ABCH")
                ABCH = true;
            else
                throw new Exception("Not a proper BAM model archive");
            int FileSize = reader.ReadInt32();
            int EplCount = reader.ReadInt32();
            HeaderSize = reader.ReadInt32();

            EplOffset = reader.ReadInt32();
            ModelDataOffset = reader.ReadInt32();
            AdditionalDataOffset = reader.ReadInt32();
            reader.ReadBytes(4); //padding

            HeaderValues = new List<int>();

            for (int i = 0; i < HeaderSize; i++)
                HeaderValues.Add(reader.ReadInt32());


            MtnChunks = new List<MtnChunk>();

            if (AdditionalDataOffset != 0)
                reader.BaseStream.Seek(AdditionalDataOffset, SeekOrigin.Begin);


            while (true)
            {
                if (reader.BaseStream.Position == EplOffset || reader.ReadByte() == 0)
                {
                    break;
                }

                reader.BaseStream.Seek(-1, SeekOrigin.Current);
                MtnChunks.Add(new MtnChunk(reader));
            }

            EplChunks = new List<EplChunk>();

            if (EplOffset != 0)
                reader.BaseStream.Seek(EplOffset, SeekOrigin.Begin);

            if (EplCount > 0)
            {
                for (int i = 0; i < EplCount; i++)
                {
                    EplChunks.Add(new EplChunk(reader));
                }
            }
            reader.BaseStream.Seek(ModelDataOffset, SeekOrigin.Begin);

            MdlChunk = new MdlChunk(reader);

            reader.BaseStream.Seek(FileSize, SeekOrigin.Begin);

        }
    }

    public class EplChunk
    {
        public long ChunkSize { get { return GeneralStuff.Align(EplData.Length + 36, 16); } }
        //public string Name { get; set; }
        public int Size { get; set; }
        public ushort ValueCount { get; set; }
        public List<int> Values { get; set; }
        public int EplValue { get; set; }
        public byte[] EplData { get; set; }

        public ushort[] Ids { get; set; }
        public EplChunk()
        {

        }
        public EplChunk(string Path)
        {
            using (BinaryReader reader = new BinaryReader(File.OpenRead(Path)))
                Read(reader);
        }

        public EplChunk(BinaryReader reader)
        {
            Read(reader);
        }
        void Read(BinaryReader reader)
        {
            string Magic = new string(reader.ReadChars(8));
            Size = reader.ReadInt32();
            reader.ReadBytes(4);

            ushort id1 = reader.ReadUInt16();
            ushort id2 = reader.ReadUInt16();
            Ids = new ushort[]{id1, id2 };
            
            int EplEndOffset = reader.ReadInt32();

            reader.ReadBytes(8);

            EplValue = reader.ReadInt32();

            int EplSize = Size - 36;

            EplData = reader.ReadBytes(EplSize);
        }

        public void Save(BinaryWriter writer)
        {
            Write(writer);
        }

        public void Save(string Path)
        {
            using (BinaryWriter writer = new BinaryWriter(File.OpenWrite(Path)))
                Write(writer);
        }

        void Write(BinaryWriter writer)
        {
            writer.Write("EPLSIGN\0".ToCharArray());

            
            writer.Write((int)GeneralStuff.Align(EplData.Length + 36,16));

            for (int i = 0; i < 4;i++)
                writer.Write((byte)0);

            for (int i = 0; i < 2; i++)
                writer.Write(Ids[i]);
            writer.Write(EplData.Length);

            for (int i = 0; i < 8; i++)
                writer.Write((byte)0);

            writer.Write(EplValue);

            writer.Write(EplData);

            for (int i = 0; i < GeneralStuff.Align(EplData.Length + 36,16) - 36- EplData.Length;i++)
                writer.Write((byte)0);
            
        }

    }

    public class MtnChunk
    {
        public long ChunkSize {
            get 
            {
                return 144;
            } 
        }
        //public string Name { get; set; }
        public int EndOffset { get; set; }
        //Mgd
        //public string MgdMagic { get; set; }
        public int MgdCount { get; set; }
        public int MgdFullSize { get; set; }
        //public int MgdValueCount { get; set; }
        public List<int> MgdValues { get; set; }
        public List<int> MgdValuesB { get; set; }
        
        public void Save(BinaryWriter writer)
        {
            Write(writer);
        }

        public void Save(string Path)
        {
            using (BinaryWriter writer = new BinaryWriter(File.OpenWrite(Path)))
                Write(writer);
        }
        public MtnChunk()
        {
        }
        public MtnChunk(string Path)
        {
            using (BinaryReader reader = new BinaryReader(File.OpenRead(Path)))
                Read(reader);
        }

        public MtnChunk(BinaryReader reader)
        {
            Read(reader);
        }

        private void Read(BinaryReader reader)
        {
            string Magic = new string(reader.ReadChars(8));
            EndOffset = reader.ReadInt32(); //does not seem to mean much
            reader.ReadBytes(4); //reserved

            //Mgd Data

            string MgdMagic = new string(reader.ReadChars(4));
            MgdValuesB = new List<int>();
            MgdValuesB.Add(reader.ReadInt32()); //seems to always be one
            MgdValuesB.Add(reader.ReadInt32());

            int MgdValueCount = reader.ReadInt32();
            MgdValues = new List<int>();
            for (int i = 0;i < MgdValueCount;i++)
                MgdValues.Add(reader.ReadInt32());

            //(144 - 32) = 112
            int padCount = 112 - (MgdValueCount * 4);

            reader.ReadBytes(padCount); //padding
        }

        private void Write(BinaryWriter writer)
        {
            MgdFullSize = MgdValues.Count + 16;

            writer.Write("MTNSIGN\0".ToCharArray());
            writer.Write(EndOffset); 

            writer.Write((int)0); //reserved

            //start of mgd data

            writer.Write("MGD\0".ToCharArray());
            for (int i = 0; i < MgdValuesB.Count;i++)
                writer.Write(MgdValuesB[i]);

            writer.Write(MgdValues.Count);
            for (int i = 0; i < MgdValues.Count;i++)
                writer.Write(MgdValues[i]);

            int padCount = 144 - 32 - (MgdValues.Count * 4);

            for (int i =0; i < padCount; i++)
                writer.Write((byte)0);
        }


    }

    public class MdlChunk
    {
        public long ChunkSize { get { return ModelData.Length + 128; } }
        //public string Name { get; set; }
        //public int Size { get; set; }
        public byte[] ModelData { get; set; }

        public MdlChunk()
        {
        }
        public MdlChunk(string Path)
        {
            using (BinaryReader reader = new BinaryReader(File.OpenRead(Path)))
                Read(reader);
        }

        public MdlChunk(BinaryReader reader)
        {
            Read(reader);
        }

        public MdlChunk(byte[] modelData)
        {
            ModelData = modelData;
        }
        
        public void Save(BinaryWriter writer)
        {
            Write(writer);
        }

        public void Save(string Path)
        {
            using (BinaryWriter writer = new BinaryWriter(File.OpenWrite(Path)))
                Write(writer);
        }

        private void Read(BinaryReader reader)
        {
            string MAGIC = new string(reader.ReadChars(8));
            int Size = reader.ReadInt32();
            reader.ReadBytes(116); // very cool padding
            ModelData = reader.ReadBytes(Size);

        }
        

        private void Write(BinaryWriter writer)
        {
            writer.Write("MDLSIGN\0".ToCharArray());
            writer.Write(ModelData.Length);

            for (int i = 0; i < 116; i++) // padding
                writer.Write((byte)0);

            writer.Write(ModelData);


        }

    }
}
