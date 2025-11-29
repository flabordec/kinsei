using System.Diagnostics.Contracts;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

string[] startingValues =
{
    "x.o...",
    "......",
    "x.....",
    ".....x",
    "......",
    "..x...",
};
int[,] sections = {
    { 0, 0, 1, 1, 1, 2},
    { 0, 0, 1, 1, 3, 2},
    { 5, 5, 5, 6, 3, 4},
    { 5, 7, 6, 6, 3, 4},
    { 7, 7, 8, 8, 10, 10},
    { 9, 9, 8, 8, 10, 10},
}
;
int[] xPerSection = [2, 2, 1, 2, 1, 2, 2, 2, 1, 1, 2];
//var board = Board.BruteForceSolve(startingValues, sections, xPerSection);
var board = Board.LogicSolve(startingValues, sections, xPerSection);
//var board = Board.Create(4, sections, xPerSection);
if (board is not null)
{
    board.Print();
}
else
{
    Console.WriteLine("Cannot solve!");
}

public class Board
{
    public CellState[,] Cells { get; }
    public Rule[] Rules { get; }

    public int[,] Sections { get; }
    public int[] XPerSection { get; }
    public int[] OPerSection { get; }
    public int[] SectionCount { get; }
    public int Size => Cells.GetLength(1);

    public CellState this[int i, int j]
    {
        get
        {
            if (i >= 0 && i < Size && j >= 0 && j < Size)
                return Cells[i, j];
            else
                return CellState.CreateOutOfBounds(-1);
        }
    }

    public int GetXPerSection(int sectionIndex)
    {
        if (sectionIndex >= 0)
        {
            return XPerSection[sectionIndex];
        }
        else
        {
            return 0;
        }
    }

    public int GetOPerSection(int sectionIndex)
    {
        if (sectionIndex >= 0)
        {
            return OPerSection[sectionIndex];
        }
        else
        {
            return 0;
        }
    }

