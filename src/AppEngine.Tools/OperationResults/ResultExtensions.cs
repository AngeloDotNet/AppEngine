namespace AppEngine.Tools.OperationResults;

public static class ResultExtensions
{
    extension<TSource>(Result<TSource> source)
    {
        public Result<TDestination> Map<TDestination>(Func<TSource, TDestination> mapper)
        {
            ArgumentNullException.ThrowIfNull(mapper);

            if (source.Success)
            {
                return Result<TDestination>.Ok(mapper(source.Content));
            }

            return new(false, default, source.FailureReason, source.ErrorMessage, source.ErrorDetail, source.Error, source.ValidationErrors);
        }
    }

    extension<TSource>(Result<PaginatedList<TSource>> source)
    {
        public Result<PaginatedList<TDestination>> MapPaginated<TDestination>(Func<TSource, TDestination> mapper)
        {
            ArgumentNullException.ThrowIfNull(mapper);

            if (source.Success)
            {
                var mappedItems = source.Content.Items?.Select(mapper);
                var mappedList = new PaginatedList<TDestination>(mappedItems, source.Content.TotalCount, source.Content.PageIndex, source.Content.PageSize, source.Content.HasNextPage);

                return Result<PaginatedList<TDestination>>.Ok(mappedList);
            }

            return new(false, default, source.FailureReason, source.ErrorMessage, source.ErrorDetail, source.Error, source.ValidationErrors);
        }
    }
}