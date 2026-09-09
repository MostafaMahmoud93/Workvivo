namespace Workvivo.Domain.Models.General;
public class PaggingModel
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public bool IsSortAsc { get; set; } = true;
    public string? SortBy { get; set; }
    public string? FilterSearch { get; set; }
}
