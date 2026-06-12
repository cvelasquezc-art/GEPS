using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.ComponentModel.DataAnnotations;

namespace GEPS.Models
{
    [BsonIgnoreExtraElements]
    public class Semillero
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("nombre")]
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        public string Nombre { get; set; }

        [BsonElement("lineaInvestigativa")]
        [Required(ErrorMessage = "La línea de investigación es obligatoria.")]
        public string LineaInvestigativa { get; set; }

        [BsonElement("descripcion")]
        [Required(ErrorMessage = "La descripción es obligatoria.")]
        public string Descripcion { get; set; }

        [BsonElement("fechaCreacion")]
        public DateTime FechaCreacion { get; set; }

        [BsonElement("activo")]
        public bool Activo { get; set; }
    }
}