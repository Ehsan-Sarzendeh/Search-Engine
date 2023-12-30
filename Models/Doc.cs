using System.Drawing;

namespace SearchEngine.Models;

public class Doc
{
    public string Url { get; set; }
    public string Title { get; set; }
    public string Content { get; set; }

    public Doc(string url, string title, string content)
    {
        Url = url;
        Title = title;
        Content = content;
    }

    public Doc()
    {
        
    }
}