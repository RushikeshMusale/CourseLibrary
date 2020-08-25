using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Threading.Tasks;

namespace CourseLibrary.API.ActionConstraints
{
    [AttributeUsage(AttributeTargets.All, Inherited = true, AllowMultiple =true)]
    public class RequestHeaderMatchesMediaTypeAttribute: Attribute, IActionConstraint
    {
        private readonly MediaTypeCollection _mediaTypes = new MediaTypeCollection();
        private readonly string _requestHeaderToMatch;

        public RequestHeaderMatchesMediaTypeAttribute(string requestHeaderToMatch, string mediaType, 
            params string[] otherMediaTypes) // params keyword denotes that method takes variable length of parameters
        {
            _requestHeaderToMatch = requestHeaderToMatch ??
                throw new ArgumentNullException(nameof(requestHeaderToMatch));
            // check if media types are valid

            if (!MediaTypeHeaderValue.TryParse(mediaType, out MediaTypeHeaderValue parsedMediaType))
                throw new ArgumentException(nameof(mediaType));

            _mediaTypes.Add(parsedMediaType);

            // simillarly add other media types

            foreach (var otherMediaType in otherMediaTypes)
            {
                if (MediaTypeHeaderValue.TryParse(otherMediaType, out MediaTypeHeaderValue parsedOtherMediaType))
                {
                    _mediaTypes.Add(parsedOtherMediaType);
                }
                else
                    throw new ArgumentException(nameof(parsedOtherMediaType));
            }
        }

        // constraints are grouped together by the value of order, run in the stages
        // stages run in ascending order based on this property
        public int Order => 0;

        public bool Accept(ActionConstraintContext context)
        {
            // lets check if the header matches

            var requestHeaders = context.RouteContext.HttpContext.Request.Headers;
            if (!requestHeaders.ContainsKey(_requestHeaderToMatch))
                return false;

            var parsedRequestMediaType = new MediaType(requestHeaders[_requestHeaderToMatch]);

            //if one of the media type matches return true

            foreach (var mediaType in _mediaTypes)
            {
                var parsedMediaType = new MediaType(mediaType);

                if (parsedMediaType.Equals(parsedRequestMediaType))
                    return true;
            }

            return false;
        }
    }
}
