namespace Pursuit.Application.Common;

public static class PaginationPolicy
{
    public const int DefaultPageSize = 10;
    public const int MaxPageSize = 50;

    public static void EnsureValid(int page, int pageSize)
    {
        if (page < 1)
            throw new ArgumentException("Page must be at least 1.", nameof(page));

        if (pageSize < 1 || pageSize > MaxPageSize)
        {
            throw new ArgumentException(
                $"Page size must be between 1 and {MaxPageSize}.",
                nameof(pageSize));
        }
    }
}
