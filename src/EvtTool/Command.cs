using System.Text;
using EvtTool.IO;
using EvtTool.Json.Converters;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace EvtTool
{
    [JsonConverter(typeof( CommandJsonConverter ) )]
    public sealed class Command
    {
        internal const int SIZE = 0x30;

        public string CommandCode { get; set; }

        public int CommandVersion { get; set; }

        public int CommandType { get; set; }

        public int ObjectId { get; set; }

        public int Flags { get; set; }

        public int FrameStart { get; set; }

        public int FrameDuration { get; set; }

        public int DataSize { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public EvtConditionalType ConditionalType { get; set; }

        [JsonConverter(typeof(HexStringJsonConverter))]
        public uint ConditionalIndex { get; set; }

        public int ConditionalValue { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public EvtConditionalComparisonType ConditionalComparisonType { get; set; }

        [JsonConverter(typeof( DontDeserializeJsonConverter ) )]
        public CommandData Data { get; set; }

        internal void Read( EndianBinaryReader reader )
        {
            CommandCode = Encoding.ASCII.GetString( reader.ReadBytes( 4 ) );
            CommandVersion = reader.ReadInt16();
            CommandType = reader.ReadInt16();
            ObjectId = reader.ReadInt32();
            Flags = reader.ReadInt32();
            FrameStart = reader.ReadInt32();
            FrameDuration = reader.ReadInt32();
            var dataOffset = reader.ReadInt32();
            DataSize = reader.ReadInt32();
            ConditionalType = (EvtConditionalType)reader.ReadInt32();
            ConditionalIndex = reader.ReadUInt32(); // FlagConvert();
            ConditionalValue = reader.ReadInt32();
            ConditionalComparisonType = (EvtConditionalComparisonType)reader.ReadInt32();

            reader.ReadAtOffset( dataOffset, () =>
            {
                Data = CommandDataFactory.Create( CommandCode );
                Data.Read( this, reader );
            });
        }

        internal void Write( EndianBinaryWriter writer )
        {
            writer.Write( Encoding.ASCII.GetBytes( CommandCode ) );
            writer.Write( (short)CommandVersion );
            writer.Write( (short)CommandType );
            writer.Write( ObjectId );
            writer.Write( Flags );
            writer.Write( FrameStart );
            writer.Write( FrameDuration );
            writer.ScheduleOffsetWrite( () => Data.Write( this, writer ) );
            writer.Write( DataSize );
            writer.Write( (int)ConditionalType );
            writer.Write(ConditionalIndex);
            writer.Write(ConditionalValue);
            writer.Write( (int)ConditionalComparisonType );
        }

        public enum EvtConditionalType
        {
            None = 0,
            False = 1,
            Evt_Local_Data = 2,
            Bitflag = 3,
            Count = 4,
            Evt_Anim_Data = 5
        }

        public enum EvtConditionalComparisonType
        { 
            FlagValue_Equals_FlagIdResult = 0,
            FlagValue_DoesNotEqual_FlagIdResult = 1,
            FlagValue_IsLessThan_FlagIdResult = 2,
            FlagValue_IsMoreThan_FlagIdResult = 3,
            FlagValue_IsLessThanEqualTo_FlagIdResult = 4,
            FlagValue_IsMoreThanEqualTo_FlagIdResult = 5,
        }

        public void FlagConvert()
        {
            if (ConditionalType == EvtConditionalType.Bitflag)
            {
                if (ConditionalIndex  >= 0x5000000)
                { ConditionalIndex = (ConditionalIndex - 0x5000000) + 12288; }

                else if (ConditionalIndex >= 0x4000000)
                { ConditionalIndex = (ConditionalIndex - 0x4000000) + 11776; }

                else if (ConditionalIndex >= 0x3000000)
                { ConditionalIndex = (ConditionalIndex - 0x3000000) + 11264; }

                else if (ConditionalIndex >= 0x2000000)
                { ConditionalIndex = (ConditionalIndex - 0x2000000) + 6144; }

                else if (ConditionalIndex >= 0x1000000)
                { ConditionalIndex = (ConditionalIndex - 0x1000000) + 3072; }
            }
        }
    }
}