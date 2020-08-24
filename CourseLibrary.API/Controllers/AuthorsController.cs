﻿using AutoMapper;
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

            var pageNextLink = authorsFromRepo.HasNext ? 
                                    CreateAuthorResourceUri(authorsResourceParameters, 
                                    ResourceUriType.NextPage) : null;

            var pagePreviousLink = authorsFromRepo.HasPrevious ?
                                    CreateAuthorResourceUri(authorsResourceParameters,
                                    ResourceUriType.PreviousPage) : null;

            var paginationMetaData = new
            {
                totalCount = authorsFromRepo.TotalCount,
                pageSize = authorsFromRepo.PageSize,
                currentPage = authorsFromRepo.CurrentPage,
                totalPages = authorsFromRepo.TotalPages,
                pagePreviousLink,
                pageNextLink
            };

            // let's add it to response using System.Text.Json
            Response.Headers.Add("X-Pagination", JsonSerializer.Serialize(paginationMetaData));

            return Ok(_mapper.Map<IEnumerable<AuthorDto>>(authorsFromRepo)
                .ShapeData<AuthorDto>(authorsResourceParameters.Fields));
        }

        [HttpGet("{authorId}", Name ="GetAuthor")]
        public IActionResult GetAuthor(Guid authorId, string fields)
        {
            if (!_propertyCheckerService.TypeHasProperties<AuthorDto>(fields))
                return BadRequest();

            var authorFromRepo = _courseLibraryRepository.GetAuthor(authorId);

            if (authorFromRepo == null)
            {
                return NotFound();
            }

            var links = CreateLinksForAuthor(authorId, fields);

            var linkedResourceToReturn = _mapper.Map<AuthorDto>(authorFromRepo)
                                            .ShapeData<AuthorDto>(fields) as IDictionary<string, object>;

            linkedResourceToReturn.Add("links", links);

            return Ok(linkedResourceToReturn);
        }

        [HttpPost]
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
    }
}
