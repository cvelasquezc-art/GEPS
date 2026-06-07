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
                string Conexion = ConfigurationManager.AppSettings["ConexionMongo"];
                string NombreBD = ConfigurationManager.AppSettings["NombreBasedeDatosMongo"];
                MongoClient cliente = new MongoClient(Conexion);
                BasedeDatos = cliente.GetDatabase(NombreBD);
            }
            return BasedeDatos;
        }
    }
}