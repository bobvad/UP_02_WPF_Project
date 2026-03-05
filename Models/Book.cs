using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UP_02.Models
{
    public class Book
    {
        public int Id { get; set; }
        public string Genre { get; set; }
        public int? Year { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public string ImageUrl { get; set; }
        public string Description { get; set; }
        public string Language { get; set; }
        public string Content { get; set; }
        public int PageCount { get; set; }
        public bool IsCompleted { get; set; }
    }
}
