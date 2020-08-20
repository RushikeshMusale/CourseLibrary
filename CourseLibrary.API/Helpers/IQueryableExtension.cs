using CourseLibrary.API.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;

namespace CourseLibrary.API.Helpers
{
    public static class IQueryableExtension
    {
        public static IQueryable<T> ApplySort<T>(this IQueryable<T> source, 
            string orderBy, 
            Dictionary<string, PropertyMappingValue> mappingDictionary )
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            if (mappingDictionary == null)
                throw new ArgumentNullException(nameof(mappingDictionary));

            if (string.IsNullOrWhiteSpace(orderBy))
                return source;

            var orderByString = "";

            // orderby clause contains properties separated by comma
            // it can contain ascending or descending clause in each property separated by space
            var orderbyAfterSplit = orderBy.Split(',');
           
            foreach (var orderByClause in orderbyAfterSplit)
            {
                // Each property 
                var trimmedOrderByClause = orderByClause.Trim();

                var orderDescending = trimmedOrderByClause.EndsWith("desc");

                var indexOfFirstSpace = trimmedOrderByClause.IndexOf(" ");

                // Source Property name from the model
                var propertyName = indexOfFirstSpace == -1 ? 
                    trimmedOrderByClause : trimmedOrderByClause.Remove(indexOfFirstSpace);

                // find the matching property
                if (!mappingDictionary.ContainsKey(propertyName))
                    throw new ArgumentException($"Key mapping for {propertyName} is missing");

                var propertyMappingValue = mappingDictionary[propertyName];

                if (propertyMappingValue == null)
                    throw new ArgumentNullException("PropertyMappingValue");

                if (propertyMappingValue.Revert)
                    orderDescending = !orderDescending;

                // Prepare OrderBy Clause as required for the dynamic linq.sort method 
                // add each property with proper ascending or descending order.
                // for ex on collection of Author we want to sort ny name then collection.Sort("Name descending")                


                foreach (var destinationProperty in propertyMappingValue.DestinationProperties)
                {
                    
                    orderByString = orderByString
                        + (string.IsNullOrWhiteSpace(orderByString) ? string.Empty : ",")
                        + destinationProperty
                        + (orderDescending ? " descending" : " ascending");
                }
                
            }

            return source.OrderBy(orderByString);
        }
    }
}
