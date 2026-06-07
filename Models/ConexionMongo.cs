using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MongoDB.Driver;
using System.Configuration;

namespace GEPS.Models
{
    public class ConexionMongo
    {
        private static IMongoDatabase BasedeDatos;

        public static IMongoDatabase ObtenerBD()
        {
            if (BasedeDatos == null)
            {
                string Conexion = ConfigurationManager.AppSettings["MongoConnectionString"];
                string NombreBD = ConfigurationManager.AppSettings["MongoDatabaseName"];
                var cliente = new MongoClient(Conexion);
                BasedeDatos = cliente.GetDatabase(NombreBD);
            }
            return BasedeDatos;
        }
    }
}