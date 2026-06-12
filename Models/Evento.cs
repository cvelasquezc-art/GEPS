using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace GEPS.Models
{
    public class Evento
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("nombre")]
        [Required(ErrorMessage ="El nombre es obligatgorio")]
        public string Nombre { get; set; }

        [BsonElement("fecha")]
        [Required(ErrorMessage = "La fecha es obligatoria.")]
        public DateTime Fecha { get; set; }

        [BsonElement("lugar")]
        [Required(ErrorMessage = "El lugar es obligatorio.")]
        public string Lugar { get; set; }

        [BsonElement("tipo")]
        [Required(ErrorMessage = "El tipo es obligatorio.")]
        public string Tipo { get; set; }

        [BsonElement("nombreOrganizador")]
        [Required(ErrorMessage = "El nombre del organizador es obligatorio.")]
        public string NombreOrganizador { get; set; }

        [BsonElement("activo")]
        public bool Activo { get; set; }

        [BsonElement("proyectos")]
        [BsonRepresentation(BsonType.ObjectId)]
        public List<string> Proyectos { get; set; } = new List<string>();
    }
}