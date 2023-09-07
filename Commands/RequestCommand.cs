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
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace AutoRequestStore.Commands
{
    [Command]
    public class RequestCommand : ICommand
    {
        public RequestCommand(IOptions<ConnectionSettings> connection)
        {
            GraphQLHttpClientOptions options = new GraphQLHttpClientOptions();
            options.MediaType = "application/json";
            options.EndPoint = new Uri(connection.Value.Endpoint);
            
            _client = new GraphQLHttpClient(options, new SystemTextJsonSerializer());
            if(!string.IsNullOrEmpty(connection.Value.Authz))
                _client.HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {connection.Value.Authz}");
            
            //_client.SendMutationAsync()
            //_client..Add("Authorization", $"bearer {ApiKey}");

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
        private readonly GraphQLHttpClient _client;
        private int _currentSequence = int.MinValue;
        private DateTime _currentDate = DateTime.MinValue;
        private bool _isNumericSequence;


        public async ValueTask ExecuteAsync(IConsole console)
        {
            List<ExpandoObject> resultList = new List<ExpandoObject>();
            string dbFile = null;
            JObject db = null;

            var computedQuery = HasRange() ? ReplaceSequence(_baseQuery) : _baseQuery;
            var rootNode = Converter.ParseNodesFromQuery(computedQuery);
            var schema = Converter.BuildSchema(rootNode);

            if (!string.IsNullOrEmpty(FileName) && File.Exists($"{FileName}.db"))
                dbFile = $"{FileName}.db";
            else
                dbFile = CreateDataFile(rootNode);

            console.ReadKey();

            console.Output.WriteLine(schema.ToJsonString());

            do
            {
                computedQuery = HasRange() ? ReplaceSequence(_baseQuery) : _baseQuery;
                //var rootNode = Converter.ParseNodesFromQuery(computedQuery);
                //var schema = Converter.BuildSchema(rootNode);

                var queryRequest = new GraphQLRequest(computedQuery);
                //_client
                //_client.SendMutationAsync()
                var graphQLResponse = await _client.SendQueryAsync<JsonElement>(queryRequest);
                resultList = Converter.GetResultList(graphQLResponse.Data, schema["children"].AsArray());

                // SAVE TO JSON FILE
                console.Output.WriteLine("START SAVING...");
                console.Output.WriteLine(resultList.ToJsonString());

                db = JsonDB.Load(dbFile);

                Stopwatch sw = new Stopwatch();
                sw.Start();

                foreach (var item in resultList)
                {
                    db.Add(rootNode.Name.StringValue, item);
                    db = JsonDB.Load(dbFile);
                }

                sw.Stop();
                console.Output.WriteLine(sw.ElapsedMilliseconds);
            } while (HasRange());
        }

        private string ReplaceSequence(string query) 
        {
            string new_query;

            if (_isNumericSequence) 
            {
                if (_currentSequence == int.MinValue)
                {
                    _currentSequence = Convert.ToInt32(Start);
                }

                new_query = query.Replace("$var", _currentSequence++.ToString());
            }
            else
            {
                if (_currentDate == DateTime.MinValue)
                {
                    _currentDate = Convert.ToDateTime(Start);
                }

                new_query = query.Replace("$var", _currentDate.ToString("yyyy-MM-dd'T'HH:mm:ss.ffK", CultureInfo.InvariantCulture));
                _currentDate.AddDays(1);
            }

            return new_query;
        }

        private bool HasRange() 
        {
            int start, end;
            DateTime startDate, endDate;
            bool hasRange = true;

            if (string.IsNullOrEmpty(Start) || string.IsNullOrEmpty(End))
                return false;

            if (!int.TryParse(Start, out start))
                hasRange = false;

            if (!int.TryParse(End, out end))
                hasRange = false;

            if (hasRange) // is numeric
            {
                _isNumericSequence = true;

                if (end < start)
                    throw new ArgumentException("Start value should be less than end value");

                if (_currentSequence > Convert.ToInt32(End))
                    return false;
            }
            else 
            {
                if (!DateTime.TryParse(Start, out startDate))
                    hasRange = false;

                if (!DateTime.TryParse(End, out endDate))
                    hasRange = false;

                if (endDate < startDate)
                    throw new ArgumentException("Start value should be less than end value");

                if (_currentDate > endDate)
                    return false;
            }

            return hasRange;
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
