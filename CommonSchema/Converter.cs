﻿using CSJsonDB;
using GraphQLParser;
using GraphQLParser.AST;
using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace AutoRequestStore.CommonSchema
{
    public class Constants 
    {
        public const string NODE_NAME = "name";
        public const string CHILDREN_NAME = "children";
    }

    internal class Converter
    {
        public static CommonNode ParseNodesFromQuery(string query)
        {
            var parsed = Parser.Parse<GraphQLDocument>(query, new ParserOptions { Ignore = IgnoreOptions.Comments });
            var jsonDocument = JsonDocument.Parse(parsed.ToJsonString());
            var definitions = jsonDocument.RootElement.GetProperty("Definitions").EnumerateArray();

            return JsonSerializer.Deserialize<CommonNode>(definitions.First().GetRawText());
        }

        public static JsonObject BuildSchema(CommonNode node)
        {
            List<CommonNode> struc = new List<CommonNode>();
            JsonObject inner = new JsonObject();
            var res = GetInnerSelection(node, inner);
            var x = new ExpandoObject() as IDictionary<string, Object>;

            foreach (var item in struc)
            {
                x.Add(item.Name.StringValue, "Val");
            }

            var expandoResult = JsonSerializer.Serialize(x);
            Console.WriteLine(expandoResult);

            return res;
        }

        public static List<ExpandoObject> GetResultList(JsonElement element, JsonArray structure)
        {
            List<ExpandoObject> arr = new List<ExpandoObject>();
            JsonNode n;
            var obj = structure[0].AsObject();
            var hasChildren = obj.TryGetPropertyValue(Constants.CHILDREN_NAME, out n);

            if (hasChildren)
            {
                var name = obj[Constants.NODE_NAME].GetValue<string>();
                var arr2 = obj[Constants.CHILDREN_NAME].AsArray();

                if (element.ValueKind == JsonValueKind.Object)
                {
                    //TODO: change to element.TryGetProperty for check query correspond with schema
                    var r = element.GetProperty(name);
                    arr = GetResultList(r, obj[Constants.CHILDREN_NAME].AsArray());
                }
                else
                {
                    var r = element.EnumerateArray();
                    foreach (var item in r)
                    {
                        var entity = item.GetProperty(name);
                        var mapped = MapToEntity(arr2, entity);
                        arr.Add(mapped);
                    }
                }
            }
            else
            {
                var mapped = MapToEntity(structure, element);
                arr.Add(mapped);
            }

            return arr;
        }

        static ExpandoObject MapToEntity(JsonArray schemaArray, JsonElement entity)
        {
            var obj = new ExpandoObject() as IDictionary<string, object>;
            int nodeIndex = 0;

            foreach (var item in schemaArray)
            {
                var prop = item[Constants.NODE_NAME].ToString();
                var val = entity.GetProperty(prop);

                if (val.ValueKind == JsonValueKind.String)
                    obj.Add(prop, val.GetString());
                else if (val.ValueKind == JsonValueKind.Number)
                    obj.Add(prop, val.GetDecimal());
                else if (val.ValueKind == JsonValueKind.Object)
                {
                    var objNode = schemaArray[nodeIndex].AsObject();
                    var arr = GetResultList(val, objNode[Constants.CHILDREN_NAME].AsArray());

                    obj.Add(prop, arr);
                }

                nodeIndex++;
            }

            return (ExpandoObject)obj;
        }


        static JsonObject GetInnerSelection(CommonNode node, JsonObject innerStructure)
        {
            var n = new JsonObject();
            n.Add(Constants.NODE_NAME, node.Name.StringValue);

            if (node.Kind == 2)
            {
                n.Add("collection", node.Name.StringValue);
            }

            if (node.SelectionSet is not null && node.SelectionSet.Selections.Count > 0)
            {
                var children = new JsonArray();
                JsonObject inner = new JsonObject();
                foreach (var item in node.SelectionSet.Selections)
                {
                    var child = GetInnerSelection(item, inner);
                    children.Add(child);
                }

                n.Add(Constants.CHILDREN_NAME, children);
            }
            else
            {
                innerStructure.Add(node.Name.StringValue, "val");
            }

            return n;
        }
    }
}
