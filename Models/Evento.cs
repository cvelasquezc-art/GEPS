using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GEPS.Models
{
    public class Evento
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("nombre")]
        public string Nombre { get; set; }

        [BsonElement("fecha")]
        public DateTime Fecha { get; set; }

        [BsonElement("lugar")]
        public string Lugar { get; set; }

        [BsonElement("tipo")]
        public string Tipo { get; set; }

        [BsonElement("nombreOrganizador")]
        public string NombreOrganizador { get; set; }

        [BsonElement("activo")]
        public bool Activo { get; set; }

        [BsonElement("proyectos")]
        [BsonRepresentation(BsonType.ObjectId)]
        public List<string> Proyectos { get; set; } = new List<string>();
    }
}