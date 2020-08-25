using AutoMapper;
using CourseLibrary.API.Helpers;
using CourseLibrary.API.Models;
using CourseLibrary.API.ResourceParameters;
using CourseLibrary.API.Services;

using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CourseLibrary.API.Entities;
using System.Dynamic;
using System.Reflection.Metadata;
using Microsoft.Net.Http.Headers;
using CourseLibrary.API.ActionConstraints;

namespace CourseLibrary.API.Controllers
{
    [ApiController]
    [Route("api/authors")]
    public class AuthorsController : ControllerBase
    {
        private readonly ICourseLibraryRepository _courseLibraryRepository;
        private readonly IMapper _mapper;
        private readonly IPropertyMappingService _propertyMappingService;
        private readonly IPropertyCheckerService _propertyCheckerService;

        public AuthorsController(ICourseLibraryRepository courseLibraryRepository,
            IMapper mapper, 
            IPropertyMappingService propertyMappingService,
            IPropertyCheckerService propertyCheckerService)
        {
            _courseLibraryRepository = courseLibraryRepository ??
                throw new ArgumentNullException(nameof(courseLibraryRepository));
            _mapper = mapper ??
                throw new ArgumentNullException(nameof(mapper));
            _propertyMappingService = propertyMappingService ??
                throw new ArgumentNullException(nameof(propertyMappingService));
            _propertyCheckerService = propertyCheckerService ??
                throw new ArgumentNullException(nameof(propertyCheckerService));
        }

        [HttpGet(Name ="GetAuthors")]
        [HttpHead]
        public IActionResult GetAuthors(
            [FromQuery] AuthorsResourceParameters authorsResourceParameters)
        {
            if (!_propertyCheckerService.TypeHasProperties<AuthorDto>(authorsResourceParameters.Fields))
                return BadRequest();

            if (!_propertyMappingService.
                ValidMappingExsitsFor<AuthorDto,Author>(authorsResourceParameters.OrderBy) )
            {
                return BadRequest();
            }

            var authorsFromRepo = _courseLibraryRepository.GetAuthors(authorsResourceParameters);

            // Let's create pagination metadata, it consists,
            // total count, pagesize, current page, total pages, 
            // link to previous & next pages

           
            var paginationMetaData = new
            {
                totalCount = authorsFromRepo.TotalCount,
                pageSize = authorsFromRepo.PageSize,
                currentPage = authorsFromRepo.CurrentPage,
                totalPages = authorsFromRepo.TotalPages
            };

            // let's add it to response using System.Text.Json
            Response.Headers.Add("X-Pagination", JsonSerializer.Serialize(paginationMetaData));

            var links = CreateLinksForAuthors(authorsResourceParameters, authorsFromRepo.HasPrevious, authorsFromRepo.HasNext);

            var shapedAuthors = _mapper.Map<IEnumerable<AuthorDto>>(authorsFromRepo)
                .ShapeData<AuthorDto>(authorsResourceParameters.Fields);

            var shapedAuthorsWithLinks =  shapedAuthors.Select(author =>
            {
                var authorAsDictionary = author as IDictionary<string, object>;
                var authorLinks = CreateLinksForAuthor((Guid)authorAsDictionary["Id"], null);
                authorAsDictionary.Add("links", authorLinks);
                return authorAsDictionary;
            });

            var linkedCollectionResource = new
            {
                value = shapedAuthorsWithLinks,
                links
            };
            return Ok(linkedCollectionResource);
        }

