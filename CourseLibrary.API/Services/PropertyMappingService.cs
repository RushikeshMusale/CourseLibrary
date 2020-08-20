using CourseLibrary.API.Entities;
using CourseLibrary.API.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CourseLibrary.API.Services
{
    public class PropertyMappingService : IPropertyMappingService
    {
        private Dictionary<string, PropertyMappingValue> _authorPropertyMapping =
            new Dictionary<string, PropertyMappingValue>(StringComparer.OrdinalIgnoreCase)
            {
                {"Id", new PropertyMappingValue(new List<string>(){"Id"} ) },
                {"MainCategory", new PropertyMappingValue(new List<string>(){"MainCategory"} ) },
                {"Name", new PropertyMappingValue(new List<string>(){"FirstName","LastName"} ) },
                {"Age", new PropertyMappingValue(new List<string>(){ "DateOfBirth" }, true ) },
            };


        // Extracted IPropertyMapping Interface because IList did not allow to have IList<PropertyMapping<Tsource, TDestination>>
        // Because we can have place holders at the class level or method level.
        // but we want our propertyMappings to have mapping as <Tsource,TDestination> 
        // NOTE: that each PropertyMapping can have different Tsource & TDestination
        private IList<IPropertyMapping> _propertyMappings = new List<IPropertyMapping>();

        public bool ValidMappingExsitsFor<TSource, TDestination>(string fields) // field is orderBy clause
        {
            var propertyMapping = GetPropertyMapping<TSource, TDestination>();

            if (string.IsNullOrWhiteSpace(fields))
                return true;

            var fieldAfterSplit = fields.Split(',');

            foreach (var field in fieldAfterSplit)
            {
                var trimmedField = field.Trim();

                var indexOfFirstSpace = trimmedField.IndexOf(" ");

                var propertyName = (indexOfFirstSpace == -1) ?
                    string.Empty : trimmedField.Remove(indexOfFirstSpace);

                if (!propertyMapping.ContainsKey(propertyName))
                    return false;
            }
            return true;
        }

        public PropertyMappingService()
        {
            _propertyMappings.Add(new PropertyMapping<AuthorDto, Author>(_authorPropertyMapping));
        }
        public Dictionary<string, PropertyMappingValue> GetPropertyMapping<TSource, TDestination>()
        {

            // IEnumerable.OfType<T> returns array of objects which has type T
            var matchingMapping = _propertyMappings.OfType<PropertyMapping<TSource, TDestination>>();

            if (matchingMapping.Count() > 0)
            {
                return matchingMapping.First()._mappingDictionary;
            }
            else
                throw new Exception($"Cannot find exact property mapping instance " + $"for <{typeof(TSource)}, {typeof(TDestination)}>");
        }
    }
}
