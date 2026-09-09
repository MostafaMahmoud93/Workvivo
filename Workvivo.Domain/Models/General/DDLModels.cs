using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Workvivo.Domain.Models.General
{
    public class DDLModel<T>
    {
        public T Id { get; set; }
        public string Name { get; set; }
    }
    public class CustomDDLModel<T,U>
    {
        public T Id { get; set; }
        public U Id2 { get; set; }
        public string Name { get; set; }
    }
}
