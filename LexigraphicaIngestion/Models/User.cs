using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LexigraphicaIngestion.Models
{
    public class User : LexigraphiModel<User>
    {
        public string username;
        public string email;
        public string role;
        public int active;
        public string url;
        public string description;

        protected static User Ingest(string[] data)
        {
            return new User()
            {
                id = Int32.Parse(data[0]),
                username = data[1],
                email = data[2],
                role = data[3],
                created_at = data[4],
                updated_at = data[5],
                lock_version = Int32.Parse(data[6]),
                active = Int32.Parse(data[7]),
                url = data[8],
                description = data[9]
            };
        }

        public static IEnumerable<User> Ingest(string file)
        {
            return Ingest(file, 10, Ingest);
        }
    }
}