        [HttpGet("{authorId}", Name ="GetAuthor")]
        [Produces("application/json",
            "application/vnd.marvin.author.full+json",
            "application/vnd.marvin.author.full.hateoas+json",
            "application/vnd.marvin.author.friendly+json",
            "application/vnd.marvin.author.friendly.hateoas+json")]
        public IActionResult GetAuthor(Guid authorId, string fields,
            [FromHeader(Name = "Accept")] string mediaType)
        {

            if(!MediaTypeHeaderValue.TryParse(mediaType, 
                out MediaTypeHeaderValue parsedMediaType))
            {
                return BadRequest();
            }

            if (!_propertyCheckerService.TypeHasProperties<AuthorDto>(fields))
                return BadRequest();

            var authorFromRepo = _courseLibraryRepository.GetAuthor(authorId);

            if (authorFromRepo == null)
            {
                return NotFound();
            }

            // StringComparison.OrdinalIgnoreCase compares string byte by byte
            // and performance is better than StringComparison.InvariantCultureIgnoreCase


            var includedLinks = parsedMediaType.SubTypeWithoutSuffix.EndsWith("hateoas", 
                StringComparison.InvariantCultureIgnoreCase);

            IEnumerable<LinkDto> links = new List<LinkDto>();
            
            if(includedLinks)
            {
                links = CreateLinksForAuthor(authorId, fields);
            }

            var primaryMediaType = includedLinks ?
                parsedMediaType.SubTypeWithoutSuffix.Substring(0, parsedMediaType.SubTypeWithoutSuffix.Length - 8)  // 8 -> hateoas.
                : parsedMediaType.SubTypeWithoutSuffix;

            // NOTE: To use the custom mediaTypes we have to add output formatter to MVC Options
            // or we can add it to controller action instead of global level with help of Produces attribute
            if (primaryMediaType == "vnd.marvin.author.full")
            {
                var fullResourceToReturn = _mapper.Map<AuthorFullDto>(authorFromRepo)
                    .ShapeData<AuthorFullDto>(fields) as IDictionary<string, object>;

                if(includedLinks)
                {                    
                    fullResourceToReturn.Add("links", links);                    
                }

                return Ok(fullResourceToReturn);
            }

            var friendlyResourceToReturn = _mapper.Map<AuthorDto>(authorFromRepo)
                .ShapeData<AuthorDto>(fields) as IDictionary<string, object>;

            if (includedLinks)
            {
                friendlyResourceToReturn.Add("links", links);
            }

            return Ok(friendlyResourceToReturn);           
          
        }

        [HttpPost(Name = "CreateAuthorWithDateOfDeath")]
        [RequestHeaderMatchesMediaType("Content-Type",
          "application/vnd.marvin.authorforcreationwithdateofdeath+json")]
        [Consumes("application/vnd.marvin.authorforcreationwithdateofdeath+json")]
        //for this demo purpose, implementation is exactly same as for the CreateAuthor
        public ActionResult<AuthorDto> CreateAuthorWithDateOfDeath(AuthorForCreationWithDateOfDeathDto author)
        {
            var authorEntity = _mapper.Map<Entities.Author>(author);
            _courseLibraryRepository.AddAuthor(authorEntity);
            _courseLibraryRepository.Save();

            var authorToReturn = _mapper.Map<AuthorDto>(authorEntity);

            var links = CreateLinksForAuthor(authorToReturn.Id, null);

            var linkedResourceToReturn = authorToReturn.ShapeData<AuthorDto>(null)
                                            as IDictionary<string, object>;

            linkedResourceToReturn.Add("links", links);

            return CreatedAtRoute("GetAuthor",
                new { authorId = authorToReturn.Id },
                linkedResourceToReturn);
        }


        [HttpPost(Name ="CreateAuthor")]
        [RequestHeaderMatchesMediaType("Content-Type",
            "application/json",
            "application/vnd.marvin.authorforcreation+json")]
        [Consumes("application/json",
            "application/vnd.marvin.authorforcreation+json")]
        public ActionResult<AuthorDto> CreateAuthor(AuthorForCreationDto author)
        {
            var authorEntity = _mapper.Map<Entities.Author>(author);
            _courseLibraryRepository.AddAuthor(authorEntity);
            _courseLibraryRepository.Save();

            var authorToReturn = _mapper.Map<AuthorDto>(authorEntity);
   
            var links = CreateLinksForAuthor(authorToReturn.Id, null);

            var linkedResourceToReturn = authorToReturn.ShapeData<AuthorDto>(null)
                                            as IDictionary<string, object>;

            linkedResourceToReturn.Add("links", links);

            return CreatedAtRoute("GetAuthor",
                new { authorId = authorToReturn.Id },
                linkedResourceToReturn);
        }

      
        [HttpOptions]
        public IActionResult GetAuthorsOptions()
        {
            Response.Headers.Add("Allow", "GET,OPTIONS,POST");
            return Ok();
        }

