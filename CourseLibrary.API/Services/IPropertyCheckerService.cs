namespace CourseLibrary.API.Services
{
    public interface IPropertyCheckerService
    {
        bool TypeHasProperties<TSource>(string fields);
    }
}