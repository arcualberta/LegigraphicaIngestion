using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LexigraphicaIngestion.Models
{
    public class PhotoWord : LexigraphiBase<PhotoWord>
    {
        public int photo_id;
        public int word_id;

        protected static PhotoWord Ingest(string[] data)
        {
            return new PhotoWord()
            {
                photo_id = Int32.Parse(data[0]),
                word_id = Int32.Parse(data[1])
            };
        }

        public static IEnumerable<PhotoWord> Ingest(string file)
        {
            return Ingest(file, 2, Ingest);
        }
    }
}
