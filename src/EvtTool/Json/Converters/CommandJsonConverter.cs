using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EvtTool.Json.Converters
{
    public sealed class CommandJsonConverter : JsonConverter<Command>
    {
        public override bool CanWrite => false;

        public override Command ReadJson( JsonReader reader, Type objectType, Command existingValue, bool hasExistingValue, JsonSerializer serializer )
        {
            // Deserialize command
            var jsonObject = JObject.Load( reader );
            var command = new Command();
            serializer.Populate( jsonObject.CreateReader(), command );

            if ( command.CommandCode.Length > 0 )
            {
                // Deserialize command data
                var data = CommandDataFactory.Create( command.CommandCode );
                serializer.Populate( jsonObject[ "Data" ].CreateReader(), data );
                command.Data = data;
            }
            else
            {
                // If type is empty, then it's probably dummied out.
                command.Data = new UnknownCommandData() { Bytes = new byte[0] };
                command.DataSize = 0;
            }

            return command;
        }

        public override void WriteJson( JsonWriter writer, Command value, JsonSerializer serializer ) => throw new NotImplementedException();
    }
}