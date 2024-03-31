using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using EvtTool.IO;
using EvtTool.Json.Converters;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace EvtTool
{
    [JsonObject( ItemRequired = Required.Always )]
    public sealed class EvtFile : ICommandList, ISaveable
    {
        private const int MAGIC = 0x45565400;

        public const int CURRENT_VERSION = 0x0002722E;
        public const int ROYAL_VERSION = 0x00029B9C;

        [JsonConverter(typeof(StringEnumConverter))]
        public EvtTool.IO.Endianness Endianness { get; set; }

        [JsonConverter(typeof(HexStringJsonConverter))]
        public uint Version { get; set; }

        public short MajorId { get; set; }

        public short MinorId { get; set; }

        public byte Rank { get; set; }

        public byte Level { get; set; }

        public int FileHeaderSize { get; set; }

        [JsonConverter(typeof(HexStringJsonConverter))]
        public uint Flags { get; set; }

        public int TotalFrame { get; set; }

        public byte FrameRate { get; set; }

        public byte InitScriptIndex { get; set; }

        public short StartFrame { get; set; }

        public short LetterBoxInFrame { get; set; }

        public int InitEnvAssetID { get; set; }

        public int InitEnvAssetIDDbg { get; set; }

        public string EventBmdPath { get; set; }

        public int EventBmdPathLength { get; set; }

        public int EmbedMsgFileOfs { get; set; }

        public int EmbedMsgFileSize { get; set; }

        public string EventBfPath { get; set; }

        public int EventBfPathLength { get; set; }

        public int EmbedBfFileOfs { get; set; }

        public int EmbedBfFileSize { get; set; }

        public int[] MarkerFrame { get; set; }


        public List<EvtObject> Objects { get; }

        public List<Command> Commands { get; }

        public EvtFile()
        {
            Version = CURRENT_VERSION;
            FrameRate = 30;
            InitEnvAssetID = 1;

            Objects = new List<EvtObject>();
            Commands = new List<Command>();
        }

        public EvtFile( string path ) : this()
        {
            using ( var stream = File.OpenRead( path ) )
                Read( new EndianBinaryReader( stream, Endianness.Big ) );
        }

        public void Save( string path )
        {
            using ( var stream = FileHelper.Create( path ) )
                Write( new EndianBinaryWriter( stream, Endianness.Big ) );
        }

        internal void Read( EndianBinaryReader reader )
        {
            Endianness = Endianness.Big;

            var magic = reader.ReadInt32();

            if ((magic & 0xFFFFFF00) != MAGIC)
            {
                throw new InvalidDataException("Magic value does not match");
            }

            // Get endianness from last byte of magic, 0 is LE
            Endianness = (Endianness)(magic & 0xFF);
            reader.Endianness = Endianness;

            Version = reader.ReadUInt32();
            MajorId = reader.ReadInt16();
            MinorId = reader.ReadInt16();
            Rank = reader.ReadByte();
            Level = reader.ReadByte();
            _ = reader.ReadInt16();
            var fileSize = reader.ReadInt32();
            FileHeaderSize = reader.ReadInt32();

            Flags = reader.ReadUInt32();
            TotalFrame = reader.ReadInt32();
            FrameRate = reader.ReadByte();
            InitScriptIndex = reader.ReadByte();
            StartFrame = reader.ReadInt16();
            LetterBoxInFrame = reader.ReadInt16();
            _ = reader.ReadInt16();
            InitEnvAssetID = reader.ReadInt32();
            InitEnvAssetIDDbg = reader.ReadInt32();
            var objectCount = reader.ReadInt32();
            var objectOffset = reader.ReadInt32();
            var objectSize = reader.ReadInt32();
            Trace.Assert( objectSize == EvtObject.SIZE, $"Object size != {EvtObject.SIZE}" );
            _ = reader.ReadInt32();
            var commandCount = reader.ReadInt32();
            var commandOffset = reader.ReadInt32();
            var commandSize = reader.ReadInt32();
            Trace.Assert( commandSize == Command.SIZE, $"Command size != {Command.SIZE}" );
            _ = reader.ReadInt32();
            var PointerToEventBmdPath = reader.ReadInt32();
            EventBmdPathLength = reader.ReadInt32();
            EmbedMsgFileOfs = reader.ReadInt32();
            EmbedMsgFileSize = reader.ReadInt32();
            var PointerToEventBfPath = reader.ReadInt32();
            EventBfPathLength = reader.ReadInt32();
            EmbedBfFileOfs = reader.ReadInt32();
            EmbedBfFileSize = reader.ReadInt32();

            int markerFrameCount;

            if (Version != ROYAL_VERSION)
            {
                markerFrameCount = 8;
            }
            else
            {
                markerFrameCount = 48;
            }

            MarkerFrame = reader.ReadInt32s(markerFrameCount);

            EventBmdPath = GetEvtString(reader, PointerToEventBmdPath);

            EventBfPath = GetEvtString(reader, PointerToEventBfPath);

            reader.SeekBegin( objectOffset );
            Objects.Capacity = objectCount;
            for ( int i = 0; i < objectCount; i++ )
            {
                var obj = new EvtObject();
                obj.Read( reader );
                Objects.Add( obj );
            }

            reader.SeekBegin( commandOffset );
            Commands.Capacity = commandCount;
            for ( int i = 0; i < commandCount; i++ )
            {
                var command = new Command();
                command.Read( reader );
                Commands.Add( command );
            }
        }
        
        internal string GetEvtString(EndianBinaryReader reader, int pointerToString)
        {
            if (pointerToString > 0)
            {
                reader.Seek(pointerToString, SeekOrigin.Begin);
                return reader.ReadString(StringBinaryFormat.NullTerminated);
            }
            else return "Null";
        }

        internal void Write( EndianBinaryWriter writer )
        {
            writer.Endianness = Endianness;
            
            if ( Endianness == Endianness.Big )
            {
                writer.Write(0x45565401);
            }
            else writer.Write( 0x00545645 );

            writer.Write( Version );
            writer.Write( MajorId );
            writer.Write( MinorId );
            writer.Write( Rank );
            writer.Write( Level );
            writer.Write( (short)0 );
            writer.ScheduleFileSizeWrite();
            writer.Write( FileHeaderSize );
            writer.Write( Flags );
            writer.Write( TotalFrame );
            writer.Write( FrameRate );
            writer.Write( InitScriptIndex );
            writer.Write( StartFrame );
            writer.Write( LetterBoxInFrame );
            writer.Write((short)0);
            writer.Write( InitEnvAssetID );
            writer.Write( InitEnvAssetIDDbg );
            writer.Write( Objects.Count );
            writer.ScheduleOffsetWrite( () => Objects.ForEach( o => o.Write( writer ) ) );
            writer.Write( EvtObject.SIZE );
            writer.Write( 0 );
            writer.Write( Commands.Count );
            writer.ScheduleOffsetWrite( () => Commands.ForEach( c => c.Write( writer ) ) );
            writer.Write( Command.SIZE );
            writer.Write( 0 );
            writer.Write( (int) 0 ); // dummy bmd pointer, field50
            //writer.Write( EventBmdPath );
            writer.Write( EventBmdPathLength );
            writer.Write( EmbedMsgFileOfs );
            writer.Write( EmbedMsgFileSize );
            writer.Write((int)0); // dummy bf pointer, field60
            //writer.Write( EventBfPath );
            writer.Write( EventBfPathLength );
            writer.Write( EmbedBfFileOfs );
            writer.Write( EmbedBfFileSize );

            writer.Write(MarkerFrame);

            writer.PerformScheduledWrites();

            WriteEvtString(writer, EventBmdPath, 0x50);

            WriteEvtString(writer, EventBfPath, 0x60);
        }

        void WriteEvtString(EndianBinaryWriter writer, string filePath, int pointerToString)
        {
            if (filePath != "Null")
            {
                // original EVT files have bmd string at the end, this makes no difference but i want them to be as 1:1 as possible :raidoufrost:
                writer.SeekBegin(writer.Length);
                int currentPos = (int)writer.Position;

                var padding = 0x10 - (filePath.Length % 0x10);

                writer.Write(filePath, StringBinaryFormat.FixedLength, filePath.Length + padding);

                // write string offset
                writer.SeekBegin(pointerToString);
                writer.Write(currentPos);
                writer.Write(filePath.Length + padding);

                // fix filesize
                writer.SeekBegin(0x10);
                writer.Write((int)writer.Length);
            }
        }
    }
}