    public Board(string[] startingValues, int[,] sections, int[] xPerSection)
    {
        int size = startingValues.GetLength(0);
        int stringSize = (
            from x in startingValues
            select x.Length
            )
            .Distinct()
            .SingleOrDefault();

        if (
            startingValues.GetLength(0) != size || stringSize != size ||
            sections.GetLength(0) != size || sections.GetLength(1) != size)
        {
            throw new ArgumentException("Grid must be square and sections and starting values must match size");
        }
        if ((size & 1) != 0)
        {
            throw new ArgumentException("The size must be perfectly divisible by 2");
        }

        int[] sectionCount = new int[xPerSection.Length];

        var cells = new CellState[size, size];
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                cells[i, j] = CellState.CreateDefault(sections[i, j]);
                if (startingValues[i][j] == 'x')
                {
                    cells[i, j].SetX();
                }
                else if (startingValues[i][j] == 'o')
                {
                    cells[i, j].SetO();
                }
                else if (startingValues[i][j] != '.')
                {
                    throw new ArgumentException("Invalid character in board");
                }
                sectionCount[sections[i, j]]++;
            }
        }

        var rules = new Rule[]
        {
            new ThreeInARowRule(),
            new NoSquares(),
            new MaxPerRowColumn(),
            new CountPerSection(),
        };
        Cells = cells;
        Rules = rules;

        SectionCount = sectionCount;
        XPerSection = xPerSection;
        OPerSection = new int[sectionCount.Length];
        for (int i = 0; i < OPerSection.Length; i++)
        {
            OPerSection[i] = SectionCount[i] - XPerSection[i];
        }

        Sections = sections;
    }

    public void Print()
    {
        Console.WriteLine(new string('-', (Size * 2) - 1));
        for (int i = 0; i < Size; i++)
        {
            for (int j = 0; j < Size; j++)
            {
                if (Cells[i, j].IsX)
                {
                    Console.Write("X");
                }
                else if (Cells[i, j].IsO)
                {
                    Console.Write("O");
                }
                else if (Cells[i, j].CanX && Cells[i, j].CanO)
                {
                    Console.Write(".");
                }
                else if (Cells[i, j].CanX || Cells[i, j].CanO)
                {
                    Console.Write("?");
                }
                else
                {
                    Console.Write("!");
                }
                Console.Write(" ");
            }
            Console.WriteLine();
        }
        Console.WriteLine(new string('-', (Size * 2) - 1));
        Console.WriteLine();
    }

    private void ApplyRules()
    {
        for (int i = 0; i < Size; i++)
        {
            for (int j = 0; j < Size; j++)
            {
                this[i, j].ResetState();
            }
        }

        foreach (var rule in Rules)
        {
            rule.Initialize(this);
        }

        for (int i = 0; i < Size; i++)
        {
            for (int j = 0; j < Size; j++)
            {
                if (!this[i, j].IsX && !this[i, j].IsO)
                {
                    foreach (var rule in Rules)
                    {
                        rule.UpdateCellState(this, i, j);
                    }
                }
            }
        }

        Print();
    }

    private bool InnerBruteForceSolve(int i, int j)
    {
        if (i == Size)
        {
            j++;
            i = 0;
        }
        if (j == Size)
        {
            return true;
        }

        if (this[i, j].IsX || this[i, j].IsO)
        {
            return InnerBruteForceSolve(i + 1, j);
        }
        else
        {
            ApplyRules();
            if (this[i, j].CanX)
            {
                int value = this[i, j].Value;
                this[i, j].SetX();
                var canBuild = InnerBruteForceSolve(i + 1, j);
                if (canBuild)
                {
                    return true;
                }
                else
                {
                    this[i, j].Value = value;
                    ApplyRules();
                }
            }
            if (this[i, j].CanO)
            {
                int value = this[i, j].Value;
                this[i, j].SetO();
                var canBuild = InnerBruteForceSolve(i + 1, j);
                if (canBuild)
                {
                    return true;
                }
                else
                {
                    this[i, j].Value = value;
                    ApplyRules();
                }
            }
        }
        return false;
    }

    private bool InnerLogicSolve()
    {
        int totalSpaces = Size * Size;
        int spacesLeft = 0;
        for (int i = 0; i < Size; i++)
        {
            for (int j = 0; j < Size; j++)
            {
                if (!this[i, j].IsX && !this[i, j].IsO)
                    spacesLeft++;
            }
        }

        while (true)
        {
            if (spacesLeft == 0)
            {
                return true;
            }

            bool solvedAny = false;
            ApplyRules();
            for (int i = 0; i < Size; i++)
            {
                for (int j = 0; j < Size; j++)
                {
                    if (this[i, j].IsX || this[i, j].IsO)
                        continue;

                    if (this[i, j].CanX && !this[i, j].CanO)
                    {
                        this[i, j].SetX();
                        spacesLeft--;
                        solvedAny = true;
                    }
                    if (this[i, j].CanO && !this[i, j].CanX)
                    {
                        this[i, j].SetO();
                        spacesLeft--;
                        solvedAny = true;
                    }
                }
            }
            if (!solvedAny)
            {
                return false;
            }
        }
    }

    public static Board? Create(int[,] sections, int[] xPerSection)
    {
        int size = sections.GetLength(0);
        string[] startingValues = new string[size];
        for (int i = 0; i < size; i++)
        {
            startingValues[i] = new string('.', size);
        }
        Board board = new Board(startingValues, sections, xPerSection);
        bool solved = board.InnerBruteForceSolve(0, 0);
        return solved ? board : null;
    }

    public static Board? BruteForceSolve(string[] startingValues, int[,] sections, int[] xPerSection)
    {
        Board board = new Board(startingValues, sections, xPerSection);
        bool solved = board.InnerBruteForceSolve(0, 0);
        return solved ? board : null;
    }

    public static Board? LogicSolve(string[] startingValues, int[,] sections, int[] xPerSection)
    {
        Board board = new Board(startingValues, sections, xPerSection);
        bool solved = board.InnerLogicSolve();
        return solved ? board : null;
    }
}

public class CellState
{
    public static CellState CreateOutOfBounds(int section) => new CellState(0, section);
    public static CellState CreateDefault(int section) => new CellState(0b11, section);

    private readonly int _section;
    public int Section => _section;

    private CellState(int value, int section)
    {
        _value = value;
        _section = section;
    }

    int _value;
    public bool CanX => (_value & 0b0001) != 0;
    public bool CanO => (_value & 0b0010) != 0;
    public bool IsX => (_value & 0b0100) != 0;
    public bool IsO => (_value & 0b1000) != 0;

    public void NoXIfTrue(bool test)
    {
        if (test)
            _value = _value & ~0b01;
    }
    public void NoOIfTrue(bool test)
    {
        if (test)
            _value = _value & ~0b10;
    }
    public void ResetState()
    {
        _value = _value | 0b11;
    }
    public void SetX() => _value = 0b0101;
    public void SetO() => _value = 0b1010;
    public int Value
    {
        get => _value;
        set => _value = value;
    }
}

public abstract class Rule
{
    public virtual void Initialize(Board board)
    {

    }
    public abstract void UpdateCellState(Board board, int i, int j);
}

public class NoSquares : Rule
{
    //    -1  0   1
    // -1
    //  0
    //  1

