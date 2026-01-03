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