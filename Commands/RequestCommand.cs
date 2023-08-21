using AutoRequestStore.CommonSchema;
using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;
using CSJsonDB;
using GraphQL;
using GraphQL.Client.Abstractions;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.SystemTextJson;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace AutoRequestStore.Commands
{
    [Command]
    public class RequestCommand : ICommand
    {
        public RequestCommand(IOptions<ConnectionSettings> connection)
        {
            _client = new GraphQLHttpClient(connection.Value.Endpoint, new SystemTextJsonSerializer());
            _baseQuery = connection.Value.Query;
        }

        //[CommandParameter(0, Description = "Value whose logarithm is to be found.")]
        //public double Millisecons { get; init; }

        // Name: --interval
        // Short name: -i
        [CommandOption("interval", 'i', Description = "Interval in milliseconds.")]
        public double Interval { get; init; } = 1000;

        // Name: --name
        // Short name: -n
        [CommandOption("name", 'n', Description = "Output file name.")]
        public string Name { get; init; }

        private string _baseQuery;
        private readonly IGraphQLClient _client;

        public RequestCommand(IGraphQLClient client)
        {
            _client = client;
        }

        public async ValueTask ExecuteAsync(IConsole console)
        {
            List<ExpandoObject> resultList = new List<ExpandoObject>();

            var rootNode = Converter.ParseNodesFromQuery(_baseQuery);
            var schema = Converter.BuildSchema(rootNode);

            var queryRequest = new GraphQLRequest(_baseQuery);
            var graphQLResponse = await _client.SendQueryAsync<JsonElement>(queryRequest);
            resultList = Converter.GetResultList(graphQLResponse.Data, schema["children"].AsArray());

            // SAVE TO JSON FILE
            console.Output.WriteLine("START SAVING...");
            console.Output.WriteLine(resultList.ToJsonString());

            var dbFile = CreateDataFile(rootNode);
            var db = JsonDB.Load(dbFile);

            Stopwatch sw = new Stopwatch();
            sw.Start();

            foreach (var item in resultList)
            {
                db.Add(rootNode.Name.StringValue, item);
                db = JsonDB.Load(dbFile);
            }

            sw.Stop();
            console.Output.WriteLine(sw.ElapsedMilliseconds);
        }

        private string CreateDataFile(CommonNode node)
        {
            var collection = new JsonObject();
            var stamp = DateTime.Now.ToString("yMMddHHmmss");
            var fileName = Name ?? $"data_{stamp}.db";

            collection.Add(node.Name.StringValue, new JsonArray());

            using (var writer = File.CreateText(fileName)) 
            {
                writer.WriteLine(collection.ToJsonString());
            }

            return fileName;
        }
    }
}
