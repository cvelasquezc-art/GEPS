using System;
using System.ComponentModel.DataAnnotations;
using System.Web;
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
        [Required(ErrorMessage = "La cédula es obligatoria.")]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "La cédula debe tener exactamente 10 dígitos numéricos.")]
        public string Cedula { get; set; }

        [BsonElement("nombre")]
        public string Nombre { get; set; }

        [BsonElement("correo")]
        [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
        public string Correo { get; set; }

        [BsonElement("clave")]
        [Required(ErrorMessage = "La contraseña es obligatoria.")]
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
