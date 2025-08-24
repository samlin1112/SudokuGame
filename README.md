# Sudoku Game (C# WinForms)

## Overview

This is a fully functional Sudoku game developed in C# using WinForms. The game features a randomly generated puzzle with a unique solution and interactive gameplay with both mouse and keyboard.

## Features

* **Random Sudoku Puzzle Generation:** Each game provides a unique puzzle.
* **Mouse & Keyboard Input:** Click a cell to select and type 1-9 to fill.
* **Highlight Matching Numbers:** Hover over a number to highlight all matching numbers.
* **Mistake Limit:** Game ends after 3 incorrect entries, revealing the solution in red.
* **Timer:** Tracks time spent solving the puzzle.
* **Remaining Numbers Counter:** Shows how many of each number are left.
* **Win Detection:** Displays a message when the puzzle is correctly solved.

## Getting Started

### Prerequisites

* Visual Studio 2019 or later
* .NET Framework 4.7.2 or compatible

### Installation

1. Clone the repository
2. Open the solution file `SudokuGame.sln` in Visual Studio.
3. Build and run the project.

## How to Play

1. Select a cell by clicking on it.
2. Type a number from 1 to 9 using your keyboard.
3. Hover over any filled number to see all identical numbers highlighted.
4. If you make 3 mistakes, the game will end and show the correct solution.
5. The timer tracks how long you take to solve the puzzle.
6. Monitor the remaining numbers counter at the bottom.

## File Structure

* `Program.cs` - Entry point of the application.
* `Form1.cs` - Main game form and logic.
* `SudokuGenerator.cs` - Sudoku puzzle generation logic.
