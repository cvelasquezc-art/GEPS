using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace GEPS.Models
{
    public class Proyecto
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("titulo")]
        [Required(ErrorMessage = "El título es obligatorio.")]
        public string Titulo { get; set; }

        [BsonElement("duracionMeses")]
        [Required(ErrorMessage = "La duración en meses es obligatoria.")]
        public int DuracionMeses { get; set; }

        [BsonElement("objetivo")]
        [Required(ErrorMessage = "El objetivo es obligatorio.")]
        public string Objetivo { get; set; }

        [BsonElement("fechaInicio")]
        [Required(ErrorMessage = "La fecha de inicio es obligatoria.")]
        public DateTime FechaInicio { get; set; }

        [BsonElement("estado")]
        public string Estado { get; set; }

        [BsonElement("idSemillero")]
        [BsonRepresentation(BsonType.ObjectId)]
        public string IdSemillero { get; set; }

        [BsonElement("fases")]
        public List<Fase> Fases { get; set; } = new List<Fase>();

        public class Fase
        {
            [BsonElement("nombre")]
            [Required(ErrorMessage = "El nombre de la fase es obligatorio.")]
            public string Nombre { get; set; }

            [BsonElement("duracionMeses")]
            [Required(ErrorMessage = "La duración en meses es obligatoria.")]
            public int DuracionMeses { get; set; }

            [BsonElement("actividades")]
            public List<Actividad> Actividades { get; set; } = new List<Actividad>();
        }

        public class Actividad
        {
            [BsonElement("nombre")]
            [Required(ErrorMessage = "El nombre de la actividad es obligatorio.")]
            public string Nombre { get; set; }

            [BsonElement("duracionDias")]
            [Required(ErrorMessage = "La duración en días es obligatoria.")]
            public int DuracionDias { get; set; }

            [BsonElement("fechaEntrega")]
            [Required(ErrorMessage = "La fecha de entrega es obligatoria.")]
            public DateTime FechaEntrega { get; set; }
        }
    }
}