using AutoRequestStore.CommonSchema;
using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;
using CSJsonDB;
using GraphQL;
using GraphQL.Client.Abstractions;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Dynamic;
using System.Text.Json;

namespace AutoRequestStore.Commands
{
    [Command]
    public class RequestCommand : ICommand
    {
        public RequestCommand(IOptions<ConnectionSettings> connection)
        {
            Console.Out.WriteLine(connection.Value.Endpoint);
        }

        //[CommandParameter(0, Description = "Value whose logarithm is to be found.")]
        //public double Millisecons { get; init; }

        // Name: --interval
        // Short name: -i
        [CommandOption("interval", 'i', Description = "Interval in milliseconds.")]
        public double Interval { get; init; } = 1000;

        private string baseQuery2 = @"
            query allPeople {
                allPeople(first: 10) {
                    edges {
                        node {
                            name
                            gender
                            }
                    }
                }
            }";
        private readonly IGraphQLClient _client;

        public RequestCommand(IGraphQLClient client)
        {
            _client = client;
        }

        public async ValueTask ExecuteAsync(IConsole console)
        {
            List<ExpandoObject> resultList = new List<ExpandoObject>();

            var rootNode = Converter.ParseNodesFromQuery(baseQuery2);
            var schema = Converter.BuildSchema(rootNode);


            var queryRequest = new GraphQLRequest(baseQuery2);
            var graphQLResponse = await _client.SendQueryAsync<JsonElement>(queryRequest);
            resultList = Converter.GetResultList(graphQLResponse.Data, schema["children"].AsArray());

            // SAVE TO JSON FILE
            console.Output.WriteLine("START SAVING...");
            console.Output.WriteLine(resultList.ToJsonString());

            var db = JsonDB.Load("data.db");

            //var i = 0;
            Stopwatch sw = new Stopwatch();
            sw.Start();

            foreach (var item in resultList)
            {
                db.Add(rootNode.Name.StringValue, item);
                db = JsonDB.Load("data.db");
            }

            sw.Stop();
            console.Output.WriteLine(sw.ElapsedMilliseconds);
        }
    }
}
