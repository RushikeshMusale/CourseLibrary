using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace CourseLibrary.API.Helpers
{
    public static class ObjectExtensions
    {
        public static ExpandoObject ShapeData<TSource>(this TSource source, string fields)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            var propertyInfos = new List<PropertyInfo>();
            if(string.IsNullOrWhiteSpace(fields))
            {
                var properties = typeof(TSource).GetProperties(BindingFlags.Public | BindingFlags.Instance);
                propertyInfos.AddRange(properties);
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

                    propertyInfos.Add(propertyInfo);
                }
            }

            var dataShapedObject = new ExpandoObject();
            foreach (var propertyInfo in propertyInfos)
            {
                var propertyValue = propertyInfo.GetValue(source);
                ((IDictionary<string, object>)dataShapedObject).Add(propertyInfo.Name, propertyValue);
            }

            return dataShapedObject;
        }
    }
}
