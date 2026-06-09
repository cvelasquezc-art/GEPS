using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GEPS.Models
{
    public class Proyecto
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("titulo")]
        public string Titulo { get; set; }

        [BsonElement("duracionMeses")]
        public int DuracionMeses { get; set; }

        [BsonElement("objetivo")]
        public string Objetivo { get; set; }

        [BsonElement("fechaInicio")]
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
            public string Nombre { get; set; }

            [BsonElement("duracionMeses")]
            public int DuracionMeses { get; set; }

            [BsonElement("actividades")]
            public List<Actividad> Actividades { get; set; } = new List<Actividad>();
        }

        public class Actividad
        {
            [BsonElement("nombre")]
            public string Nombre { get; set; }

            [BsonElement("duracionDias")]
            public int DuracionDias { get; set; }

            [BsonElement("fechaEntrega")]
            public DateTime FechaEntrega { get; set; }
        }
    }
}