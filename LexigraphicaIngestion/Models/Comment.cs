using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LexigraphicaIngestion.Models
{
    public class Comment : LexigraphiModel<Comment>
    {
        public int user_id;
        public int photo_id;
        public string comment;

        protected static Comment Ingest(string[] data)
        {
            return new Comment()
            {
                id = Int32.Parse(data[0]),
                user_id = Int32.Parse(data[1]),
                photo_id = Int32.Parse(data[2]),
                created_at = data[3],
                updated_at = data[4],
                lock_version = Int32.Parse(data[5]),
                comment = data[6]
            };
        }

        public static IEnumerable<Comment> Ingest(string file)
        {
            return Ingest(file, 7, Ingest);
        }
    }
}
