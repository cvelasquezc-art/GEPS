using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.ComponentModel.DataAnnotations;

namespace GEPS.Models
{
    public class Reunion
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("fecha")]
        [Required(ErrorMessage = "La fecha es obligatoria.")]
        public DateTime Fecha { get; set; }

        [BsonElement("hora")]
        [Required(ErrorMessage = "La hora es obligatoria.")]
        public string Hora { get; set; }

        [BsonElement("lugar")]
        [Required(ErrorMessage = "El lugar es obligatorio.")]
        public string Lugar { get; set; }

        [BsonElement("enlace")]
        [Required(ErrorMessage = "El enlace es obligatorio.")]
        public string Enlace { get; set; }

        [BsonElement("motivo")]
        [Required(ErrorMessage = "El motivo es obligatorio.")]
        public string Motivo { get; set; }

        [BsonElement("activo")]
        public bool Activo { get; set; }

        [BsonElement("idProyecto")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string IdProyecto { get; set; }
    }
}