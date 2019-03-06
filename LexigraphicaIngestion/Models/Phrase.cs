using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LexigraphicaIngestion.Models
{
    public class Phrase : LexigraphiModel<Phrase>
    {
        public int photo_id;
        public string phrase;

        protected static Phrase Ingest(string[] data)
        {
            return new Phrase()
            {
                id = Int32.Parse(data[0]),
                photo_id = Int32.Parse(data[1]),
                phrase = data[2],
                created_at = data[3],
                updated_at = data[4],
                lock_version = Int32.Parse(data[5])
            };
        }

        public static IEnumerable<Phrase> Ingest(string file)
        {
            return Ingest(file, 6, Ingest);
        }
    }
}
