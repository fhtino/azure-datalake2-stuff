using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;


namespace FakeDataProducer
{

    public static class ModelJsonBuilder
    {

        private static Dictionary<string, string> _typeConversionDict = new Dictionary<string, string>()
        {
            { "System.String", "string" },
            { "System.Int16", "int64"},
            { "System.Int32", "int64"},
            { "System.Int64", "int64"},
            { "System.UInt16", "int64"},
            { "System.UInt32", "int64"},
            { "System.UInt64", "int64"},
            { "System.Single", "double"},
            { "System.Double", "double"},
            { "System.Decimal", "decimal"},
            { "System.Boolean", "boolean" },
            { "System.DateTime", "dateTime"},
            { "System.DateTimeOffset", "dateTimeOffset" }
        };



        public static string Run(List<Type> listOfTypes,
                                 List<Tuple<Type, string, Type, string>> relationships,
                                 List<Tuple<Type, string>> _csvFileList)
        {

            var modelJson = new ModelJson();
            modelJson.name = "mydata";
            modelJson.version = "1.0";
            modelJson.modifiedTime = DateTime.UtcNow;
            modelJson.entities = new List<ModelJson.Entity>();
            modelJson.relationships = new List<ModelJson.Relationship>();

            // entities
            foreach (var type in listOfTypes)
            {
                var e = ModelJsonEntityFromClass(type);
                var files = _csvFileList.Where(x => x.Item1 == type).ToList();
                e.partitions = Enumerable.Range(0, files.Count)
                                         .Select(i => new ModelJson.Partition
                                         {
                                             name = "P_" + i,
                                             location = files[i].Item2,
                                             refreshTime = DateTime.UtcNow
                                         })
                                         .ToList();
                modelJson.entities.Add(e);
            }

            // relationships
            if (relationships != null)
            {
                foreach (var relation in relationships)
                {
                    var r = new ModelJson.Relationship()
                    {
                        Type = "SingleKeyRelationship",
                        fromAttribute = new ModelJson.RelationshipAttribute()
                        {
                            entityName = relation.Item1.Name,
                            attributeName = relation.Item2
                        },
                        toAttribute = new ModelJson.RelationshipAttribute()
                        {
                            entityName = relation.Item3.Name,
                            attributeName = relation.Item4
                        }
                    };

                    modelJson.relationships.Add(r);
                }
            }

            // serialize
            var jsonBody = JsonSerializer.Serialize<ModelJson>(
                                modelJson,
                                new JsonSerializerOptions
                                {
                                    WriteIndented = true
                                });
            
            return jsonBody;
        }



        private static ModelJson.Entity ModelJsonEntityFromClass(Type t)
        {
            return new ModelJson.Entity()
            {
                Type = "LocalEntity",
                name = t.Name,
                description = t.Name,
                attributes = t.GetProperties()
                              .Select(p =>
                                    new ModelJson.Attribute()
                                    {
                                        name = p.Name,
                                        dataType = _typeConversionDict[p.PropertyType.FullName]
                                    })
                              .ToList(),
                partitions = new List<ModelJson.Partition>()
            };
        }

    }
}
