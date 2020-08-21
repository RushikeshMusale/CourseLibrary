using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace CourseLibrary.API.Helpers
{
    public static class IEnumerableExtension
    {
        public static IEnumerable<ExpandoObject> ShapeData<TSource>(this IEnumerable<TSource> source, string fields)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            var expandoObjectList = new List<ExpandoObject>();

            //now we will use reflection once on the source type to get the list of properties on the source           

            var propertyInfoList = new List<PropertyInfo>();

            if(string.IsNullOrWhiteSpace(fields))
            {
                // if fields is empty, then we will take all the properties from source
                var propertyFromSourceLists = typeof(TSource).GetProperties(BindingFlags.Public | BindingFlags.Instance);
                propertyInfoList.AddRange(propertyFromSourceLists);
            }
            else
            {
                var fieldsAfterSplit = fields.Split(',');

                foreach (var field in fieldsAfterSplit)
                {
                    var propertyName = field.Trim();
                    
                    var propertyInfo = typeof(TSource).GetProperty(propertyName, 
                        BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

                    if (propertyInfo == null)
                        throw new Exception($"Property {propertyName} can not be found in the source {typeof(TSource)}");

                    propertyInfoList.Add(propertyInfo);
                }
            }

            // now we will create expando Objects from the source
            // ExpandoObject impelements IDictionary interface

            foreach (TSource sourceObject in source)    
            {
                var dataShapedObject = new ExpandoObject();
                foreach (var propertyInfo in propertyInfoList)
                {
                    var propertyValue = propertyInfo.GetValue(sourceObject);
                    ((IDictionary<string, object>)dataShapedObject).Add(propertyInfo.Name, propertyValue);
                }

                expandoObjectList.Add(dataShapedObject);
            }

            return expandoObjectList;
        }
    }
}
