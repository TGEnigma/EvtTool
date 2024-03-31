using EvtTool.IO;
using EvtTool.Json.Converters;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace EvtTool
{
    public sealed class EvtObject
    {
        internal const int SIZE = 0x30;

        public int Id { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public EvtObjectType Type { get; set; }

        public int ResourceCategory { get; set; }

        public int ResourceUniqueId { get; set; }

        public int ResourceMajorId { get; set; }

        public short ResourceSubId { get; set; }

        public short ResourceMinorId { get; set; }

        [JsonConverter(typeof(HexStringJsonConverter))]
        public uint Flags { get; set; }

        public int BaseMotionNo { get; set; }

        public int ExtBaseMotionNo { get; set; }

        public int ExtAddMotionNo { get; set; }

        public int Reserve28 { get; set; }

        public int Reserve2C { get; set; }

        public EvtObject()
        {
            ResourceCategory = 1;
            BaseMotionNo = -1;
            ExtBaseMotionNo = -1;
            ExtAddMotionNo = -1;
        }

        internal void Read( EndianBinaryReader reader )
        {
            Id = reader.ReadInt32();
            Type = ( EvtObjectType ) reader.ReadInt32();
            ResourceCategory = reader.ReadInt32();
            ResourceUniqueId = reader.ReadInt32();
            ResourceMajorId = reader.ReadInt32();
            ResourceSubId = reader.ReadInt16();
            ResourceMinorId = reader.ReadInt16();
            Flags = reader.ReadUInt32();
            BaseMotionNo = reader.ReadInt32();
            ExtBaseMotionNo = reader.ReadInt32();
            ExtAddMotionNo = reader.ReadInt32();
            Reserve28 = reader.ReadInt32();
            Reserve2C = reader.ReadInt32();
        }

        internal void Write( EndianBinaryWriter writer )
        {
            writer.Write( Id );
            writer.Write( ( int ) Type );
            writer.Write( ResourceCategory );
            writer.Write( ResourceUniqueId );
            writer.Write( ResourceMajorId );
            writer.Write( ResourceSubId );
            writer.Write( ResourceMinorId );
            writer.Write( Flags );
            writer.Write( BaseMotionNo );
            writer.Write( ExtBaseMotionNo );
            writer.Write( ExtAddMotionNo );
            writer.Write( Reserve28 );
            writer.Write( Reserve2C );
        }
    }
}