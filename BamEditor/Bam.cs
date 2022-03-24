using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace BamEditor
{
    public class Bam
    {
        public int FileSize { get; set; }
        public int EplCount { get; set; }
        public int HeaderSize { get; set; }

        public int EplOffset { get; set; }
        public int ModelDataOffset { get; set; }
        public int AdditionalDataOffset { get; set; }

        public List<int> HeaderValues { get; set; }

        public List<MtnChunk> MtnChunks { get; set; }
        public List<EplChunk> EplChunks { get; set; }
        public MdlChunk MdlChunk { get; set; }

        public Bam(string Path)
        {
            using (BinaryReader reader = new BinaryReader(File.OpenRead(Path)))
                Read(reader);
        }
        public Bam(BinaryReader reader)
        {
            Read(reader);
        }

        private void Read(BinaryReader reader)
        {
            if (new string(reader.ReadChars(4)) != "ATBC")
            {
                throw new Exception("Not a proper BAM model archive");
            }

            FileSize = reader.ReadInt32();
            EplCount = reader.ReadInt32();
            HeaderSize = reader.ReadInt32();

            EplOffset = reader.ReadInt32();
            ModelDataOffset = reader.ReadInt32();
            AdditionalDataOffset = reader.ReadInt32();
            reader.ReadBytes(4); //padding

            HeaderValues = new List<int>();

            for (int i = 0; i < HeaderSize; i++)
                HeaderValues.Add(reader.ReadInt32());

            reader.BaseStream.Seek(AdditionalDataOffset, SeekOrigin.Begin);

            MtnChunks = new List<MtnChunk>();

            while (true)
            {
                if (reader.BaseStream.Position == EplOffset)
                {
                    break;
                }
                if (reader.ReadByte() == 0)
                {
                    reader.BaseStream.Seek(EplOffset, SeekOrigin.Begin);
                    break;
                }
                else 
                {
                    reader.BaseStream.Seek(-1, SeekOrigin.Current);
                    MtnChunks.Add(new MtnChunk(reader));
                }

            }


            EplChunks = new List<EplChunk>();

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
        public string Name { get; set; }
        public int Size { get; set; }
        public ushort valueCount { get; set; }
        public List<int> Values { get; set; }
        public List<int> EplValues { get; set; }
        public byte[] EplData { get; set; }

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
            Name = new string(reader.ReadChars(8));
            Size = reader.ReadInt32();
            reader.ReadBytes(4);

            valueCount = reader.ReadUInt16();
            valueCount = reader.ReadUInt16();
            Values = new List<int>();
            EplValues = new List<int>();
            for (int i = 0; i < valueCount; i++)
                Values.Add(reader.ReadInt32());

            for (int i = 0; i < 4; i++)
                EplValues.Add(reader.ReadInt32());
            

            int EplSize = Size - 36 - valueCount * 4;

            EplData = reader.ReadBytes(EplSize);
        }

    }

    public class MtnChunk
    {
        public string Name { get; set; }
        public int FakeSize { get; set; }
        //Mgd
        public string MgdMagic { get; set; }
        public int MgdCount { get; set; }
        public int MgdFullSize { get; set; }
        public int MgdValueCount { get; set; }
        public List<int> MgdValues { get; set; }
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
            Name = new string(reader.ReadChars(8));
            FakeSize = reader.ReadInt32();
            reader.ReadBytes(4); //padding

            //Mgd Data

            MgdMagic = new string(reader.ReadChars(4));
            MgdCount = reader.ReadInt32();
            MgdFullSize = reader.ReadInt32();
            MgdValueCount = reader.ReadInt32();
            MgdValues = new List<int>();
            for (int i = 0;i < MgdValueCount;i++)
                MgdValues.Add(reader.ReadInt32());

            int padCount = (MgdFullSize * 4) - 32 - (MgdValueCount * 4);


            reader.ReadBytes(padCount);



        }

    }

    public class MdlChunk
    {
        public string Name { get; set; }
        public int Size { get; set; }
        public byte[] ModelData { get; set; }

        public MdlChunk(string Path)
        {
            using (BinaryReader reader = new BinaryReader(File.OpenRead(Path)))
                Read(reader);
        }

        public MdlChunk(BinaryReader reader)
        {
            Read(reader);
        }

        public void Read(BinaryReader reader)
        {
            Name = new string(reader.ReadChars(8));
            Size = reader.ReadInt32();
            reader.ReadBytes(116); // very cool padding
            ModelData = reader.ReadBytes(Size);

        }
    }
}
