using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LexigraphicaIngestion.Models
{
    public abstract class LexigraphiModel<T> : LexigraphiBase<T>
    {
        public int id;
        public string created_at;
        public string updated_at;
        public int lock_version;
    }
}
