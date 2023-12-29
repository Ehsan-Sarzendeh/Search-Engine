namespace SearchEngine.Models;

public class PostingList
{
    public int Repetition { get; set; }
    public Dictionary<string, int> Docs { get; set; }

    public PostingList()
    {
        Docs = new Dictionary<string, int>();
    }

    public void Add(string url, Weight weight)
    {
        Repetition += 1;
        if (Docs.ContainsKey(url))
        {
            var currentWeight = Docs[url]!;
            Docs[url] = currentWeight + (int)weight;
        }
        else
        {
            Docs.Add(url, (int)weight);
        }
    }

    public Dictionary<string, int> InFiles()
    {
        return Docs;
    }
}

public enum Weight
{
    Title = 200,
    Content = 1
}