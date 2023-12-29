using System.Collections;

namespace SearchEngine.Models;

public class PostingList
{
    private Dictionary<Doc, int> Docs { get; set; }

    public PostingList()
    {
        Docs = new Dictionary<Doc, int>();
    }

    public void Add(Doc infile, Weight weight)
    {
        if (Docs.ContainsKey(infile))
        {
            var currentWeight = (int)Docs[infile]!;
            Docs[infile] = currentWeight + (int)weight;
        }
        else
        {
            Docs.Add(infile, (int)weight);
        }
    }

    public Dictionary<Doc, int> InFiles()
    {
        return Docs;
    }
}

public enum Weight
{
    Title = 3,
    Content = 1
}