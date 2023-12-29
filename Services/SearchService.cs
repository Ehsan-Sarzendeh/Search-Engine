using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;
using HtmlAgilityPack;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Newtonsoft.Json;
using SearchEngine.Dto;
using SearchEngine.Models;
using Index = SearchEngine.Models.Index;

namespace SearchEngine.Services;

public class SearchService
{
    private Index _index;

    public SearchService()
    {
        SetIndex();
    }

    #region Indexing
    private void SetIndex()
    {
        if (_index != null) return;

        Indexing();

        // if (File.Exists("index.json")) LoadIndex();
        // else Indexing();
    }
    private void LoadIndex()
    {
        var json = File.ReadAllText("index.json");
        _index = JsonConvert.DeserializeObject<Index>(json);
    }
    private void Indexing()
    {
        var watch = Stopwatch.StartNew();

        _index = new Index();

        var xmlDoc = new XmlDocument();
        xmlDoc.Load("output.xml");

        foreach (XmlNode node in xmlDoc.ChildNodes[0]!.ChildNodes)
        {
            var url = node.ChildNodes[0]!.InnerText;
            var html = node.ChildNodes[1]!.InnerText;

            var docTitle = Regex.Match(html, "<title>([^<]*)</title>", RegexOptions.IgnoreCase | RegexOptions.Multiline).Groups[1].Value;
            docTitle = NormalizeString(docTitle);

            var docDescription = Regex.Match(html, "<meta name=\"description\" content=\"([^<]*)\" />", RegexOptions.IgnoreCase | RegexOptions.Multiline).Groups[1].Value;
            docDescription = NormalizeString(docDescription);

            html = RemoveTags(html, "title", "script", "noscript");
            var htmlContent = NormalizeHtml(html);

            if (string.IsNullOrEmpty(docDescription))
            {
                docDescription = htmlContent.Length switch
                {
                    > 350 => htmlContent[..350] + " ...",
                    > 100 => htmlContent[..100] + " ...",
                    _ => htmlContent
                };
            }

            var doc = new Doc(url, docTitle, docDescription);

            foreach (var word in docTitle.Split(' '))
            {
                if (string.IsNullOrEmpty(word.Trim()) || string.IsNullOrWhiteSpace(word.Trim())) continue;
                _index.Add(word, doc, Weight.Title);
            }

            foreach (var word in htmlContent.Split(' '))
            {
                if (string.IsNullOrEmpty(word.Trim()) || string.IsNullOrWhiteSpace(word.Trim())) continue;
                _index.Add(word, doc, Weight.Content);
            }
        }

        watch.Stop();

        Console.WriteLine($"Indexing time: {watch.ElapsedMilliseconds / 1000d} seconds");

        var json = JsonConvert.SerializeObject(_index);
        File.WriteAllText("index.json", json);
    }
    private static string NormalizeHtml(string strHtml)
    {
        // Replace all tags with a space
        strHtml = Regex.Replace(strHtml, "<(.|\n)+?>", " ");

        // Remove all WhiteSpace and compress all WhiteSpace to one space
        strHtml = Regex.Replace(strHtml, @"\s+", " ");

        // Remove Prices
        // strHtml = Regex.Replace(strHtml, @"[0-9],+", string.Empty);

        strHtml = strHtml
            .Replace("<", "&lt;")
            .Replace(">", "&gt;");

        strHtml = NormalizeString(strHtml);

        return strHtml;
    }

    private static string NormalizeString(string input)
    {
        const string pattern = "[\\~#%&*{}/:;,.،!()<>«»?|\"-]";

        input = Regex.Replace(input, pattern, "");

        input = input
            .Replace("ﮎ", "ک")
            .Replace("ﮏ", "ک")
            .Replace("ﮐ", "ک")
            .Replace("ﮑ", "ک")
            .Replace("ك", "ک")
            .Replace("ي", "ی")
            .Replace("ھ", "ه")
            .Replace(" ", " ")
            .Replace("‌", " ")
            .Replace("|", "")
            .Replace("x200C", "");

        return input;
    }

