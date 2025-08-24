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
        private int[,] solution = new int[9, 9];   // �����
        private int[,] puzzle = new int[9, 9];     // �D�� (���ū�)
        private bool[,] isGiven = new bool[9, 9];  // �O�_���D�ص��w
        private int mistakeCount = 0;
        private TextBox selectedCell = null;
        private System.Windows.Forms.Timer gameTimer;
        private int elapsedSeconds = 0;
        private Label lblTimer;
        private Label statusLabel;
        private Button newGameBtn;
        private ComboBox difficultyBox;

        private static readonly Random rng = new Random();

        public Form1()
        {
            Text = "Sudoku (WinForms)";
            Width = 560;
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

            // �L��
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
                    tb.MouseEnter += Cell_MouseEnter;   // �ƤJ���G
                    tb.MouseLeave += Cell_MouseLeave;   // �٭��C��
                    tb.KeyPress += Cell_KeyPress;       // ��L��J

                    cells[r, c] = tb;
                    board.Controls.Add(tb, c, r);
                }
            }

            // �����u��C
            var bottom = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(8, 8, 8, 8)
            };

            newGameBtn = new Button
            {
                Text = "�s��",
                AutoSize = true
            };
            newGameBtn.Click += (s, e) => NewGame();

            difficultyBox = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 120
            };
            difficultyBox.Items.AddRange(new object[] { "²��", "���q", "�x��" });
            difficultyBox.SelectedIndex = 1; // �w�]���q

            statusLabel = new Label
            {
                AutoSize = true,
                Text = "���~�G0/3",
                Margin = new Padding(12, 10, 0, 0)
            };

            bottom.Controls.Add(newGameBtn);
            bottom.Controls.Add(new Label { Text = "���סG", AutoSize = true, Margin = new Padding(16, 10, 0, 0) });
            bottom.Controls.Add(difficultyBox);
            bottom.Controls.Add(statusLabel);
            root.Controls.Add(bottom, 0, 1);
        }
        private void InitTimer()
        {
            // �T�O�C���}�l�ɡA�p�ɾ��Q��l�ƥB�����ƳЫ�
            if (gameTimer == null)
            {
                gameTimer = new System.Windows.Forms.Timer();
                gameTimer.Interval = 1000; // �C��
                gameTimer.Tick += (s, e) =>
                {
                    elapsedSeconds++;
                    lblTimer.Text = $"�ɶ��G{elapsedSeconds / 60:D2}:{elapsedSeconds % 60:D2}";
                };
            }
            
            // ����òM���ª��p�ɾ��A�קK���ƱҰ�
            if (gameTimer.Enabled)
            {
                gameTimer.Stop();
            }
            if (lblTimer == null)
            {
                lblTimer = new Label { Text = "�ɶ��G00:00", Dock = DockStyle.Top, Font = new Font("Consolas", 14, FontStyle.Bold), Height = 30, TextAlign = ContentAlignment.MiddleCenter };
            }
            // ���s��l�ƭp�ɾ����ɶ�
            elapsedSeconds = 0;
            lblTimer.Text = "�ɶ��G00:00";
            gameTimer.Start(); // ���s�}�l�p�ɾ�

            // �T�O�p�ɾ��u��ܤ@��
            if (!this.Controls.Contains(lblTimer))
            {
                this.Controls.Add(lblTimer);
            }
        }

        private void NewGame()
        {
            mistakeCount = 0;
            statusLabel.Text = "���~�G0/3";
            InitTimer();
            // 1) �ͦ������
            solution = Sudoku.GenerateFullSolution();

            // 2) �����׫��ťB�O�Ұߤ@��
            int clues = difficultyBox.SelectedIndex switch
            {
                0 => rng.Next(38, 46), // ²��G���h�u��
                2 => rng.Next(24, 30), // �x���G���ֽu��
                _ => rng.Next(30, 38), // ���q
            };
            puzzle = Sudoku.MakePuzzleWithUniqueSolution(solution, clues);

            // 3) ���J�L��
            LoadPuzzle();
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
            // 3x3 �϶��I���L����A��Ū
            bool block = ((r / 3) + (c / 3)) % 2 == 0;
            return block ? Color.White : Color.FromArgb(245, 245, 245);
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
            // ���G�G�ۦP�Ʀr���G���A�䥦�̭��
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
            // �٭�I��
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

            if (isGiven[r, c]) { e.Handled = true; return; } // �D�ص��w���i��

            if (e.KeyChar == (char)Keys.Back || e.KeyChar == (char)Keys.Delete)
            {
                tb.Text = string.Empty;
                e.Handled = true;
                return;
            }

            if (!char.IsDigit(e.KeyChar) || e.KeyChar == '0')
            {
                e.Handled = true; // �u���\ 1-9
                return;
            }

            int num = e.KeyChar - '0';
            e.Handled = true; // �ڭ̦ۦ�B�z��J

            // �P�_���T�P�_
            if (solution[r, c] == num)
            {
                tb.Text = num.ToString();
                tb.ForeColor = Color.Black;

                if (IsBoardCompleted())
                {
                    gameTimer.Stop();
                    MessageBox.Show($"���ߧ����I�Ӯ� {elapsedSeconds / 60:D2}:{elapsedSeconds % 60:D2}");
                }
            }
            else
            {
                tb.Text = num.ToString();
                tb.ForeColor = Color.Red;
                mistakeCount++;
                statusLabel.Text = $"���~�G{mistakeCount}/3";

                if (mistakeCount >= 3)
                {
                    RevealSolutionAsFailure();
                    gameTimer.Stop();
                    MessageBox.Show($"�C�����ѡI���~�W�L�T���I�ή� {elapsedSeconds / 60:D2}:{elapsedSeconds % 60:D2}");
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
                        cells[r, c].ForeColor = Color.Red; // �D�ؤ��쥻�n����ܬ���
                }
            }
        }
    }

    /// <summary>
    /// �ƿW�ͦ�/���Ү֤�
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

            // ��m�����H����
            var idx = Enumerable.Range(0, 81).OrderBy(_ => rng.Next()).ToList();

            // ���q����ѤU cluesTarget �ӽu���A�L�{���O���ߤ@��
            int clues = 81;
            foreach (int k in idx)
            {
                if (clues <= cluesTarget) break;

                int r = k / 9, c = k % 9;
                int saved = puzzle[r, c];
                puzzle[r, c] = 0;

                if (!HasUniqueSolution(puzzle))
                {
                    // ���ߤ@�A�M�^
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
            // �Τ@�ӭp�ƨD�Ѿ��A����ӸѴN���e����
            CountSolutions((int[,])puzzle.Clone(), 0, 0, ref count, 2);
            return count == 1;
        }

        private static bool SolveBacktracking(int[,] board, int r, int c, bool randomize)
        {
            if (r == 9) return true;          // ����
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
            if (count >= limit) return;       // ����
            if (r == 9) { count++; return; }
            if (c == 9) { CountSolutions(board, r + 1, 0, ref count, limit); return; }
            if (board[r, c] != 0) { CountSolutions(board, r, c + 1, ref count, limit); return; }

            // ²��ҵo���G���խԿ�Ƹ��֪� (���B�N�N�a�p��)
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
            // �H���ƥH�W�[�h�˩�
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
