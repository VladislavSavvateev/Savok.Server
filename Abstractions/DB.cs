using MySql.Data.MySqlClient;

namespace Savok.Server.Abstractions {
    public abstract class DB {
        public abstract string ConnectionString { get; }

        public MySqlConnection Connection(string connectionString = null) {
            var con = new MySqlConnection(connectionString ?? ConnectionString);
            con.Open();
            return con;
        }

        public MySqlCommand Command(string command, params object[] args) {
            var com = new MySqlCommand(args.Length == 0 ? command : string.Format(command, args), Connection());
            return com;
        }
    }
}