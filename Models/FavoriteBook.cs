using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UP_02.Models
{
    public class FavoriteBook
    {
            public int Id { get; set; }
            public int BookId { get; set; }
            public string Title { get; set; }
            public string Author { get; set; }
            public string ImageUrl { get; set; }
            public string AddedDate { get; set; }
        
    }
}
