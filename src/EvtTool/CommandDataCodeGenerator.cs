using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EvtTool.IO;

namespace EvtTool
{
    internal static class CommandDataCodeGenerator
    {
        public static void Generate( string evtDirectoryPath, string ecsDirectoryPath )
        {
            var commandDataStructures = new Dictionary<string, List<Structure>>();

            foreach ( var file in
                Directory.EnumerateFiles( evtDirectoryPath, "*.EVT" )
                         .Concat( Directory.EnumerateFiles( ecsDirectoryPath, "*.ECS" ) ) )
            {
                ICommandList list;
                if ( file.EndsWith( ".EVT" ) )
                {
                    list = new EvtFile( file );
                }
                else
                {
                    list = new EcsFile( file );
                }

                foreach ( var command in list.Commands )
                {
                    var data = command.Data as UnknownCommandData;
                    if ( data == null )
                    {
                        commandDataStructures[command.CommandCode] = null;
                        continue;
                    }

                    var structure = new Structure( command.CommandCode );
                    var integers = new List<int>();
                    for ( int i = 0; i < data.Bytes.Length; i += 4 )
                    {
                        integers.Add( data.Bytes[i] << 24 | data.Bytes[i + 1] << 16 | data.Bytes[i + 2] << 8 | data.Bytes[i + 3] );
                    }

                    for ( var i = 0; i < integers.Count; i++ )
                    {
                        var integer = integers[i];
                        object value = integer;
                        var type = typeof( int );
                        if ( IsLikelyFloat( integer ) )
                        {
                            value = Unsafe.ReinterpretCast<int, float>( integer );
                            type = typeof( float );
                        }

                        structure.Fields.Add( new Field( i * 4, type, value ) );
                    }

                    if ( !commandDataStructures.ContainsKey( command.CommandCode ) )
                        commandDataStructures[command.CommandCode] = new List<Structure>();

                    commandDataStructures[command.CommandCode].Add( structure );
                }
            }

            using ( var writer = File.CreateText( "out.txt" ) )
            {
                foreach ( string commandType in commandDataStructures.Keys.OrderBy( x => x ) )
                {
                    var commandTypeNormalized = commandType.Replace( "_", "" );
                    writer.WriteLine( $"        case FourCC( \"{commandType}\" ): struct CommandData_{commandTypeNormalized} Data; break;" );
                }

                writer.WriteLine();

                foreach ( string commandType in commandDataStructures.Keys.OrderBy( x => x ) )
                {
                    var commandTypeNormalized = commandType.Replace( "_", "" );
                    writer.WriteLine( $"        case \"{commandType}\": return new {commandTypeNormalized}CommandData();" );
                }

                writer.WriteLine();

                foreach ( string commandType in commandDataStructures.Keys.OrderBy( x => x ) )
                {
                    var commandTypeNormalized = commandType.Replace( "_", "" );
                    var structures = commandDataStructures[commandType];
                    if ( structures == null )
                    {
                        writer.WriteLine( $"// {commandTypeNormalized}\n" );
                        continue;
                    }

                    var finalStructure = new Structure( commandType );

                    foreach ( var field in structures[0].Fields )
                    {
                        var nonZeroFields = structures.SelectMany( x => x.Fields ).Where( x => x.Offset == field.Offset && x.Value != ( object )0 );
                        var type = typeof( int );
                        if ( nonZeroFields.Any( x => x.Type == typeof( float ) ) )
                        {
                            type = typeof( float );
                        }

                        finalStructure.Fields.Add( new Field( field.Offset, type, null ) );
                    }

                    writer.WriteLine( "typedef struct" );
                    writer.WriteLine( "{" );

                    foreach ( var field in finalStructure.Fields )
                    {
                        var fieldType = "u32";
                        if ( field.Type == typeof( float ) )
                            fieldType = "f32";

                        writer.WriteLine( $"    {fieldType} Field{field.Offset:X2};" );
                    }

                    writer.WriteLine( "} CommandData_" + commandTypeNormalized + ";" );
                    writer.WriteLine();

                    using ( var csWriter = File.CreateText( $"{commandTypeNormalized}CommandData.cs" ) )
                    {
                        csWriter.WriteLine( "using EvtTool.IO;" );
                        csWriter.WriteLine();
                        csWriter.WriteLine( "namespace EvtTool" );
                        csWriter.WriteLine( "{" );
                        csWriter.WriteLine( $"    public sealed class {commandTypeNormalized}CommandData : CommandData" );
                        csWriter.WriteLine( "    {" );
                        foreach ( var field in finalStructure.Fields )
                        {
                            var fieldType = "int";
                            if ( field.Type == typeof( float ) )
                                fieldType = "float";

                            csWriter.WriteLine( $"        public {fieldType} Field{field.Offset:X2} {{ get; set; }}" );
                        }
                        csWriter.WriteLine();
                        csWriter.WriteLine( "        internal override void Read( Command command, EndianBinaryReader reader )" );
                        csWriter.WriteLine( "        {" );
                        foreach ( var field in finalStructure.Fields )
                        {
                            var method = "ReadInt32";
                            if ( field.Type == typeof( float ) )
                                method = "ReadSingle";

                            csWriter.WriteLine( $"            Field{field.Offset:X2} = reader.{method}();" );
                        }
                        csWriter.WriteLine( "        }" );
                        csWriter.WriteLine();
                        csWriter.WriteLine( "        internal override void Write( Command command, EndianBinaryWriter writer )" );
                        csWriter.WriteLine( "        {" );
                        foreach ( var field in finalStructure.Fields )
                        {
                            csWriter.WriteLine( $"            writer.Write( Field{field.Offset:X2} );" );
                        }
                        csWriter.WriteLine( "        }" );
                        csWriter.WriteLine( "    }" );
                        csWriter.WriteLine( "}" );
                    }
                }
            }

            Environment.Exit( 0 );
        }

        private static bool IsLikelyFloat( int value )
        {
            if ( value == -1 )
                return false;

            var floatValue = Unsafe.ReinterpretCast<int, float>( value );
            if ( floatValue < 0 )
                floatValue = -floatValue;

            return floatValue > 0.001 && floatValue <= 1000000f;
        }

        private static bool IsLikely2Shorts( int value )
        {
            return value >= 0x00010000;
        }

        private class Structure
        {
            public string Type { get; }

            public List<Field> Fields { get; } = new List<Field>();

            public Structure( string type )
            {
                Type = type;
            }
        }

        private class Field
        {
            public int Offset { get; }

            public Type Type { get; }

            public object Value { get; }

            public Field( int offset, Type type, object value )
            {
                Offset = offset;
                Type = type;
                Value = value;
            }
        }
    }
}