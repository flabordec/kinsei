using System.Globalization;
using System.Security.Cryptography.X509Certificates;

public record CellIndex(int I, int J);

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


public class Board
{
    private static readonly Rule[] RulesForSolving = [
        new ThreeInARowRule(),
        new NoSquares(),
        new MaxPerRowColumn(),
        new CountPerSection(),
    ];
    private static readonly Rule[] RulesForCreating = [
        new ThreeInARowRule(),
        new NoSquares(),
        new MaxPerRowColumn(),
    ];

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

    private Board(string[] startingValues, int[,] sections, int[] xPerSection, Rule[] rules)
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

        for (int i = 0; i < Size; i++)
        {
            for (int j = 0; j < Size; j++)
            {
                Console.Write($"{Sections[i, j]:000} ");
            }
            Console.WriteLine();
        }
        Console.WriteLine(new string('-', (Size * 2) - 1));

        for (int i = 0; i < XPerSection.Length; i++)
        {
            Console.WriteLine($"Section {i}: Xs={XPerSection[i]}, Os={OPerSection[i]}, Total={SectionCount[i]}");
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

    private bool InnerBruteForceSolveForX(int i, int j, Random random)
    {
        if (this[i, j].CanX)
        {
            int value = this[i, j].Value;
            this[i, j].SetX();
            var canBuild = InnerBruteForceSolve(i + 1, j, random);
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
        return false;
    }

    private bool InnerBruteForceSolveForO(int i, int j, Random random)
    {
        if (this[i, j].CanO)
        {
            int value = this[i, j].Value;
            this[i, j].SetO();
            var canBuild = InnerBruteForceSolve(i + 1, j, random);
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
        return false;
    }

    private bool InnerBruteForceSolve(int i, int j, Random random)
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
            return InnerBruteForceSolve(i + 1, j, random);
        }
        else
        {
            ApplyRules();
            if (random.NextDouble() < 0.5)
            {
                bool canBuild;
                canBuild = InnerBruteForceSolveForX(i, j, random);
                if (canBuild)
                    return true;
                canBuild = InnerBruteForceSolveForO(i, j, random);
                if (canBuild)
                    return true;
            }
            else
            {

                bool canBuild;
                canBuild = InnerBruteForceSolveForO(i, j, random);
                if (canBuild)
                    return true;
                canBuild = InnerBruteForceSolveForX(i, j, random);
                if (canBuild)
                    return true;
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

    public static Board? BruteForceSolve(string[] startingValues, int[,] sections, int[] xPerSection)
    {
        Board board = new Board(startingValues, sections, xPerSection, RulesForSolving);
        bool solved = board.InnerBruteForceSolve(0, 0, new Random(0));
        return solved ? board : null;
    }

    public static Board? LogicSolve(string[] startingValues, int[,] sections, int[] xPerSection)
    {
        Board board = new Board(startingValues, sections, xPerSection, RulesForSolving);
        bool solved = board.InnerLogicSolve();
        return solved ? board : null;
    }

    private static void FillRandomSections(Random random, int[,] sections, int sectionCount, int maxValuesPerSection)
    {
        int size = sections.GetLength(0);

        int[] counts = new int[sectionCount];
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                sections[i, j] = -1;
            }
        }

        var seen = new HashSet<CellIndex>();
        for (int i = 0; i < sectionCount; i++)
        {
            while (true)
            {
                int sectionI = random.Next(size);
                int sectionJ = random.Next(size);
                var sectionIx = new CellIndex(sectionI, sectionJ);
                if (seen.Add(sectionIx))
                {
                    sections[sectionI, sectionJ] = i;
                    break;
                }
            }
        }

        bool done = false;
        bool anyProgress = false;
        while (!done)
        {
            done = true;
            anyProgress = false;
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    if (sections[i, j] == -1)
                    {
                        done = false;
                        (int, int)[] deltas = [
                            (-1, 0),
                            (1, 0),
                            (0, -1),
                            (0, 1),
                        ];
                        random.Shuffle(deltas);

                        for (int deltaIx = 0; deltaIx < deltas.Length; deltaIx++)
                        {
                            int newI = i + deltas[deltaIx].Item1;
                            int newJ = j + deltas[deltaIx].Item2;

                            bool inBounds = newI >= 0 && newI < size && newJ >= 0 && newJ < size;
                            if (inBounds && sections[newI, newJ] != -1)
                            {
                                if (counts[sections[newI, newJ]] < maxValuesPerSection || !anyProgress)
                                {
                                    int section = sections[newI, newJ];
                                    sections[i, j] = section;
                                    counts[section]++;
                                    anyProgress = true;
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    public static Board? CreateRandom(int size, int? seed = null)
    {
        Random random = seed != null ? new Random(seed.Value) : new Random();

        int sectionCount = random.Next(size + size / 2, size * 2 + size / 2);
        int maxValuesPerSection = (size * size) / sectionCount;
        int[,] sections = new int[size, size];
        int[] xPerSection = new int[sectionCount];

        FillRandomSections(random, sections, sectionCount, maxValuesPerSection);

        string[] startingValues = new string[size];
        for (int i = 0; i < size; i++)
        {
            startingValues[i] = new string('.', size);
        }
        Board board = new Board(startingValues, sections, xPerSection, RulesForCreating);

        bool solved = board.InnerBruteForceSolve(0, 0, random);
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                if (board.Cells[i, j].IsX)
                {
                    xPerSection[sections[i, j]]++;
                }
            }
        }

        return solved ? board : null;
    }
}
