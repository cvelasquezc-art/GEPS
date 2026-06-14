using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace GEPS.Models
{
    [BsonIgnoreExtraElements]
    public class Semillero
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("nombre")]
        public string Nombre { get; set; }

        [BsonElement("lineaInvestigativa")]
        public string LineaInvestigativa { get; set; }

        [BsonElement("descripcion")]
        public string Descripcion { get; set; }

        [BsonElement("fechaCreacion")]
        public DateTime FechaCreacion { get; set; }

        [BsonElement("activo")]
        public bool Activo { get; set; }
    }
}