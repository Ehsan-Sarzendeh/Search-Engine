using SearchEngine.Models;

namespace SearchEngine.Dto;

public class SearchResult
{
    public List<Doc> Docs { get; set; }
    public string SearchQuery { get; set; }
    public int Count { get; set; }
    public double Time { get; set; }
}