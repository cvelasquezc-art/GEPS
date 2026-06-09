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

        [Required(ErrorMessage = "El campo Fecha es obligatorio.")]
        [BsonElement("fecha")]
        public DateTime Fecha { get; set; }

        [Required(ErrorMessage = "El campo Hora es obligatorio.")]
        [BsonElement("hora")]
        public string Hora { get; set; }

        [Required(ErrorMessage = "El campo Lugar es obligatorio.")]
        [BsonElement("lugar")]
        public string Lugar { get; set; }

        [Required(ErrorMessage = "El campo Enlace es obligatorio.")]
        [BsonElement("enlace")]
        public string Enlace { get; set; }

        [Required(ErrorMessage = "El campo Motivo es obligatorio.")]
        [BsonElement("motivo")]
        public string Motivo { get; set; }

        [BsonElement("idProyecto")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string IdProyecto { get; set; }
    }
}