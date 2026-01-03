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
// var board = Board.LogicSolve(startingValues, sections, xPerSection);
//var board = Board.Create(4, sections, xPerSection);

var board = Board.CreateRandom(8);
if (board is not null)
{
    board.Print();
}
else
{
    Console.WriteLine("Cannot solve!");
}