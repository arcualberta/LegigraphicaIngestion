using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace LexigraphicaIngestion.Models
{
    public abstract class LexigraphiBase<T>
    {
        public static IEnumerable<T> Ingest(string file, int spread, Func<string[], T> onIngest)
        {
            Console.Write("Reading file: " + file + "...");

            List<T> result = new List<T>();
            string[] input = new string[spread];
            string[] data = File.ReadAllText(file).Split(new string[] { "|||" }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToArray();
            for (var i = spread; i < data.Count() - spread; i += spread)
            {
                Array.Copy(data, i, input, 0, spread);
                result.Add(onIngest(input));
            }

            Console.WriteLine("done.");
            return result;
        }
    }
}
