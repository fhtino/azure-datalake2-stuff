using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;



namespace FakeDataProducer
{

    public class ModelJson
    {
        public string name { get; set; }
        public string version { get; set; }
        public DateTimeOffset modifiedTime { get; set; }
        public List<Entity> entities { get; set; }
        public List<Relationship> relationships { get; set; }


        // -- sub-classes

        public class Entity
        {
            public string name { get; set; }
            [JsonPropertyName("$type")]
            public string Type { get; set; }
            public string description { get; set; }
            public List<Attribute> attributes { get; set; }
            public List<Partition> partitions { get; set; }
        }


        public class Attribute
        {
            public string name { get; set; }
            public string dataType { get; set; }
        }


        public class Partition
        {
            public string name { get; set; }
            public DateTimeOffset refreshTime { get; set; }
            public string location { get; set; }
        }


        public class Relationship
        {
            [JsonPropertyName("$type")]
            public string Type { get; set; }
            public RelationshipAttribute fromAttribute { get; set; }
            public RelationshipAttribute toAttribute { get; set; }
        }


        public class RelationshipAttribute
        {
            public string entityName { get; set; }
            public string attributeName { get; set; }
        }

    }
}