        [HttpDelete("{authorId}", Name = "DeleteAuthor")]
        public ActionResult DeleteAuthor(Guid authorId)
        {
            var authorFromRepo = _courseLibraryRepository.GetAuthor(authorId);

            if (authorFromRepo == null)
            {
                return NotFound();
            }

            _courseLibraryRepository.DeleteAuthor(authorFromRepo);

            _courseLibraryRepository.Save();

            return NoContent();
        }

        private string CreateAuthorResourceUri(AuthorsResourceParameters authorsResourceParameters,
            ResourceUriType resourceUriType)
        {
            switch(resourceUriType)
            {
                case ResourceUriType.PreviousPage:
                    return Url.Link("GetAuthors", 
                        new
                        {
                            fields = authorsResourceParameters.Fields,
                            orderBy = authorsResourceParameters.OrderBy,
                            pageNumber = authorsResourceParameters.PageNumber-1,
                            pageSize = authorsResourceParameters.PageSize,
                            mainCategory = authorsResourceParameters.MainCategory,
                            searchQuery = authorsResourceParameters.SearchQuery,
                        });

                case ResourceUriType.NextPage:
                    return Url.Link("GetAuthors",
                        new
                        {
                            fields = authorsResourceParameters.Fields,
                            orderBy = authorsResourceParameters.OrderBy,
                            pageNumber = authorsResourceParameters.PageNumber + 1,
                            pageSize = authorsResourceParameters.PageSize,
                            mainCategory = authorsResourceParameters.MainCategory,
                            searchQuery = authorsResourceParameters.SearchQuery
                        });

                case ResourceUriType.Current:
                default:
                    return Url.Link("GetAuthors",
                        new
                        {
                            fields = authorsResourceParameters.Fields,
                            orderBy = authorsResourceParameters.OrderBy,
                            pageNumber = authorsResourceParameters.PageNumber,
                            pageSize = authorsResourceParameters.PageSize,
                            mainCategory = authorsResourceParameters.MainCategory,
                            searchQuery = authorsResourceParameters.SearchQuery
                        });
            }
        }

        private IEnumerable<LinkDto> CreateLinksForAuthor(Guid authorId, string fields)
        {
            // this is a place to add links related to authors
            // so if we have any buisenss constraints for a capability, 
            // here should we decide to add or not the link
            var links = new List<LinkDto>();
            if(string.IsNullOrWhiteSpace(fields))
            {
                links.Add(
                    new LinkDto(
                        Url.Link("GetAuthor", new { authorId }),
                        "self",
                        "GET"));
            }
            else
            {
                links.Add(
                    new LinkDto(
                        Url.Link("GetAuthor", new { authorId , fields}),
                        "self",
                        "GET"));
            }

            links.Add(
                    new LinkDto(
                        Url.Link("DeleteAuthor", new { authorId }),
                        "delete_author",
                        "DELETE"));

            links.Add(
                new LinkDto(
                        Url.Link("CreateCourseForAuthor", new {authorId}),
                        "create_course_for_author",
                        "POST"
                    ));

            links.Add(
                new LinkDto(
                        Url.Link("GetCoursesForAuthor", new { authorId }),
                        "courses",
                        "GET"
                    ));            

            return links;
        }


        public IEnumerable<LinkDto> CreateLinksForAuthors(AuthorsResourceParameters authorsResourceParameters, bool hasPrevious, bool hasNext)
        {
            var links = new List<LinkDto>();

            links.Add(
                new LinkDto(
                    CreateAuthorResourceUri(authorsResourceParameters, ResourceUriType.Current),
                    "self",
                    "GET"
                    ));

            if(hasPrevious)
            {
                links.Add(
                    new LinkDto( 
                        CreateAuthorResourceUri(authorsResourceParameters, ResourceUriType.PreviousPage),
                        "previous-page",
                        "GET"
                    ));
            }

            if (hasNext)
            {
                links.Add(
                    new LinkDto(
                        CreateAuthorResourceUri(authorsResourceParameters, ResourceUriType.NextPage),
                        "next-page",
                        "GET"
                    ));
            }
            return links;
        }
    }
}
