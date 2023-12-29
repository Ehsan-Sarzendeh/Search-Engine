namespace SearchEngine.Models;

public class Index
{
    public Dictionary<string, PostingList> IndexDic { get; set; }

    public Index()
    {
        IndexDic = new Dictionary<string, PostingList>();
    }

    public void Add(string word, string url, Weight weight)
    {
        if (IndexDic.ContainsKey(word))
        {
            var postingList = IndexDic[word];
            postingList!.Add(url, weight);
        }
        else
        {
            var postingList = new PostingList();
            postingList.Add(url, weight);
            IndexDic.Add(word, postingList);
        }
    }

    public Dictionary<string, int> Search(string searchWord)
    {
        if (!IndexDic.ContainsKey(searchWord)) return null;

        var postingList = IndexDic[searchWord];
        return postingList!.InFiles();
    }

    public string[] Words()
    {
        return IndexDic.Keys.ToArray();
    }
}
