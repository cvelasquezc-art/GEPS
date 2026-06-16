using MongoDB.Driver;
using System.Configuration;

namespace GEPS.Models
{
    public class ConexionMongo
    {
        private static IMongoDatabase _database;

        public static IMongoDatabase ObtenerDB()
        {
            if (_database == null)
            {
                string connectionString = ConfigurationManager
                    .AppSettings["MongoConnectionString"];
                string databaseName = ConfigurationManager
                    .AppSettings["MongoDatabaseName"];
                var client = new MongoClient(connectionString);
                _database = client.GetDatabase(databaseName);
            }
            return _database;
        }
    }
}