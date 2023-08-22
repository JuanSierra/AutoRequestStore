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
using Newtonsoft.Json.Linq;
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
            _baseQuery = File.ReadAllText(connection.Value.Query);
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
        public string FileName { get; init; }

        // Name: --start
        // Short name: -s
        [CommandOption("start", 's', Description = "Start value.")]
        public string Start { get; init; }

        // Name: --end
        // Short name: -e
        [CommandOption("end", 'e', Description = "End value.")]
        public string End { get; init; }


        private string _baseQuery;
        private readonly IGraphQLClient _client;
        private int _currentSequence = int.MinValue;

        public RequestCommand(IGraphQLClient client)
        {
            _client = client;
        }

        public async ValueTask ExecuteAsync(IConsole console)
        {
            List<ExpandoObject> resultList = new List<ExpandoObject>();
            var firstRun = true;
            string dbFile = null;
            JObject db = null;

            while (HasRange() || firstRun) 
            {
                var computedQuery = HasRange() ? ReplaceSequence(_baseQuery): _baseQuery;
                var rootNode = Converter.ParseNodesFromQuery(computedQuery);
                var schema = Converter.BuildSchema(rootNode);

                var queryRequest = new GraphQLRequest(computedQuery);
                var graphQLResponse = await _client.SendQueryAsync<JsonElement>(queryRequest);
                resultList = Converter.GetResultList(graphQLResponse.Data, schema["children"].AsArray());

                // SAVE TO JSON FILE
                console.Output.WriteLine("START SAVING...");
                console.Output.WriteLine(resultList.ToJsonString());

                if (firstRun)
                {
                    if (!string.IsNullOrEmpty(FileName) && File.Exists($"{FileName}.db"))
                        dbFile = $"{FileName}.db";
                    else
                        dbFile = CreateDataFile(rootNode);

                    db = JsonDB.Load(dbFile);
                }

                Stopwatch sw = new Stopwatch();
                sw.Start();

                foreach (var item in resultList)
                {
                    db.Add(rootNode.Name.StringValue, item);
                    db = JsonDB.Load(dbFile);
                }

                sw.Stop();
                console.Output.WriteLine(sw.ElapsedMilliseconds);
                firstRun = false;
            }
        }

        private string ReplaceSequence(string query) 
        {
            if (_currentSequence == int.MinValue) 
            {
                _currentSequence = Convert.ToInt32(Start);
            }
            var new_query = query.Replace("$var", _currentSequence++.ToString());
            Console.WriteLine(new_query);

            return new_query;
        }

        private bool HasRange() 
        {
            int start, end;

            if (string.IsNullOrEmpty(Start) || string.IsNullOrEmpty(End))
                return false;

            if (!int.TryParse(Start, out start))
                return false;
            if (!int.TryParse(End, out end))
                return false;
            if (_currentSequence > Convert.ToInt32(End))
                return false;

            return true;
        }

        private string CreateDataFile(CommonNode node)
        {
            var collection = new JsonObject();
            var stamp = DateTime.Now.ToString("yMMddHHmmss");
            var fileName = FileName ?? $"data_{stamp}";

            collection.Add(node.Name.StringValue, new JsonArray());

            using (var writer = File.CreateText($"{fileName}.db")) 
            {
                writer.WriteLine(collection.ToJsonString());
            }

            return $"{fileName}.db";
        }
    }
}
