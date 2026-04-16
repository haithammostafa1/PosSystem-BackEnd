using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pos.Datalayer.Helpers
{
    public class PagedList<T>
    {
        public List<T> Items { get; set; }
        public PaginationMetaData MetaData { get; set; }

        public PagedList(List<T> items, int count, int pageNumber, int pageSize)
        {
            MetaData = new PaginationMetaData(count, pageNumber, pageSize);
            Items = items;
        }
    }
}
