using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.ComponentModel.DataAnnotations;

namespace GEPS.Models
{
    public class Semillero
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [Required(ErrorMessage = "El campo Nombre es obligatorio.")]
        [BsonElement("nombre")]
        public string Nombre { get; set; }

        [Required(ErrorMessage = "El campo Fecha de Creación es obligatorio.")]
        [BsonElement("fechaCreacion")]
        public DateTime FechaCreacion { get; set; }

        [Required(ErrorMessage = "El campo Línea Investigativa es obligatorio.")]
        [BsonElement("lineaInvestigativa")]
        public string LineaInvestigativa { get; set; }

        [BsonElement("activo")]
        public bool Activo { get; set; }

        [Required(ErrorMessage = "El campo Descripción es obligatorio.")]
        [BsonElement("descripcion")]
        public string Descripcion { get; set; }
    }
}