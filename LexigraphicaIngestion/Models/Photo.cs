using Catfish.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LexigraphicaIngestion.Models
{
    public class Photo : LexigraphiModel<Photo>
    {
        public int user_id;
        public string description;
        public string filename;
        public string format;
        public string text_type;

        protected static Photo Ingest(string[] data)
        {
            return new Photo()
            {
                id = Int32.Parse(data[0]),
                user_id = Int32.Parse(data[1]),
                description = data[2],
                filename = data[3],
                format = data[4],
                created_at = data[5],
                updated_at = data[6],
                lock_version = Int32.Parse(data[7]),
                text_type = data[8]
            };
        }

        public static IEnumerable<Photo> Ingest(string file)
        {
            return Ingest(file, 9, Ingest);
        }
    }
}
