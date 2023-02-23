using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;

namespace RfidReader.Database
{
    class MySqlDatabase
    {
        public MySqlConnection Con;
        public MySqlDatabase()
        {
            var builder = new ConfigurationBuilder()
                        .SetBasePath(Directory.GetCurrentDirectory())
                        .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            IConfiguration _configuration = builder.Build();
            string? MyConnectionString = _configuration.GetConnectionString("MySqlDB");

            Con = new MySqlConnection(MyConnectionString);
            this.Con.Open();
        }
    }
}