    private static string RemoveTags(string html, params string[] tags)
    {
        return tags
            .Aggregate(html, (current, tag) => new Regex($@"<{tag}[^>]*>[\s\S]*?</{tag}>")
            .Replace(current, ""));
    }

    #endregion

    #region Search
    public SearchResult Search(string query)
    {
        var watch = Stopwatch.StartNew();

        var regex = new Regex(@"\s+");
        query = regex.Replace(query.Trim(), " ");
        query = NormalizeString(query);
        var words = query.Split(' ');

        #region Search In Index

        var searchResults = new Dictionary<Doc, int>[words.Length];

        var allMatch = true;
        int indexOfShortestResultSet = -1, lengthOfShortestResultSet = -1;

        for (var i = 0; i < words.Length; i++)
        {
            words[i] = words[i].Trim().ToLower();

            var postingList = _index.Search(words[i]);
            if (postingList is null)
            {
                var properWord =
                    (from word in _index.Words()
                     let distance = StringDistance(word, words[i])
                     where distance <= 2
                     orderby distance
                     select word)
                    .ToList().FirstOrDefault();

                if (properWord != null)
                {
                    words[i] = properWord;
                    postingList = _index.Search(words[i]);
                }

                if (postingList is null)
                {
                    allMatch = false;
                    break;
                };
            };

            searchResults[i] = postingList;

            var resultsInThisSet = searchResults[i].Count;

            if (lengthOfShortestResultSet == -1 || lengthOfShortestResultSet > resultsInThisSet)
            {
                indexOfShortestResultSet = i;
                lengthOfShortestResultSet = resultsInThisSet;
            }
        }

        #endregion

        #region Subscribing

        var finalResult = new Dictionary<Doc, int>();

        if (allMatch)
        {
            var shortResultsSet = searchResults[indexOfShortestResultSet];

            foreach (var (doc, docWeight) in shortResultsSet)
            {
                int matchCount = 0, weight = 0;

                for (var i = 0; i < searchResults.Length; i++)
                {
                    if (i == indexOfShortestResultSet)
                    {
                        matchCount += 1;
                        weight += docWeight;
                    }
                    else
                    {
                        var searchResultsIndex = searchResults[i];

                        foreach (var (indexDoc, indexWeight) in searchResultsIndex)
                        {
                            if (doc != indexDoc) continue;

                            matchCount += 1;
                            weight += indexWeight;
                            break;
                        }

                    }
                }

                if (matchCount != words.Length) continue;

                if (!finalResult.ContainsKey(doc))
                    finalResult.Add(doc, weight);
            }
        }

        #endregion

        var docs = finalResult
            .OrderByDescending(x => x.Value)
            .Select(x => x.Key)
            .ToList();

        watch.Stop();

        return new SearchResult()
        {
            Docs = docs,
            SearchQuery = string.Join(' ', words),
            Count = docs.Count,
            Time = watch.ElapsedMilliseconds / 1000d
        };
    }
    private static int StringDistance(string s, string t)
    {
        // Special cases
        if (s == t) return 0;
        if (s.Length == 0) return t.Length;
        if (t.Length == 0) return s.Length;
        // Initialize the distance matrix
        var distance = new int[s.Length + 1, t.Length + 1];
        for (var i = 0; i <= s.Length; i++) distance[i, 0] = i;
        for (var j = 0; j <= t.Length; j++) distance[0, j] = j;
        // Calculate the distance
        for (var i = 1; i <= s.Length; i++)
        {
            for (var j = 1; j <= t.Length; j++)
            {
                var cost = s[i - 1] == t[j - 1] ? 0 : 1;
                distance[i, j] = Math.Min(Math.Min(distance[i - 1, j] + 1, distance[i, j - 1] + 1), distance[i - 1, j - 1] + cost);
            }
        }
        // Return the distance
        return distance[s.Length, t.Length];
    }
    #endregion
}