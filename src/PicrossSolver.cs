using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace PicrossSolver
{
    public class PicrossSolver : Form
    {
        //===================================================================== CONSTANTS
        private const int GRID_SIZE = 20;
        private const int MARGIN = 12;

        private readonly Color COL_TEXT = Color.DarkSlateGray;
        private readonly Color COL_BORDER = Color.Navy;
        private readonly Color COL_SEPARATOR5 = Color.SteelBlue;
        private readonly Color COL_SEPARATOR1 = Color.SkyBlue;
        private readonly Color COL_EMPTY1 = Color.WhiteSmoke;
        private readonly Color COL_EMPTY2 = Color.LightSkyBlue;
        private readonly Color COL_TRUE1 = Color.CornflowerBlue;
        private readonly Color COL_TRUE2 = Color.RoyalBlue;
        private readonly Color COL_FALSE = Color.PaleVioletRed;

        //===================================================================== VARIABLES
        private Button _btnLoad = new Button();
        private Button _btnSolve = new Button();
        private ListBox _lstData = new ListBox();
        private Panel _pnlPuzzle = new Panel();

        private long _solveTime;
        private Puzzle _puzzle;
        //===================================================================== INITIALIZE
        public PicrossSolver()
        {
            this.ClientSize = new Size(350, 250);

            _btnLoad.Location = new Point(MARGIN, MARGIN);
            _btnLoad.Text = "Load";
            _btnLoad.Click += btnLoad_Click;

            _btnSolve.Location = new Point(MARGIN, _btnLoad.Bottom + 6);
            _btnSolve.Text = "Solve";
            _btnSolve.Click += btnSolve_Click;

            _lstData.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Bottom;
            _lstData.HorizontalScrollbar = true;
            _lstData.IntegralHeight = false;
            _lstData.Location = new Point(MARGIN, _btnSolve.Bottom + 6);
            _lstData.Size = new Size(_btnLoad.Width, ClientSize.Height - _btnSolve.Bottom - 6 - MARGIN);
            foreach (string path in Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*data*.txt"))
                _lstData.Items.Add(Path.GetFileName(path));

            _pnlPuzzle.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Bottom;
            _pnlPuzzle.BackColor = Color.WhiteSmoke;
            _pnlPuzzle.BorderStyle = BorderStyle.FixedSingle;
            _pnlPuzzle.Location = new Point(_btnLoad.Right + 6, MARGIN);
            _pnlPuzzle.Size = new Size(ClientSize.Width - _btnLoad.Right - 6 - MARGIN, ClientSize.Height - MARGIN * 2);
            _pnlPuzzle.Paint += pnlPuzzle_Paint;

            this.DoubleBuffered = true;
            this.Icon = new Icon(Assembly.GetCallingAssembly().GetManifestResourceStream("PicrossSolver.Icon.ico"));
            this.Text = "Picross Solver";

            this.Controls.Add(_btnLoad);
            this.Controls.Add(_btnSolve);
            this.Controls.Add(_lstData);
            this.Controls.Add(_pnlPuzzle);
        }

        //===================================================================== PROPERTIES
        private int GridLeft
        {
            get
            {
                if (_puzzle != null) return _puzzle.MaxClueCountY * GRID_SIZE + GRID_SIZE;
                else return 0;
            }
        }
        private int GridTop
        {
            get
            {
                if (_puzzle != null) return _puzzle.MaxClueCountX * GRID_SIZE + GRID_SIZE;
                else return 0;
            }
        }
        private int GridRight
        {
            get
            {
                if (_puzzle != null) return GridLeft + GridWidth;
                else return 0;
            }
        }
        private int GridBottom
        {
            get
            {
                if (_puzzle != null) return GridTop + GridHeight;
                else return 0;
            }
        }
        private int GridWidth
        {
            get
            {
                if (_puzzle != null) return _puzzle.Width * GRID_SIZE;
                else return 0;
            }
        }
        private int GridHeight
        {
            get
            {
                if (_puzzle != null) return _puzzle.Height * GRID_SIZE;
                else return 0;
            }
        }

        //===================================================================== EVENTS
        private void pnlPuzzle_Paint(object sender, PaintEventArgs e)
        {
            if (_puzzle == null) return;
            DrawSolveTime(e.Graphics);
            DrawSolution(e.Graphics);
            DrawGridLines(e.Graphics);
            DrawClues(e.Graphics);
        }
        private void DrawSolveTime(Graphics g)
        {
            using (Brush b = new SolidBrush(COL_TEXT))
                g.DrawString(_solveTime.ToString("n0") + " ms", this.Font, b, 5, 5);
        }
        private void DrawSolution(Graphics g)
        {
            Pen pen = new Pen(COL_FALSE, 2);

            for (int y = 0; y < _puzzle.Height; y++)
            {
                for (int x = 0; x < _puzzle.Width; x++)
                {
                    Rectangle rect = new Rectangle(GridLeft + x * GRID_SIZE + 1, GridTop + y * GRID_SIZE + 1, GRID_SIZE - 1, GRID_SIZE - 1);
                    Rectangle rectGradient = new Rectangle(GridLeft + x * GRID_SIZE, GridTop + y * GRID_SIZE, GRID_SIZE, GRID_SIZE);

                    // empty square background
                    using (Brush b = new LinearGradientBrush(rectGradient, COL_EMPTY1, COL_EMPTY2, 45))
                        g.FillRectangle(b, rect);

                    if (_puzzle.GetSolution(x, y) == SolutionState.True)
                    {
                        using (Brush b = new LinearGradientBrush(rectGradient, COL_TRUE1, COL_TRUE2, 45))
                            g.FillRectangle(b, rect);
                    }
                    else if (_puzzle.GetSolution(x, y) == SolutionState.False)
                    {
                        g.DrawLine(pen, rect.X - 1, rect.Y - 1, rect.Right, rect.Bottom);
                        g.DrawLine(pen, rect.Right, rect.Y - 1, rect.X - 1, rect.Bottom);
                    }
                }
            }

            pen.Dispose();
        }
        private void DrawGridLines(Graphics g)
        {
            Pen penSep5 = new Pen(COL_SEPARATOR5);
            Pen penSep1 = new Pen(COL_SEPARATOR1);

            for (int x = 1; x < _puzzle.Width; x++)
            {
                Point p1 = new Point(GridLeft + x * GRID_SIZE, GridTop);
                Point p2 = new Point(p1.X, GridBottom);
                g.DrawLine(x % 5 == 0 ? penSep5 : penSep1, p1, p2);
            }

            for (int y = 1; y < _puzzle.Height; y++)
            {
                Point p1 = new Point(GridLeft, GridTop + y * GRID_SIZE);
                Point p2 = new Point(GridRight, p1.Y);
                g.DrawLine(y % 5 == 0 ? penSep5 : penSep1, p1, p2);
            }

            using (Pen p = new Pen(COL_BORDER))
                g.DrawRectangle(p, GridLeft, GridTop, GridWidth, GridHeight);
        }
        private void DrawClues(Graphics g)
        {
            Brush brushStr = new SolidBrush(COL_TEXT);

            Rectangle rectGradientX = new Rectangle(0, GRID_SIZE - 1, 1, _puzzle.MaxClueCountX * GRID_SIZE + 1);
            Brush brush = new LinearGradientBrush(rectGradientX, Color.WhiteSmoke, Color.PaleTurquoise, 90);

            for (int x = 0; x < _puzzle.Height; x++)
            {
                int[] clues = _puzzle.GetClue(x, true);
                Rectangle rect = new Rectangle(GridLeft + x * GRID_SIZE + 1, GridTop - clues.Length * GRID_SIZE, GRID_SIZE - 1, clues.Length * GRID_SIZE);
                g.FillRectangle(brush, rect);

                for (int clueID = 0; clueID < clues.Length; clueID++)
                {
                    int clue = clues[clueID];
                    int stringWidth = (int)g.MeasureString(clue.ToString(), this.Font).Width;
                    int stringHeight = (int)g.MeasureString(clue.ToString(), this.Font).Height;
                    int drawX = GridLeft + x * GRID_SIZE + (GRID_SIZE - stringWidth) / 2;
                    int drawY = GridTop - (clues.Length - clueID) * GRID_SIZE + (GRID_SIZE - stringHeight) / 2;
                    g.DrawString(clue.ToString(), this.Font, brushStr, drawX, drawY);
                }
            }
            brush.Dispose();

            Rectangle rectGradientY = new Rectangle(GRID_SIZE - 1, 0, _puzzle.MaxClueCountY * GRID_SIZE + 1, 1);
            brush = new LinearGradientBrush(rectGradientY, Color.WhiteSmoke, Color.PaleGreen, 0.0);

            for (int y = 0; y < _puzzle.Height; y++)
            {
                int[] clues = _puzzle.GetClue(y, false);
                Rectangle rect = new Rectangle(GridLeft - clues.Length * GRID_SIZE, GridTop + y * GRID_SIZE + 1, clues.Length * GRID_SIZE, GRID_SIZE - 1);
                g.FillRectangle(brush, rect);

                for (int clueID = 0; clueID < clues.Length; clueID++)
                {
                    int clue = clues[clueID];
                    int stringWidth = (int)g.MeasureString(clue.ToString(), this.Font).Width;
                    int stringHeight = (int)g.MeasureString(clue.ToString(), this.Font).Height;
                    int drawX = GridLeft - (clues.Length - clueID) * GRID_SIZE + (GRID_SIZE - stringWidth) / 2;
                    int drawY = GridTop + y * GRID_SIZE + (GRID_SIZE - stringHeight) / 2;
                    g.DrawString(clue.ToString(), this.Font, brushStr, drawX, drawY);
                }
            }
            brush.Dispose();

            brushStr.Dispose();
        }

        private void btnLoad_Click(object sender, EventArgs e)
        {
            if (_lstData.SelectedIndex != -1)
            {
                _solveTime = 0;
                _puzzle = new Puzzle(File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + _lstData.SelectedItem.ToString()));
                _pnlPuzzle.Invalidate();
                this.Width = GridRight + _pnlPuzzle.Left + 50;
                this.Height = GridBottom + _pnlPuzzle.Top + 70;
            }
        }
        private void btnSolve_Click(object sender, EventArgs e)
        {
            if (_puzzle != null)
            {
                System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
                watch.Start();
                _puzzle.Solve();
                watch.Stop();
                _solveTime = watch.ElapsedMilliseconds;

                _pnlPuzzle.Invalidate(new Rectangle(0, 0, _pnlPuzzle.Width, GRID_SIZE)); // for solve time
                _pnlPuzzle.Invalidate(new Rectangle(GridLeft, GridTop, GridRight, GridBottom));
            }
        }
    }
}
