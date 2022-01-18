using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using Typocalypse.Trie;

public class GridGenerator : MonoBehaviour
{
    public int seed = 1;
    public List<string> words;

    // Public delegates
    public delegate void GridGenerationCallback(char[] gridChars);
    public static GridGenerationCallback OnGridGenerationComplete;

    public delegate void GridSolveCallback(List<StringBuilder> availableWords);
    public static GridSolveCallback OnGridSolveComplete;

    // Public props
    public int gridSize { get; set; }

    private static readonly int numLetterGroups = 11;
    private static readonly string[] weightedLetterGroups = { "e", "t", "ar", "ino", "s", "d", "chl", "fmpu", "gy", "w", "bjkqvxz" };
    private static readonly int[] weightedLetterGroupsFrequencies = { 19, 13, 12, 11, 9, 6, 5, 4, 3, 2, 1 };

    private static StringBuilder tileBag = new StringBuilder();

    private char[] gridChars;
    private System.Random random;

    Trie<string> wordsTrie;
    private bool diagonalMovesAreValid;

    private List<StringBuilder> availableWords;

    void OnEnable()
    {
        random = new System.Random(seed);
        LoadWords();
        GenerateTrie();
    }

    void OnDisable()
    {

    }

    private void LoadWords()
    {
        TextAsset wordsData = (TextAsset)Resources.Load("words");
        words = wordsData.text.Split('\n').Select(w => w.Trim().ToLower()).ToList(); // trim + lowercase
        Debug.Log("Loaded " + words.Count + " words");
    }

    public void Generate()
    {
        GenerateTileBag();
        GenerateGrid();
        OnGridGenerationComplete?.Invoke(gridChars);
    }

    private void GenerateTileBag()
    {
        tileBag.Clear();
        for (int i = 0; i < numLetterGroups; i++)
        {
            int freq = weightedLetterGroupsFrequencies[i];
            for (int j = 0; j < freq; j++)
            {
                tileBag.Append(weightedLetterGroups[i]);
            }
        }
    }

    private void GenerateGrid()
    {
        int boardSize = gridSize * gridSize;
        List<char> gridCharsList = new List<char>(boardSize);

        for (int i = 0; i < boardSize; i++)
        {
            int tileBagIndex = random.Next() % tileBag.Length;
            // Debug.Log("random tileBag index " + tileBagIndex);
            char letter = tileBag[tileBagIndex];
            //Debug.Log("Adding " + letter.ToString());
            gridCharsList.Add(letter);
        }

        gridChars = gridCharsList.ToArray();
    }

    private void GenerateTrie()
    {
        wordsTrie = new Trie<string>();

        for (int i = 0; i < words.Count; i++)
        {
            string word = words[i];
            wordsTrie.Put(word, $"{i}");
        }
    }

    public bool IndexToGrid(int index, out int x, out int y)
    {
        x = index % gridSize;
        y = index / gridSize;
        return GridIsValid(x, y);
    }

    public int GridToIndex(int x, int y)
    {
        return (y * gridSize) + x;
    }

    public bool GridIsValid(int x, int y)
    {
        if (x < 0 || x >= gridSize)
        {
            return false;
        }
        if (y < 0 || y >= gridSize)
        {
            return false;
        }

        return true;
    }

    public void Solve(bool diagonalMovesAllowed)
    {
        diagonalMovesAreValid = diagonalMovesAllowed;
        int gridSizeSqr = gridSize * gridSize;

        List<StringBuilder> retVal = new List<StringBuilder>();

        for (int i = 0; i < gridSizeSqr; i++)
        {
            bool[] visitedIndices = new bool[gridSizeSqr];

            IndexToGrid(i, out int x, out int y);

            wordsTrie.Matcher.ResetMatch();
            var output = CombinationsHelper(visitedIndices, new StringBuilder(), x, y, new List<StringBuilder>(), 0);
            retVal.InsertRange(retVal.Count, output);
        }

        Debug.Log($"Total number of Generated Words: {retVal.Count} - {string.Join(",", retVal.Select(x => x.ToString()))}");

        availableWords = retVal;

        OnGridSolveComplete?.Invoke(availableWords);
    }

    // String GetPrefix(); // Get the prefix entered so far
    // void ResetMatch(); // Clear the currently entered prefix
    // void BackMatch(); // Remove the last character of the currently entered prefix
    // bool NextMatch(char next); // Add another character to the end of the prefix if new prefix is actually a prefix to some strings in the trie.
    // List<V> GetPrefixMatches(); // Get all the corresponding values of the keys which have a prefix corresponding to the currently entered prefix.
    // bool IsExactMatch(); // Check if the currently entered prefix is an existing string in the trie
    // V GetExactMatch(); // Get the value mapped by the currently entered prefix or null        

