using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using OriginalCircuit.AltiumSharp.BasicTypes;
using OriginalCircuit.AltiumSharp.Records;
using OpenMcdf;

namespace OriginalCircuit.AltiumSharp
{
    public class IntLibCrossReference
    {
        public string LibRef { get; set; }
        public string SchLib { get; set; }
        public int PartsCount { get; set; }
        public string Description { get; set; }
        public string SchLibSource { get; set; }
        public bool HasFootprint { get; set; }
        public string Footprint { get; set; }
        public string FootprintFormat { get; set; }
        public bool HasPCBLib { get; set; }
        public string PCBLib { get; set; }
        public string PCBLibSource { get; set; }
    }

    public class IntLibComponent
    {
        public SchLib schLib { get; set; }
        public PcbLib? pcbLib { get; set; }

        public IntLibComponent(SchLib schLib)
        {
            this.schLib = schLib;
        }
    }

    public class IntLibData
    {
        public List<byte> Version { get; set; } = new List<byte>();
        public List<IntLibCrossReference> CrossReferences { get; } = new List<IntLibCrossReference>();
        public ParameterCollection Parameters { get; set; } = new ParameterCollection();
        public Dictionary<string, IntLibComponent> Components { get; set; }

        public IntLibData()
        {
        }
    }

    /// <summary>
    /// Integrated library reader.
    /// </summary>
    public sealed class IntLibReader : CompoundFileReader<IntLibData>
    {
        protected override void DoRead()
        {
            BeginContext("Reading integrated library");

            Data.Components = new Dictionary<string, IntLibComponent>();

            ReadVersion();
            ReadIntLibParameters();
            ReadCrossReferences();

            ReadLibs();

            EndContext();
        }

        void ReadLibs()
        {
            BeginContext("Reading parts");

            try
            {
                Data.Components.Clear();

                List<CFItem> schStreams = Cf.GetStorage("SchLib").Children().ToList();
                List<CFItem> pcbStreams = Cf.GetStorage("PCBLib").Children().ToList();

                foreach (IntLibCrossReference xref in Data.CrossReferences)
                {
                    if (string.IsNullOrEmpty(xref.SchLib))
                    {
                        continue;
                    }

                    CFItem schStream = schStreams.FirstOrDefault(s => s.Name.ToLower() == xref.SchLib.Replace(":\\SchLib\\", "").ToLower());
                    if (schStream == null)
                    {
                        continue;
                    }

                    IntLibComponent? component = null;

                    using (BinaryReader schStreamReader = ((CFStream)schStream).GetBinaryReader(Encoding.ASCII))
                    {
                        schStreamReader.BaseStream.Position = 1;
                        byte[] compressedSchLib = schStreamReader.ReadBytes((int)schStreamReader.BaseStream.Length - 1);
                        byte[] decompressedSchLib = ParseCompressedZlibData(compressedSchLib, stream =>
                        {
                            using (var modelReader = new BinaryReader(stream))
                            {
                                return modelReader.ReadBytes((int)stream.Length);
                            }
                        });
                        using (SchLibReader schLibReader = new SchLibReader())
                        {
                            component = new IntLibComponent((SchLib)schLibReader.Read(new MemoryStream(decompressedSchLib)));
                            Data.Components.Add(xref.LibRef, component);
                        }
                    }

                    if (xref.HasFootprint && xref.HasFootprint && component != null)
                    {
                        CFItem pcbStream = pcbStreams.FirstOrDefault(s => s.Name.ToLower() == xref.PCBLib.Replace(":\\PCBLib\\", "").ToLower());
                        if (pcbStream == null)
                        {
                            continue;
                        }

                        using (BinaryReader pcbStreamReader = ((CFStream)pcbStream).GetBinaryReader(Encoding.ASCII))
                        {
                            pcbStreamReader.BaseStream.Position = 1;
                            byte[] compressedPcbLib = pcbStreamReader.ReadBytes((int)pcbStreamReader.BaseStream.Length - 1);
                            byte[] decompressedPcbLib = ParseCompressedZlibData(compressedPcbLib, stream =>
                            {
                                using (var modelReader = new BinaryReader(stream))
                                {
                                    return modelReader.ReadBytes((int)stream.Length);
                                }
                            });
                            using (PcbLibReader pcbLibReader = new PcbLibReader())
                            {
                                try
                                {
                                    component.pcbLib = (PcbLib)pcbLibReader.Read(new MemoryStream(decompressedPcbLib));
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine(e);
                                }
                            }
                        }
                    }
                }
            }
            finally
            {
                EndContext();
            }
        }

        void ReadIntLibParameters()
        {
            BeginContext("Reading parameters");

            try
            {
                using (BinaryReader reader = Cf.GetStream("Parameters   .bin").GetBinaryReader())
                {
                    int firstByte = reader.ReadByte();
                    Data.Parameters = ReadBlock(reader, size => ReadParameters(reader, size));
                }
            }
            finally
            {
                EndContext();
            }
        }

        void ReadVersion()
        {
            BeginContext("Reading version information");

            try
            {
                using (BinaryReader reader = Cf.GetStream("Version.txt").GetBinaryReader())
                {
                    Data.Version = reader.ReadBytes(5).ToList();
                }
            }
            finally
            {
                EndContext();
            }
        }

        void ReadCrossReferences()
        {
            BeginContext("Reading cross references");

            try
            {
                Data.CrossReferences.Clear();

                using (BinaryReader reader = Cf.GetStream("LibCrossRef.txt").GetBinaryReader())
                {

                    byte firstByte = reader.ReadByte();

                    int recordCount = reader.ReadInt32();
                    for (int i = 0; i < recordCount; i++)
                    {
                        IntLibCrossReference xref = ReadCrossReferenceRecord(reader);
                        Data.CrossReferences.Add(xref);
                    }
                }
            }
            finally
            {
                EndContext();
            }
        }

        static IntLibCrossReference ReadCrossReferenceRecord(BinaryReader reader)
        {
            IntLibCrossReference xref = new IntLibCrossReference();

            xref.LibRef = ReadStringBlock(reader);
            xref.SchLib = ReadStringBlock(reader);
            xref.PartsCount = reader.ReadInt32();
            xref.Description = ReadStringBlock(reader);
            xref.SchLibSource = ReadStringBlock(reader);
            xref.HasFootprint = reader.ReadInt32() != 0;
            if (xref.HasFootprint)
            {
                xref.Footprint = ReadStringBlock(reader);
                xref.FootprintFormat = ReadStringBlock(reader);
                xref.HasPCBLib = reader.ReadInt32() != 0;
                if (xref.HasPCBLib)
                {
                    xref.PCBLib = ReadStringBlock(reader);
                    xref.PCBLibSource = ReadStringBlock(reader);
                }
            }

            return xref;
        }
    }
}
