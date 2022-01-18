using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class WordGridController : MonoBehaviour
{
    public int gridSize = 4;
    public GameObject letterButtonPrefab;
    public GameObject availableWordTextPrefab;

    public TMPro.TextMeshProUGUI currentWord;
    public GridLayoutGroup gridLayoutGroup;
    public RectTransform availableWordsContainer;
    public GridGenerator gridGenerator;

    public bool diagonalMovesAllowed;
    public bool generateOnStart;
    public bool autoSolve;
    public int minimumWordCount = 0;

    private int consecutiveGenerationCount = 0;
    private int consecutiveGenerationCountLimit = 20;

    private List<string> gridCharacters = new List<string>();
    private List<int> activeLetters = new List<int>();

    private List<Button> buttonComponents = new List<Button>();
    private List<TMPro.TextMeshProUGUI> buttonTextComponents = new List<TMPro.TextMeshProUGUI>();

    private char[] generatedGridChars;
    private int generatedGridSize = 0;
    private int generatedGridSizeSqr = 0;

    private int lastClickedIndex = -1;

    public enum ButtonState
    {
        Normal,
        Selected
    }
    private ButtonState[] generatedGridButtonStates;

    private string currentGuess;

    // Start is called before the first frame update
    void OnEnable()
    {
        GenerateUIIfRequired();
        GridGenerator.OnGridGenerationComplete += OnGridGenerationCompleteHandler;
        GridGenerator.OnGridSolveComplete += OnGridSolveCompleteHandler;
    }

    void Start()
    {
        if (generateOnStart)
        {
            Generate();
        }

    }

    void OnDisable()
    {
        GridGenerator.OnGridGenerationComplete -= OnGridGenerationCompleteHandler;
        GridGenerator.OnGridSolveComplete -= OnGridSolveCompleteHandler;
    }

    private void GenerateUIIfRequired()
    {
        if (generatedGridSize != gridSize)
        {
            generatedGridSize = gridSize;
            generatedGridSizeSqr = gridSize * gridSize;

            ClearGrid();
            CreateGrid();
            ResetGuess();
        }
    }

    private void ClearGrid()
    {
        buttonTextComponents.Clear();
        buttonComponents.Clear();

        int gridChildCount = gridLayoutGroup.transform.childCount;
        for (int i = 0; i < gridChildCount; i++)
        {
            Transform child = gridLayoutGroup.transform.GetChild(i);
            Destroy(child.gameObject);
        }
    }

    private void CreateGrid()
    {
        gridLayoutGroup.constraintCount = gridSize;

        for (int y = 0; y < gridSize; y++)
        {
            for (int x = 0; x < gridSize; x++)
            {
                int index = (y * gridSize) + x;
                GameObject newLetterButton = Instantiate(letterButtonPrefab);
                newLetterButton.name = "Grid " + x + "," + y;
                newLetterButton.transform.SetParent(gridLayoutGroup.transform, false);
                newLetterButton.transform.localScale = Vector3.one;

                TMPro.TextMeshProUGUI textComponent = newLetterButton.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                textComponent.fontSize = 50f;
                textComponent.text = null;

                buttonTextComponents.Add(textComponent);

                Button b = newLetterButton.GetComponent<Button>();
                b.onClick.AddListener(() => ButtonTapped(index));

                buttonComponents.Add(b);
            }
        }
    }

    private void ResetButtonStates()
    {
        generatedGridButtonStates = new ButtonState[generatedGridSizeSqr];

        int count = gridSize * gridSize;
        for (int i = 0; i < count; i++)
        {
            UpdateButton(i, ButtonState.Normal);
        }
    }

    //  Generation

    public void Generate()
    {
        ResetButtonStates();

        GenerateUIIfRequired();

        gridGenerator.gridSize = gridSize;
        gridGenerator.Generate();
    }

    private void OnGridGenerationCompleteHandler(char[] gridChars)
    {
        Debug.Log("OnGridGenerationComplete");

        generatedGridChars = gridChars;

        // Set up letters on buttons
        for (int i = 0; i < generatedGridChars.Length; i++)
        {
            char gridChar = gridChars[i];
            buttonTextComponents[i].text = gridChar.ToString();
        }

        if (autoSolve)
        {
            Solve();
        }
    }

    // Interaction

    public void ButtonTapped(int index)
    {
        Debug.Log("Tapped " + generatedGridChars[index]);

        ButtonState currentStateAtIndex = generatedGridButtonStates[index];

        if (GuessInProgress)
        {
            gridGenerator.IndexToGrid(index, out int x, out int y);
            gridGenerator.IndexToGrid(lastClickedIndex, out int lastX, out int lastY);

            int deltaX = Math.Abs(lastX - x);
            int deltaY = Math.Abs(lastY - y);

            Debug.Log($"clicked {x},{y} - deltas {deltaX},{deltaY}");

            bool validMove = false;
            if (diagonalMovesAllowed)
            {
                validMove = deltaX < 2 && deltaY < 2;
            }
            else
            {
                validMove = (deltaX == 0 && deltaY == 1) || (deltaY == 0 && deltaX == 1);
            }

            if (!validMove)
            {
                Debug.Log("Illegal move");
                ResetGuess();
                return;
            }

        }

        lastClickedIndex = index;

        switch (currentStateAtIndex)
        {
            case ButtonState.Normal:
                UpdateButton(index, ButtonState.Selected);
                char gridChar = generatedGridChars[index];
                AddCharacterToGuess(gridChar);
                break;
            case ButtonState.Selected:
                UpdateButton(index, ButtonState.Normal);
                if (GuessInProgress) { ResetGuess(); }

                break;
            default:
                Debug.Log("??");
                break;
        }
    }

    private void UpdateButton(int index, ButtonState state)
    {
        Button button = buttonComponents[index];
        Image buttonImage = button.GetComponent<Image>();
        buttonImage.color = (state == ButtonState.Selected) ? new Color(.9f, .8f, .8f, 1f) : new Color(1f, 1f, 1f, 1f);
        generatedGridButtonStates[index] = state;
    }

    public void Solve()
    {
        gridGenerator.Solve(diagonalMovesAllowed);
    }

    private Dictionary<string, TMPro.TextMeshProUGUI> availbleWordsDictionary = new Dictionary<string, TMPro.TextMeshProUGUI>();

    public void PopulateAvailableWords(List<StringBuilder> availableWords)
    {
        availbleWordsDictionary.Clear();

        int availableWordsContainerChildCount = availableWordsContainer.transform.childCount;
        for (int i = 0; i < availableWordsContainerChildCount; i++)
        {
            Transform child = availableWordsContainer.transform.GetChild(i);
            Destroy(child.gameObject);
        }

        foreach (var availableWord in availableWords)
        {
            string availableWordString = availableWord.ToString();

            if (!availbleWordsDictionary.ContainsKey(availableWordString))
            {
                GameObject newAvailableWord = Instantiate(availableWordTextPrefab);
                newAvailableWord.name = "Word: " + availableWordString;
                newAvailableWord.transform.SetParent(availableWordsContainer.transform, false);
                newAvailableWord.transform.localScale = Vector3.one;

                var newAvailableWordTextComponent = newAvailableWord.GetComponent<TMPro.TextMeshProUGUI>();
                newAvailableWordTextComponent.text = availableWordString;

                availbleWordsDictionary.Add(availableWordString, newAvailableWordTextComponent);
            }

        }
    }

    private void OnGridSolveCompleteHandler(List<StringBuilder> availableWords)
    {
        if (availableWords.Count > minimumWordCount)
        {
            PopulateAvailableWords(availableWords);
            consecutiveGenerationCount = 0;
        }
        else
        {
            consecutiveGenerationCount++;
            if (consecutiveGenerationCount < consecutiveGenerationCountLimit)
            {
                Debug.Log("Failed to generate a grid with {minimumWordCount} words. Trying again...");
                Generate();
            }
            else
            {
                Debug.LogError("Failed to generate a grid with {minimumWordCount} words within {consecutiveGenerationCountLimit} attempts");
            }
        }
    }

    private bool GuessInProgress => currentGuess.Length > 0;

    private void ResetGuess()
    {
        // Debug.Log($"Reset Guess");

        currentGuess = "";
        currentWord.text = "";

        lastClickedIndex = -1;

        ResetButtonStates();
    }

    private void AddCharacterToGuess(char guessChar)
    {
        currentGuess += guessChar;
        currentWord.text = currentGuess;

        // Debug.Log($"curentGuess={currentGuess}");
        if (availbleWordsDictionary.ContainsKey(currentGuess))
        {
            Debug.Log($"Found word {currentGuess}");
            availbleWordsDictionary[currentGuess].color = Color.red;
            ResetGuess();
        }

    }

}
