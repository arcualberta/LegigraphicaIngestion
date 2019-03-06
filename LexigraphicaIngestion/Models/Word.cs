using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LexigraphicaIngestion.Models
{
    public class Word : LexigraphiModel<Word>
    {
        public string word;

        protected static Word Ingest(string[] data)
        {
            return new Word()
            {
                id = Int32.Parse(data[0]),
                word = data[1],
                created_at = data[2],
                updated_at = data[3],
                lock_version = Int32.Parse(data[4])
            };
        }

        public static IEnumerable<Word> Ingest(string file)
        {
            return Ingest(file, 5, Ingest);
        }
    }
}
