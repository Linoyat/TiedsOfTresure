using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grid : MonoBehaviour
{
    public enum PieceType
    {
        EMPTY,
        NORMAL,
        BUBBLE,
        ROW_CLEAR,
        COLUMN_CLEAR,
        RAINBOW,
        COUNT
    };

    [System.Serializable]
    public struct PiecePrefab
    {
        public PieceType type;
        public GameObject prefab;
    }

    [System.Serializable]
    public struct PiecePosition
    {
        public PieceType type;
        public int x;
        public int y;
    }

    private Dictionary<PieceType, GameObject> piecePrefabDict;
    private GamePiece[,] pieces;
    private bool inverse = false;
    private GamePiece pressedPiece;
    private GamePiece enteredPiece;
    private bool gameOver = false;
    private bool hammerMode = false;

    public int xDim;
    public int yDim;
    public float fillTime;
    public Level level;

    public PiecePrefab[] piecePrefabs;
    public GameObject backgroundPrefab;
    public PiecePosition[] initialPieces;

    // Bomb Booster System
    [Header("Bomb Booster System")]
    public int matchesOf5PlusThisLevel = 0; 
    public int requiredMatchesForBombBooster = 5; 
    private bool bombBoosterMode = false; 

    private bool isFilling = false;
    public bool IsFilling
    {
        get { return isFilling; }
    }

    private void Awake()
    {
        piecePrefabDict = new Dictionary<PieceType, GameObject>();

        for (int i = 0; i < piecePrefabs.Length; i++)
        {
            if (!piecePrefabDict.ContainsKey(piecePrefabs[i].type))
            {
                piecePrefabDict.Add(piecePrefabs[i].type, piecePrefabs[i].prefab);
            }
        }

        for (int x = 0; x < xDim; x++)
        {
            for (int y = 0; y < yDim; y++)
            {
                GameObject background = (GameObject)Instantiate(backgroundPrefab, GetWorldPosition(x, y), Quaternion.identity);
                background.transform.parent = transform;
            }
        }

        pieces = new GamePiece[xDim, yDim];

        for (int i = 0; i < initialPieces.Length; i++)
        {
            if (initialPieces[i].x >= 0 && initialPieces[i].x < xDim
                && initialPieces[i].y >= 0 && initialPieces[i].y < yDim)
            {
                SpawnNewPiece(initialPieces[i].x, initialPieces[i].y, initialPieces[i].type);
            }
        }

        for (int x = 0; x < xDim; x++)
        {
            for (int y = 0; y < yDim; y++)
            {
                if (pieces[x, y] == null)
                {
                    SpawnNewPiece(x, y, PieceType.EMPTY);
                }
            }
        }

        StartCoroutine(Fill());
    }

    public GamePiece SpawnNewPiece(int x, int y, PieceType type)
    {
        GameObject newPiece = (GameObject)Instantiate(piecePrefabDict[type], GetWorldPosition(x, y), Quaternion.identity);
        newPiece.transform.parent = transform;
        pieces[x, y] = newPiece.GetComponent<GamePiece>();
        pieces[x, y].Init(x, y, this, type);

        var sr = newPiece.GetComponent<SpriteRenderer>();
        var gp = pieces[x, y];
        if (type == PieceType.NORMAL && gp.ColorComponent != null && sr != null && sr.sprite == null)
        {
            gp.ColorComponent.SetColor(
                (ColorPiece.ColorType)Random.Range(0, gp.ColorComponent.NumColors)
            );
        }

        return pieces[x, y];
    }

    void Update()
    {
    }

    public IEnumerator Fill()
    {
        bool needsRefill = true;
        isFilling = true;

        while (needsRefill)
        {
            yield return new WaitForSeconds(fillTime);
            while (FillStep())
            {
                inverse = !inverse;
                yield return new WaitForSeconds(fillTime);
            }
            needsRefill = ClearAllValidMatches();
        }
        isFilling = false;
    }

    public bool FillStep()
    {
        bool movedPiece = false;

        for (int y = yDim - 2; y >= 0; y--)
        {
            for (int loopX = 0; loopX < xDim; loopX++)
            {
                int x = inverse ? xDim - 1 - loopX : loopX;

                GamePiece piece = pieces[x, y];

                if (piece.IsMovable())
                {
                    GamePiece pieceBelow = pieces[x, y + 1];

                    if (pieceBelow.Type == PieceType.EMPTY)
                    {
                        Destroy(pieceBelow.gameObject);
                        piece.MovableComponent.Move(x, y + 1, fillTime);
                        pieces[x, y + 1] = piece;
                        SpawnNewPiece(x, y, PieceType.EMPTY);
                        movedPiece = true;
                    }
                    else
                    {
                        for (int diag = -1; diag <= 1; diag++)
                        {
                            if (diag != 0)
                            {
                                int diagX = inverse ? x - diag : x + diag;

                                if (diagX >= 0 && diagX < xDim)
                                {
                                    GamePiece diagonalPiece = pieces[diagX, y + 1];

                                    if (diagonalPiece.Type == PieceType.EMPTY)
                                    {
                                        bool hasPieceAbove = true;

                                        for (int aboveY = y; aboveY >= 0; aboveY--)
                                        {
                                            if (pieces[diagX, aboveY].Type != PieceType.EMPTY)
                                            {
                                                hasPieceAbove = false;
                                                break;
                                            }
                                        }

                                        if (!hasPieceAbove)
                                        {
                                            Destroy(diagonalPiece.gameObject);
                                            piece.MovableComponent.Move(diagX, y + 1, fillTime);
                                            pieces[diagX, y + 1] = piece;
                                            SpawnNewPiece(x, y, PieceType.EMPTY);
                                            movedPiece = true;
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        for (int x = 0; x < xDim; x++)
        {
            GamePiece pieceBelow = pieces[x, 0];

            if (pieceBelow.Type == PieceType.EMPTY)
            {
                Destroy(pieceBelow.gameObject);
                GameObject newPiece = (GameObject)Instantiate(piecePrefabDict[PieceType.NORMAL], GetWorldPosition(x, -1), Quaternion.identity);
                newPiece.transform.parent = transform;

                pieces[x, 0] = newPiece.GetComponent<GamePiece>();
                pieces[x, 0].Init(x, -1, this, PieceType.NORMAL);
                pieces[x, 0].MovableComponent.Move(x, 0, fillTime);
                pieces[x, 0].ColorComponent.SetColor((ColorPiece.ColorType)Random.Range(0, pieces[x, 0].ColorComponent.NumColors));
                movedPiece = true;
            }
        }

        return movedPiece;
    }

    public Vector2 GetWorldPosition(int x, int y)
    {
        return new Vector2(
            x - xDim / 2f + 0.5f,
            yDim / 2f - 0.5f - y
        );
    }

    public bool IsAdjacent(GamePiece piece1, GamePiece piece2)
    {
        if (piece1 == null || piece2 == null) return false;
        return (piece1.X == piece2.X && Mathf.Abs(piece1.Y - piece2.Y) == 1)
            || (piece1.Y == piece2.Y && Mathf.Abs(piece1.X - piece2.X) == 1);
    }

    public void SwapPieces(GamePiece piece1, GamePiece piece2)
    {
        if (gameOver)
            return;

        if (piece1.IsMovable() && piece2.IsMovable())
        {
            pieces[piece1.X, piece1.Y] = piece2;
            pieces[piece2.X, piece2.Y] = piece1;

            if (GetMatch(piece1, piece2.X, piece2.Y) != null || GetMatch(piece2, piece1.X, piece1.Y) != null
                || piece1.Type == PieceType.RAINBOW || piece2.Type == PieceType.RAINBOW)
            {
                int piece1X = piece1.X;
                int piece1Y = piece1.Y;

                piece1.MovableComponent.Move(piece2.X, piece2.Y, fillTime);
                piece2.MovableComponent.Move(piece1X, piece1Y, fillTime);

                if (piece1.Type == PieceType.RAINBOW && piece1.IsClearable() && piece2.IsColored())
                {
                    ClearColorPiece clearColor = piece1.GetComponent<ClearColorPiece>();
                    if (clearColor)
                        clearColor.Color = piece2.ColorComponent.Color;

                    ClearPiece(piece1.X, piece1.Y);
                }

                if (piece2.Type == PieceType.RAINBOW && piece2.IsClearable() && piece1.IsColored())
                {
                    ClearColorPiece clearColor = piece2.GetComponent<ClearColorPiece>();
                    if (clearColor)
                        clearColor.Color = piece1.ColorComponent.Color;

                    ClearPiece(piece2.X, piece2.Y);
                }

                if (piece1.Type == PieceType.ROW_CLEAR || piece1.Type == PieceType.COLUMN_CLEAR)
                {
                    ClearPiece(piece1.X, piece1.Y);
                }

                if (piece2.Type == PieceType.ROW_CLEAR || piece2.Type == PieceType.COLUMN_CLEAR)
                {
                    ClearPiece(piece2.X, piece2.Y);
                }

                pressedPiece = null;
                enteredPiece = null;

                ClearAllValidMatches();
                StartCoroutine(Fill());
                level.OnMove();
            }
            else
            {
                pieces[piece1.X, piece1.Y] = piece1;
                pieces[piece2.X, piece2.Y] = piece2;
            }
        }
    }

    public void PressPiece(GamePiece piece)
    {
        pressedPiece = piece;
    }

    public void EnterPiece(GamePiece piece)
    {
        enteredPiece = piece;
    }

    public void ReleasePiece()
    {
        if (hammerMode && pressedPiece != null)
        {
            ClearPiece(pressedPiece.X, pressedPiece.Y);
            ClearAllValidMatches();
            StartCoroutine(Fill());
            level.OnMove();

            hammerMode = false;
            pressedPiece = null;
        }
        else if (bombBoosterMode && pressedPiece != null)
        {
            // Use bomb booster at selected position
            ExplodeBombAt(pressedPiece.X, pressedPiece.Y);
            ClearAllValidMatches();
            StartCoroutine(Fill());
            level.OnMove();

            bombBoosterMode = false;
            pressedPiece = null;
        }
        else if (IsAdjacent(pressedPiece, enteredPiece))
        {
            SwapPieces(pressedPiece, enteredPiece);
        }
    }

    public List<GamePiece> GetMatch(GamePiece piece, int newX, int newY)
    {
        if (piece.IsColored())
        {
            ColorPiece.ColorType color = piece.ColorComponent.Color;
            List<GamePiece> horizontalPieces = new List<GamePiece>();
            List<GamePiece> verticalPieces = new List<GamePiece>();
            List<GamePiece> matchingPieces = new List<GamePiece>();

            // First check horizontal
            horizontalPieces.Add(piece);

            for (int dir = 0; dir <= 1; dir++)
            {
                for (int xOffset = 1; xOffset < xDim; xOffset++)
                {
                    int x = (dir == 0) ? newX - xOffset : newX + xOffset;

                    if (x < 0 || x >= xDim)
                        break;

                    if (pieces[x, newY].IsColored() && pieces[x, newY].ColorComponent.Color == color)
                        horizontalPieces.Add(pieces[x, newY]);
                    else
                        break;
                }
            }

            if (horizontalPieces.Count >= 3)
                matchingPieces.AddRange(horizontalPieces);

            // Traverse vertically if we found a horizontal match
            if (horizontalPieces.Count >= 3)
            {
                List<GamePiece> newVerticals = new List<GamePiece>();

                for (int i = 0; i < horizontalPieces.Count; i++)
                {
                    for (int dir = 0; dir <= 1; dir++)
                    {
                        for (int yOffset = 1; yOffset < yDim; yOffset++)
                        {
                            int y = (dir == 0) ? newY - yOffset : newY + yOffset;

                            if (y < 0 || y >= yDim)
                                break;

                            if (pieces[horizontalPieces[i].X, y].IsColored() &&
                                pieces[horizontalPieces[i].X, y].ColorComponent.Color == color)
                            {
                                newVerticals.Add(pieces[horizontalPieces[i].X, y]);
                            }
                            else
                                break;
                        }
                    }
                }

                if (newVerticals.Count >= 2)
                {
                    verticalPieces.AddRange(newVerticals);
                    matchingPieces.AddRange(verticalPieces);
                }
            }

            if (matchingPieces.Count >= 3)
                return matchingPieces;

            // Now check vertical
            horizontalPieces.Clear();
            verticalPieces.Clear();
            verticalPieces.Add(piece);

            for (int dir = 0; dir <= 1; dir++)
            {
                for (int yOffset = 1; yOffset < yDim; yOffset++)
                {
                    int y = (dir == 0) ? newY - yOffset : newY + yOffset;

                    if (y < 0 || y >= yDim)
                        break;

                    if (pieces[newX, y].IsColored() && pieces[newX, y].ColorComponent.Color == color)
                        verticalPieces.Add(pieces[newX, y]);
                    else
                        break;
                }
            }

            if (verticalPieces.Count >= 3)
                matchingPieces.AddRange(verticalPieces);

            // Traverse horizontally if we found a vertical match
            if (verticalPieces.Count >= 3)
            {
                List<GamePiece> newHorizontals = new List<GamePiece>();

                for (int i = 0; i < verticalPieces.Count; i++)
                {
                    for (int dir = 0; dir <= 1; dir++)
                    {
                        for (int xOffset = 1; xOffset < xDim; xOffset++)
                        {
                            int x = (dir == 0) ? newX - xOffset : newX + xOffset;

                            if (x < 0 || x >= xDim)
                                break;

                            if (pieces[x, verticalPieces[i].Y].IsColored() &&
                                pieces[x, verticalPieces[i].Y].ColorComponent.Color == color)
                            {
                                newHorizontals.Add(pieces[x, verticalPieces[i].Y]);
                            }
                            else
                                break;
                        }
                    }
                }

                if (newHorizontals.Count >= 2)
                {
                    horizontalPieces.AddRange(newHorizontals);
                    matchingPieces.AddRange(horizontalPieces);
                }
            }

            if (matchingPieces.Count >= 3)
                return matchingPieces;
        }

        return null;
    }

    // Check for matches and award bomb boosters when appropriate
    public bool ClearAllValidMatches()
    {
        bool needsRefill = false;

        for (int y = 0; y < yDim; y++)
        {
            for (int x = 0; x < xDim; x++)
            {
                if (pieces[x, y].IsClearable())
                {
                    List<GamePiece> match = GetMatch(pieces[x, y], x, y);

                    if (match != null)
                    {
                        // Track matches of 5+ for bomb booster system
                        if (match.Count >= 5)
                        {
                            matchesOf5PlusThisLevel++;
                            Debug.Log("Match of " + match.Count + "! Total 5+ matches this level: " + matchesOf5PlusThisLevel);

                            // Check if player earned a bomb booster
                            CheckAndAwardBombBooster();
                        }

                        PieceType specialPieceType = PieceType.COUNT;
                        GamePiece randomPiece = match[Random.Range(0, match.Count)];

                        int specialPieceX = randomPiece.X;
                        int specialPieceY = randomPiece.Y;

                        if (match.Count == 4)
                        {
                            if (pressedPiece == null || enteredPiece == null)
                            {
                                specialPieceType = (PieceType)Random.Range((int)PieceType.ROW_CLEAR, (int)PieceType.COLUMN_CLEAR);
                            }
                            else if (pressedPiece.Y == enteredPiece.Y)
                            {
                                specialPieceType = PieceType.ROW_CLEAR;
                            }
                            else
                            {
                                specialPieceType = PieceType.COLUMN_CLEAR;
                            }
                        }
                        else if (match.Count >= 6)
                        {
                            specialPieceType = PieceType.RAINBOW;
                        }

                        for (int i = 0; i < match.Count; i++)
                        {
                            if (ClearPiece(match[i].X, match[i].Y))
                            {
                                needsRefill = true;

                                if (match[i] == pressedPiece || match[i] == enteredPiece)
                                {
                                    specialPieceX = match[i].X;
                                    specialPieceY = match[i].Y;

                                }
                                if (specialPieceType != PieceType.COUNT)
                                {
                                    Destroy(pieces[specialPieceX, specialPieceY].gameObject);

                                    GamePiece newPiece = SpawnNewPiece(specialPieceX, specialPieceY, specialPieceType);

                                    if ((specialPieceType == PieceType.ROW_CLEAR || specialPieceType == PieceType.COLUMN_CLEAR)
                                        && newPiece.IsColored() && match[0].IsColored())
                                    {
                                        newPiece.ColorComponent.SetColor(match[0].ColorComponent.Color);
                                    }
                                    else if (specialPieceType == PieceType.RAINBOW && newPiece.IsColored())
                                    {
                                        newPiece.ColorComponent.SetColor(ColorPiece.ColorType.ANY);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        return needsRefill;
    }

    // Check and award bomb booster when requirements are met
    private void CheckAndAwardBombBooster()
    {
        // Award bomb booster every 5 matches of 5+
        if (matchesOf5PlusThisLevel >= requiredMatchesForBombBooster)
        {
            BombBoosterManager.AddBombBooster();
            Debug.Log("Bomb Booster Earned! Total bomb boosters: " + BombBoosterManager.GetBombBoosterCount());

            // Notify HUD that a booster was earned
            if (level != null && level.hud != null)
            {
                level.hud.OnBombBoosterEarned();
            }

            // Reset counter for next booster
            matchesOf5PlusThisLevel = 0;
        }
    }

    // Enable bomb booster selection mode
    public void EnableBombBoosterMode()
    {
        bombBoosterMode = true;
        hammerMode = false; // Disable other modes
        Debug.Log("Bomb Booster Mode Enabled - Click on the board to place bomb!");
    }

    // Explode bomb at specified position
    private void ExplodeBombAt(int centerX, int centerY)
    {
        Debug.Log("Exploding bomb at: " + centerX + ", " + centerY);

        // Clear center piece
        ClearPiece(centerX, centerY);

        // Clear 6 surrounding pieces in circular pattern
        int[] bombPatternX = { 0, 0, 1, -1, 1, -1 }; // Relative to center
        int[] bombPatternY = { 1, -1, 0, 0, 1, -1 }; // Relative to center

        for (int i = 0; i < bombPatternX.Length; i++)
        {
            int bombX = centerX + bombPatternX[i];
            int bombY = centerY + bombPatternY[i];

            // Check if position is within board boundaries
            if (bombX >= 0 && bombX < xDim && bombY >= 0 && bombY < yDim)
            {
                ClearPiece(bombX, bombY);
            }
        }
    }

    public bool ClearPiece(int x, int y)
    {
        if (pieces[x, y].IsClearable() && !pieces[x, y].ClearableComponent.IsBeingCleared)
        {
            pieces[x, y].ClearableComponent.Clear();
            SpawnNewPiece(x, y, PieceType.EMPTY);
            ClearObstacles(x, y);

            return true;
        }

        return false;
    }

    public void ClearObstacles(int x, int y)
    {
        for (int adjacentX = x - 1; adjacentX <= x + 1; adjacentX++)
        {
            if (adjacentX != x && adjacentX >= 0 && adjacentX < xDim)
            {
                if (pieces[adjacentX, y].Type == PieceType.BUBBLE && pieces[adjacentX, y].IsClearable())
                {
                    pieces[adjacentX, y].ClearableComponent.Clear();
                    SpawnNewPiece(adjacentX, y, PieceType.EMPTY);
                }
            }
        }

        for (int adjacentY = y - 1; adjacentY <= y + 1; adjacentY++)
        {
            if (adjacentY != y && adjacentY >= 0 && adjacentY < yDim)
            {
                if (pieces[x, adjacentY].Type == PieceType.BUBBLE && pieces[x, adjacentY].IsClearable())
                {
                    pieces[x, adjacentY].ClearableComponent.Clear();
                    SpawnNewPiece(x, adjacentY, PieceType.EMPTY);
                }
            }
        }
    }

    public void ClearRow(int row)
    {
        for (int x = 0; x < xDim; x++)
            ClearPiece(x, row);
    }

    public void ClearColumn(int column)
    {
        for (int y = 0; y < yDim; y++)
            ClearPiece(column, y);
    }

    public void ClearColor(ColorPiece.ColorType color)
    {
        for (int x = 0; x < xDim; x++)
        {
            for (int y = 0; y < yDim; y++)
            {
                if (pieces[x, y].IsColored() && (pieces[x, y].ColorComponent.Color == color || color == ColorPiece.ColorType.ANY))
                {
                    ClearPiece(x, y);
                }
            }
        }
    }

    public void GameOver()
    {
        gameOver = true;
    }

    public List<GamePiece> GetPiecesOfType(PieceType type)
    {
        List<GamePiece> piecesOfType = new List<GamePiece>();

        for (int x = 0; x < xDim; x++)
            for (int y = 0; y < yDim; y++)
                if (pieces[x, y].Type == type)
                    piecesOfType.Add(pieces[x, y]);

        return piecesOfType;
    }

    public void EnableHammerMode()
    {
        hammerMode = true;
    }
}