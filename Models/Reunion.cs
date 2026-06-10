using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace GEPS.Models
{
    public class Reunion
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("fecha")]
        public DateTime Fecha { get; set; }

        [BsonElement("hora")]
        public string Hora { get; set; }

        [BsonElement("lugar")]
        public string Lugar { get; set; }

        [BsonElement("enlace")]
        public string Enlace { get; set; }

        [BsonElement("motivo")]
        public string Motivo { get; set; }

        [BsonElement("activo")]
        public bool Activo { get; set; }

        [BsonElement("idProyecto")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string IdProyecto { get; set; }
    }
}