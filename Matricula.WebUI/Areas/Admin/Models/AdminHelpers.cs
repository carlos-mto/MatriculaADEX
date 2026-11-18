using System.Globalization;

namespace Matricula.WebUI.Areas.Admin.Models;

public class Pager
{
    public int Page { get; }
    public int PageSize { get; }
    public int Total { get; }
    public int TotalPages { get; }

    public Pager(int page, int pageSize, int total)
    {
        PageSize = pageSize;
        Total = total;
        TotalPages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
        Page = Math.Clamp(page, 1, TotalPages);
    }
}

public static class PagedExtensions
{
    public const int PageSize = 20;

    // The query must already be ordered.
    public static async Task<(List<T> Items, Pager Pager)> ToPagedAsync<T>(this IQueryable<T> query, int page, int pageSize = PageSize)
    {
        var pager = new Pager(page, pageSize, await query.CountAsync());
        var items = await query.Skip((pager.Page - 1) * pageSize).Take(pageSize).ToListAsync();
        return (items, pager);
    }
}

public static class LimaTime
{
    private static readonly TimeZoneInfo Zone = Find();

    private static TimeZoneInfo Find()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("America/Lima"); }
        catch (Exception e) when (e is TimeZoneNotFoundException or InvalidTimeZoneException) { return TimeZoneInfo.Utc; }
    }

    public static string Format(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Zone).ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);
}

public record DeleteButtonModel(Guid Id, string Message, string Action = "Delete", string Label = "Eliminar");