    /// <summary>
    /// This helper function is used to generate all possible combinations of words starting at the row and column
    /// It is called recursively in all 8 directions (North, NorthEast, East, SouthEast, South, SouthWest, West, NorthWest)
    /// Everytime it append the char at it's current location to the "currentWord", and if the word is valid, it is added to
    /// the prefixList. This function uses a BitMap of boolean values to keep track of the cells it has visited to avoid getting
    /// into infinite loop
    /// </summary>
    /// <param name="matrix">The input matrix</param>
    /// <param name="visited">The boolean bitmap that tracks the visited locations</param>
    /// <param name="currentPrefix">The current prefix</param>
    /// <param name="row">The current row on the board. The valid range is 0..N-1</param>
    /// <param name="column">The current column on the board. The valid range is 0..N-1</param>
    /// <param name="prefixList">The List of the valid words</param>
    /// <returns></returns>
    private List<StringBuilder> CombinationsHelper(
        bool[] visitedIndices,
        StringBuilder currentPrefix,
        int x,
        int y,
        List<StringBuilder> prefixList,
        int depth)
    {
        // Debug.Log($"CombinationsHelper() {x},{y}");

        // if (depth > 3)
        // {
        //     return prefixList;
        // }

        int currentIndex = GridToIndex(x, y);

        // if the row or column are not in valid range [0..N-1], exit out of the function
        if (!GridIsValid(x, y))
        {
            // Debug.Log("Grid is not valid");
            return prefixList;
        }

        // if we have already visited this cell before, exit out of the function
        if (visitedIndices[currentIndex])
        {
            // Debug.Log("Visited");
            return prefixList;
        }

        StringBuilder newWord = new StringBuilder(currentPrefix.ToString());

        char currentChar = gridChars[currentIndex];
        newWord.Append(currentChar);

        bool matchesAvailable = wordsTrie.Matcher.NextMatch(currentChar);

        // Debug.Log($"At {x},{y}@{depth} newWord={newWord}, currentPrefix={currentPrefix.ToString()}, currentChar={currentChar}, Matcher.GetPrefix()={wordsTrie.Matcher.GetPrefix()}");

        if (matchesAvailable)
        {
            // Debug.Log("We have matches for " + wordsTrie.Matcher.GetPrefix());
            if (newWord.Length > 3 && wordsTrie.Matcher.IsExactMatch())
            {
                Debug.Log($"Found complete word {newWord.ToString()} - Matcher.GetPrefix()={wordsTrie.Matcher.GetPrefix()}");
                prefixList.Add(newWord);
            }

            // Debug.Log("Possible words " + string.Join(", ", wordsTrie.Matcher.GetPrefixMatches().Select(m => words[System.Convert.ToInt32(m)]).ToArray()));

            visitedIndices[currentIndex] = true;
            CombinationsHelper(visitedIndices, newWord, x, y + 1, prefixList, depth + 1); // N

            if (diagonalMovesAreValid)
            {
                CombinationsHelper(visitedIndices, newWord, x + 1, y + 1, prefixList, depth + 1); // NE
            }

            CombinationsHelper(visitedIndices, newWord, x + 1, y, prefixList, depth + 1); // E

            if (diagonalMovesAreValid)
            {
                CombinationsHelper(visitedIndices, newWord, x + 1, y - 1, prefixList, depth + 1); // SE
            }

            CombinationsHelper(visitedIndices, newWord, x, y - 1, prefixList, depth + 1); // S

            if (diagonalMovesAreValid)
            {
                CombinationsHelper(visitedIndices, newWord, x - 1, y - 1, prefixList, depth + 1); // SW
            }
            CombinationsHelper(visitedIndices, newWord, x - 1, y, prefixList, depth + 1); // W

            if (diagonalMovesAreValid)
            {
                CombinationsHelper(visitedIndices, newWord, x - 1, y + 1, prefixList, depth + 1); // NW

            }

            visitedIndices[currentIndex] = false;

            wordsTrie.Matcher.BackMatch();
        }
        else
        {
            // Debug.Log("No matches possible");
        }


        // Debug.Log($"After BackMatch(), Matcher.GetPrefix()={wordsTrie.Matcher.GetPrefix()}");

        return prefixList;
    }

}
