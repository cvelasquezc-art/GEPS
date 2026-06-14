using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace GEPS.Models
{
    public class Usuario
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("cedula")]
        public string Cedula { get; set; }

        [BsonElement("nombre")]
        public string Nombre { get; set; }

        [BsonElement("correo")]
        public string Correo { get; set; }

        [BsonElement("clave")]
        public string Clave { get; set; }

        [BsonElement("rol")]
        public string Rol { get; set; }

        [BsonElement("fechaNacimiento")]
        public DateTime? FechaNacimiento { get; set; }

        [BsonElement("genero")]
        public string Genero { get; set; }

        [BsonElement("celular")]
        public string Celular { get; set; }

        [BsonElement("programa")]
        public string Programa { get; set; }

        [BsonElement("idSemillero")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string IdSemillero { get; set; }

        [BsonElement("activo")]
        public bool Activo { get; set; }

        [BsonElement("codigoRecuperacion")]
        public string CodigoRecuperacion { get; set; }

        [BsonElement("codigoExpira")]
        public DateTime? CodigoExpira { get; set; }
    }
}
