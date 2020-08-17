using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CourseLibrary.API.ResourceParameters
{
    public class AuthorsResourceParameters
    {
        const int maxPageSize = 20;
        public string MainCategory { get; set; }
        public string SearchQuery { get; set; }

        public int PageNumber { get; set; } = 1;

        private int _pageSize =10;

        // Traditional way of declaring properties
        //public int PageSize {
        //    get { return _pageSize; }
        //    set { _pageSize = value > maxPageSize ? maxPageSize : value; }
        //}

        // New way of declaring properties
        public int PageSize 
        { 
            get => _pageSize; 
            set => _pageSize = (value > maxPageSize) ? maxPageSize : value; 
        }
    }
}
