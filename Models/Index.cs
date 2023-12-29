namespace SearchEngine.Models;

public class Index
{
    private readonly Dictionary<string, PostingList> _index;

    public Index()
    {
        _index = new Dictionary<string, PostingList>();
    }

    public void Add(string word, Doc infile, Weight weight)
    {
        if (_index.ContainsKey(word))
        {
            var postingList = _index[word];
            postingList!.Add(infile, weight);
        }
        else
        {
            var postingList = new PostingList();
            postingList.Add(infile, weight);
            _index.Add(word, postingList);
        }
    }

    public Dictionary<Doc, int> Search(string searchWord)
    {
        if (!_index.ContainsKey(searchWord)) return null;

        var postingList = _index[searchWord];
        return postingList!.InFiles();
    }

    public string[] Words()
    {
        return _index.Keys.ToArray();
    }
}
