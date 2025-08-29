// =============================
// Form1.cs
// =============================
using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace SudokuGame
{
    public partial class Form1 : Form
    {
        private TextBox[,] cells = new TextBox[9, 9];
        private int[,] solution = new int[9, 9];   // 完整解
        private int[,] puzzle = new int[9, 9];     // 題目 (挖空後)
        private bool[,] isGiven = new bool[9, 9];  // 是否為題目給定
        private int mistakeCount = 0;
        private TextBox selectedCell = null;
        private System.Windows.Forms.Timer gameTimer;
        private int elapsedSeconds = 0;
        private Label lblTimer;
        private Label statusLabel;
        private Button newGameBtn;
        private ComboBox difficultyBox;
        private Label countsLabel;
        private static readonly Random rng = new Random();

        public Form1()
        {
            Text = "Sudoku (WinForms)";
            Width = 660;
            Height = 660;
            InitControls();
            NewGame();
            


        }

        private void InitControls()
        {
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 1,
            };
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 90));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 10));
            Controls.Add(root);

            // 盤面
            var board = new TableLayoutPanel
            {
                RowCount = 9,
                ColumnCount = 9,
                Dock = DockStyle.Fill,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.Single
            };
            for (int i = 0; i < 9; i++)
            {
                board.RowStyles.Add(new RowStyle(SizeType.Percent, 11.11f));
                board.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 11.11f));
            }

            root.Controls.Add(board, 0, 0);

            for (int r = 0; r < 9; r++)
            {
                for (int c = 0; c < 9; c++)
                {
                    var tb = new TextBox
                    {
                        Dock = DockStyle.Fill,
                        Font = new Font("Consolas", 22, FontStyle.Bold),
                        TextAlign = HorizontalAlignment.Center,
                        MaxLength = 1,
                        BorderStyle = BorderStyle.FixedSingle
                    };
                    tb.Tag = new Point(r, c);
                    tb.MouseClick += Cell_MouseClick;
                    tb.MouseEnter += Cell_MouseEnter;   // 滑入高亮
                    tb.MouseLeave += Cell_MouseLeave;   // 還原顏色
                    tb.KeyPress += Cell_KeyPress;       // 鍵盤輸入

                    cells[r, c] = tb;
                    board.Controls.Add(tb, c, r);
                }
            }

            // 底部工具列
            var bottom = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(8, 8, 8, 8)
            };

            newGameBtn = new Button
            {
                Text = "新局",
                AutoSize = true
            };
            newGameBtn.Click += (s, e) => NewGame();

            difficultyBox = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 120
            };
            difficultyBox.Items.AddRange(new object[] { "簡單", "普通", "困難" });
            difficultyBox.SelectedIndex = 1; // 預設普通
            countsLabel = new Label
            {
                AutoSize = true,
                Text = "剩餘：1:9 2:9 3:9 4:9 5:9 6:9 7:9 8:9 9:9",
                Margin = new Padding(16, 10, 0, 0)
            };
            bottom.Controls.Add(countsLabel);
            statusLabel = new Label
            {
                AutoSize = true,
                Text = "錯誤：0/3",
                Margin = new Padding(12, 10, 0, 0)
            };

            bottom.Controls.Add(newGameBtn);
            bottom.Controls.Add(new Label { Text = "難度：", AutoSize = true, Margin = new Padding(16, 10, 0, 0) });
            bottom.Controls.Add(difficultyBox);
            bottom.Controls.Add(statusLabel);
            root.Controls.Add(bottom, 0, 1);
        }
        private void InitTimer()
        {
            // 確保遊戲開始時，計時器被初始化且不重複創建
            if (gameTimer == null)
            {
                gameTimer = new System.Windows.Forms.Timer();
                gameTimer.Interval = 1000; // 每秒
                gameTimer.Tick += (s, e) =>
                {
                    elapsedSeconds++;
                    lblTimer.Text = $"時間：{elapsedSeconds / 60:D2}:{elapsedSeconds % 60:D2}";
                };
            }
            
            // 停止並清除舊的計時器，避免重複啟動
            if (gameTimer.Enabled)
            {
                gameTimer.Stop();
            }
            if (lblTimer == null)
            {
                lblTimer = new Label { Text = "時間：00:00", Dock = DockStyle.Top, Font = new Font("Consolas", 14, FontStyle.Bold), Height = 30, TextAlign = ContentAlignment.MiddleCenter };
            }
            // 重新初始化計時器的時間
            elapsedSeconds = 0;
            lblTimer.Text = "時間：00:00";
            gameTimer.Start(); // 重新開始計時器

            // 確保計時器只顯示一次
            if (!this.Controls.Contains(lblTimer))
            {
                this.Controls.Add(lblTimer);
            }
        }
        private void UpdateCounts()
        {
            int[] counts = new int[10]; // 統計 1~9 出現次數

            for (int r = 0; r < 9; r++)
            {
                for (int c = 0; c < 9; c++)
                {
                    if (int.TryParse(cells[r, c].Text, out int v) && v >= 1 && v <= 9)
                    {
                        counts[v]++;
                    }
                }
            }

            // 每個數字應該最多出現 9 次
            string text = "剩餘：";
            for (int n = 1; n <= 9; n++)
            {
                int remain = 9 - counts[n];
                text += $"{n}:{remain} ";
            }
            countsLabel.Text = text.Trim();
        }

        private void NewGame()
        {
            mistakeCount = 0;
            statusLabel.Text = "錯誤：0/3";
            InitTimer();
            // 1) 生成完整解
            solution = Sudoku.GenerateFullSolution();

            // 2) 依難度挖空且保證唯一解
            int clues = difficultyBox.SelectedIndex switch
            {
                0 => rng.Next(38, 46), // 簡單：較多線索
                2 => rng.Next(24, 30), // 困難：較少線索
                _ => rng.Next(30, 38), // 普通
            };
            puzzle = Sudoku.MakePuzzleWithUniqueSolution(solution, clues);

            // 3) 載入盤面
            LoadPuzzle();
            UpdateCounts();

        }

        private void LoadPuzzle()
        {
            for (int r = 0; r < 9; r++)
            {
                for (int c = 0; c < 9; c++)
                {
                    int v = puzzle[r, c];
                    var tb = cells[r, c];
                    tb.ForeColor = Color.Black;
                    tb.BackColor = GetDefaultCellColor(r, c);

                    if (v != 0)
                    {
                        tb.Text = v.ToString();
                        tb.ReadOnly = true;
                        isGiven[r, c] = true;
                    }
                    else
                    {
                        tb.Text = string.Empty;
                        tb.ReadOnly = false;
                        isGiven[r, c] = false;
                    }
                }
            }
        }

        private static Color GetDefaultCellColor(int r, int c)
        {
            // 3x3 區塊背景微分色，易讀
            bool block = ((r / 3) + (c / 3)) % 2 == 0;
            return block ? Color.FromArgb(245, 0, 0) : Color.FromArgb(0, 245,0 );
        }

        private void Cell_MouseClick(object sender, MouseEventArgs e)
        {
            selectedCell = sender as TextBox;
        }

        private void Cell_MouseEnter(object sender, EventArgs e)
        {
            var tb = sender as TextBox;
            if (tb == null) return;
            if (string.IsNullOrWhiteSpace(tb.Text)) return;

            string num = tb.Text.Trim();
            // 高亮：相同數字全亮黃，其它依原色
            for (int r = 0; r < 9; r++)
            {
                for (int c = 0; c < 9; c++)
                {
                    if (cells[r, c].Text.Trim() == num && num != "")
                        cells[r, c].BackColor = Color.Khaki;
                    else
                        cells[r, c].BackColor = GetDefaultCellColor(r, c);
                }
            }
        }

        private void Cell_MouseLeave(object sender, EventArgs e)
        {
            // 還原背景
            for (int r = 0; r < 9; r++)
                for (int c = 0; c < 9; c++)
                    cells[r, c].BackColor = GetDefaultCellColor(r, c);
        }

        private void Cell_KeyPress(object sender, KeyPressEventArgs e)
        {
            var tb = sender as TextBox;
            if (tb == null) return;
            var p = (Point)tb.Tag;
            int r = p.X, c = p.Y;

            if (isGiven[r, c]) { e.Handled = true; return; } // 題目給定不可改

            if (e.KeyChar == (char)Keys.Back || e.KeyChar == (char)Keys.Delete)
            {
                tb.Text = string.Empty;
                e.Handled = true;
                UpdateCounts();
                return;
            }

            if (!char.IsDigit(e.KeyChar) || e.KeyChar == '0')
            {
                e.Handled = true; // 只允許 1-9
                return;
            }

            int num = e.KeyChar - '0';
            e.Handled = true; // 我們自行處理輸入

            // 判斷正確與否
            if (solution[r, c] == num)
            {
                tb.Text = num.ToString();
                tb.ForeColor = Color.Black;

                if (IsBoardCompleted())
                {
                    gameTimer.Stop();
                    MessageBox.Show($"恭喜完成！耗時 {elapsedSeconds / 60:D2}:{elapsedSeconds % 60:D2}");
                }
                UpdateCounts();

            }
            else
            {
                tb.Text = num.ToString();
                tb.ForeColor = Color.Red;
                mistakeCount++;
                statusLabel.Text = $"錯誤：{mistakeCount}/3";

                if (mistakeCount >= 3)
                {
                    RevealSolutionAsFailure();
                    gameTimer.Stop();
                    MessageBox.Show($"遊戲失敗！錯誤超過三次！用時 {elapsedSeconds / 60:D2}:{elapsedSeconds % 60:D2}");
                }
            }

        }

        private bool IsBoardCompleted()
        {
            for (int r = 0; r < 9; r++)
                for (int c = 0; c < 9; c++)
                {
                    if (string.IsNullOrWhiteSpace(cells[r, c].Text)) return false;
                    if (!int.TryParse(cells[r, c].Text, out int v)) return false;
                    if (v != solution[r, c]) return false;
                }
            return true;
        }

        private void RevealSolutionAsFailure()
        {
            for (int r = 0; r < 9; r++)
            {
                for (int c = 0; c < 9; c++)
                {
                    cells[r, c].Text = solution[r, c].ToString();
                    if (!isGiven[r, c])
                        cells[r, c].ForeColor = Color.Red; // 題目中原本要填的顯示紅色
                }
            }
        }
    }

    /// <summary>
    /// 數獨生成/驗證核心
    /// </summary>
    public static class Sudoku
    {
        private static readonly Random rng = new Random();

        public static int[,] GenerateFullSolution()
        {
            var board = new int[9, 9];
            SolveBacktracking(board, 0, 0, randomize: true);
            return board;
        }

        public static int[,] MakePuzzleWithUniqueSolution(int[,] full, int cluesTarget)
        {
            int[,] puzzle = (int[,])full.Clone();

            // 位置順序隨機化
            var idx = Enumerable.Range(0, 81).OrderBy(_ => rng.Next()).ToList();

            // 儘量挖到剩下 cluesTarget 個線索，過程中保持唯一解
            int clues = 81;
            foreach (int k in idx)
            {
                if (clues <= cluesTarget) break;

                int r = k / 9, c = k % 9;
                int saved = puzzle[r, c];
                puzzle[r, c] = 0;

                if (!HasUniqueSolution(puzzle))
                {
                    // 不唯一，撤回
                    puzzle[r, c] = saved;
                }
                else
                {
                    clues--;
                }
            }

            return puzzle;
        }

        public static bool HasUniqueSolution(int[,] puzzle)
        {
            int count = 0;
            // 用一個計數求解器，找到兩個解就提前停止
            CountSolutions((int[,])puzzle.Clone(), 0, 0, ref count, 2);
            return count == 1;
        }

        private static bool SolveBacktracking(int[,] board, int r, int c, bool randomize)
        {
            if (r == 9) return true;          // 完成
            if (c == 9) return SolveBacktracking(board, r + 1, 0, randomize);
            if (board[r, c] != 0) return SolveBacktracking(board, r, c + 1, randomize);

            var nums = Enumerable.Range(1, 9).ToList();
            if (randomize)
                Shuffle(nums);

            foreach (int v in nums)
            {
                if (IsSafe(board, r, c, v))
                {
                    board[r, c] = v;
                    if (SolveBacktracking(board, r, c + 1, randomize))
                        return true;
                    board[r, c] = 0;
                }
            }
            return false;
        }

        private static void CountSolutions(int[,] board, int r, int c, ref int count, int limit)
        {
            if (count >= limit) return;       // 早停
            if (r == 9) { count++; return; }
            if (c == 9) { CountSolutions(board, r + 1, 0, ref count, limit); return; }
            if (board[r, c] != 0) { CountSolutions(board, r, c + 1, ref count, limit); return; }

            // 簡單啟發式：嘗試候選數較少者 (此處就就地計算)
            var candidates = GetCandidates(board, r, c);
            foreach (int v in candidates)
            {
                board[r, c] = v;
                CountSolutions(board, r, c + 1, ref count, limit);
                if (count >= limit) { board[r, c] = 0; return; }
                board[r, c] = 0;
            }
        }

        private static List<int> GetCandidates(int[,] board, int r, int c)
        {
            bool[] used = new bool[10];
            for (int i = 0; i < 9; i++)
            {
                used[board[r, i]] = true;
                used[board[i, c]] = true;
            }
            int br = (r / 3) * 3, bc = (c / 3) * 3;
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                    used[board[br + i, bc + j]] = true;

            var list = new List<int>();
            for (int v = 1; v <= 9; v++) if (!used[v]) list.Add(v);
            // 隨機化以增加多樣性
            Shuffle(list);
            return list;
        }

        private static bool IsSafe(int[,] board, int r, int c, int v)
        {
            for (int i = 0; i < 9; i++)
                if (board[r, i] == v || board[i, c] == v)
                    return false;

            int br = (r / 3) * 3, bc = (c / 3) * 3;
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                    if (board[br + i, bc + j] == v)
                        return false;
            return true;
        }

        private static void Shuffle<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
