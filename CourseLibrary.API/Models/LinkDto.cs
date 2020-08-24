using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CourseLibrary.API.Models
{
    public class LinkDto
    {
        public string Href { get; private set; }

        public string Link { get; private set; }

        public string Method { get; private set; }

        public LinkDto(string href, string link, string method)
        {
            Href = href;
            Link = link;
            Method = method;
        }

    }
}