    public override void UpdateCellState(Board board, int i, int j)
    {
        board[i, j].NoXIfTrue(board[i - 1, j - 1].IsX && board[i - 1, j].IsX && board[i, j - 1].IsX);
        board[i, j].NoXIfTrue(board[i + 1, j - 1].IsX && board[i + 1, j].IsX && board[i, j - 1].IsX);
        board[i, j].NoXIfTrue(board[i - 1, j + 1].IsX && board[i - 1, j].IsX && board[i, j + 1].IsX);
        board[i, j].NoXIfTrue(board[i - 1, j + 1].IsX && board[i - 1, j].IsX && board[i, j + 1].IsX);

        board[i, j].NoOIfTrue(board[i - 1, j - 1].IsO && board[i - 1, j].IsO && board[i, j - 1].IsO);
        board[i, j].NoOIfTrue(board[i + 1, j - 1].IsO && board[i + 1, j].IsO && board[i, j - 1].IsO);
        board[i, j].NoOIfTrue(board[i - 1, j + 1].IsO && board[i - 1, j].IsO && board[i, j + 1].IsO);
        board[i, j].NoOIfTrue(board[i - 1, j + 1].IsO && board[i - 1, j].IsO && board[i, j + 1].IsO);
    }
}

public class ThreeInARowRule : Rule
{
    public override void UpdateCellState(Board board, int i, int j)
    {
        board[i, j].NoXIfTrue(board[i - 1, j].IsX && board[i - 2, j].IsX);
        board[i, j].NoOIfTrue(board[i - 1, j].IsO && board[i - 2, j].IsO);

        board[i, j].NoXIfTrue(board[i - 1, j].IsX && board[i + 1, j].IsX);
        board[i, j].NoOIfTrue(board[i - 1, j].IsO && board[i + 1, j].IsO);

        board[i, j].NoXIfTrue(board[i + 1, j].IsX && board[i + 2, j].IsX);
        board[i, j].NoOIfTrue(board[i + 1, j].IsO && board[i + 2, j].IsO);

        board[i, j].NoXIfTrue(board[i, j - 1].IsX && board[i, j - 2].IsX);
        board[i, j].NoOIfTrue(board[i, j - 1].IsO && board[i, j - 2].IsO);

        board[i, j].NoXIfTrue(board[i, j - 1].IsX && board[i, j + 1].IsX);
        board[i, j].NoOIfTrue(board[i, j - 1].IsO && board[i, j + 1].IsO);

        board[i, j].NoXIfTrue(board[i, j + 1].IsX && board[i, j + 2].IsX);
        board[i, j].NoOIfTrue(board[i, j + 1].IsO && board[i, j + 2].IsO);

    }
}

public class MaxPerRowColumn : Rule
{
    public override void UpdateCellState(Board board, int i, int j)
    {
        int xCount, oCount;

        xCount = 0;
        oCount = 0;
        for (int x = 0; x < board.Size; x++)
        {
            if (board[x, j].IsX)
                xCount++;
            if (board[x, j].IsO)
                oCount++;
        }

        board[i, j].NoXIfTrue(xCount >= board.Size / 2);
        board[i, j].NoOIfTrue(oCount >= board.Size / 2);


        xCount = 0;
        oCount = 0;
        for (int x = 0; x < board.Size; x++)
        {
            if (board[i, x].IsX)
                xCount++;
            if (board[i, x].IsO)
                oCount++;
        }

        board[i, j].NoXIfTrue(xCount >= board.Size / 2);
        board[i, j].NoOIfTrue(oCount >= board.Size / 2);
    }
}

public class CountPerSection : Rule
{
    private int[]? _xPerSection;
    private int[]? _oPerSection;

    public override void Initialize(Board board)
    {
        _xPerSection = new int[board.SectionCount.Length];
        _oPerSection = new int[board.SectionCount.Length];

        for (int i = 0; i < board.Size; i++)
        {
            for (int j = 0; j < board.Size; j++)
            {
                int section = board.Sections[i, j];
                if (board[i, j].IsX)
                    _xPerSection[section]++;
                if (board[i, j].IsO)
                    _oPerSection[section]++;
            }
        }
    }

    public override void UpdateCellState(Board board, int i, int j)
    {
        if (_xPerSection is null || _oPerSection is null)
            throw new NullReferenceException("You must call initialize before update");

        int section = board.Sections[i, j];
        int allowedX = board.GetXPerSection(section);
        int currentX = _xPerSection[section];
        board[i, j].NoXIfTrue(!board[i, j].IsX && currentX >= allowedX);


        int allowedO = board.GetOPerSection(section);
        int currentO = _oPerSection[section];
        board[i, j].NoOIfTrue(!board[i, j].IsO && currentO >= allowedO);
    }
}