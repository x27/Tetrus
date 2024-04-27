using System;
using System.Drawing;
using System.Windows.Forms;

namespace Tetrus
{
    public partial class TetrusForm : Form
    {
        const int BOARD_WIDTH = 12;
        const int BOARD_HEIGHT = 19;
        const int BOARD_SIZE = BOARD_WIDTH * BOARD_HEIGHT;
        const int CELL_SIZE = 40;

        private Timer _timer = new Timer();
        private int _timerTick;
        private double _gravity;

        private byte[] _randomBag = new byte[8];
        private int _randomBagIndex;
        private int _rotatePhase;
        private int _level;
        private int _lineCount;

        private int _figureCol;
        private int _figureRow;

        private byte[] _board = new byte[BOARD_HEIGHT * BOARD_WIDTH];

        private Random _random = new Random(DateTime.Now.Millisecond);

        private SolidBrush _brush = new SolidBrush(Color.LightGray);
        private SolidBrush _brushGameOver = new SolidBrush(Color.Gray);
        private Pen _pen = new Pen(Color.LightGray, 1);
        private Font _font = new Font("Verdana", 20);
        private Font _fontGameOver = new Font("Verdana", 40);

        private bool _isGameOver = false;

        static readonly byte[] _tetraminos = new byte[]
        {
            // I
            04,05,06,07,
            02,06,10,14,
            08,09,10,11,
            01,05,09,13,
            // J
            00,04,05,06,
            01,02,05,09,
            04,05,06,10,
            01,05,08,09,
            // L
            02,04,05,06,
            01,05,09,10,
            04,05,06,08,
            00,01,05,09,
            // O
            01,02,05,06,
            01,02,05,06,
            01,02,05,06,
            01,02,05,06,
            // S
            01,02,04,05,
            01,05,06,10,
            05,06,08,09,
            00,04,05,09,
            // T
            01,04,05,06,
            01,05,06,09,
            04,05,06,09,
            01,04,05,09,
            // Z
            00,01,05,06,
            02,05,06,09,
            04,05,09,10,
            01,04,05,08
        };

        public TetrusForm()
        {
            InitializeComponent();
            DoubleBuffered = true;

            InitGame();

            _timer.Interval = 10;
            _timer.Tick += _timer_Tick;
            _timer.Start();
        }

        private void InitGame()
        {
            FillRandomBag(false);

            // clear board 
            for(var i=0; i<BOARD_SIZE; i++)
                _board[i] = 0;
            // set bottom
            for (var i = 0; i < BOARD_WIDTH; i++)
                _board[BOARD_SIZE - BOARD_WIDTH + i] = 1;
            // set left and right borders
            for (var i = 0; i < BOARD_HEIGHT; i++)
            {
                _board[i * BOARD_WIDTH] = 1;
                _board[i * BOARD_WIDTH + BOARD_WIDTH - 1] = 1;
            }

            _randomBagIndex = 0;
            _level = 1;
            _lineCount = 0;
            _timerTick = 0;

            InitTetramino();
        }

        private void InitTetramino()
        {
            _rotatePhase = 0;
            _figureCol = 4;
            _figureRow = 0;
            _gravity = Math.Pow(0.8 - ((_level - 1) * 0.007), _level - 1);
        }

        /// <summary>
        /// https://tetris.fandom.com/wiki/Random_Generator
        /// </summary>
        private void FillRandomBag(bool needCopyLast = true)
        {
            var count = 0;
            if (needCopyLast)
            {
                _randomBag[0] = _randomBag[7];
                count++;
            }

            while (count < 7)
            {
                var value = _random.Next(7);
                var notFounded = true;
                for (int i = 0; i < count; i++)
                {
                    if (value == _randomBag[i])
                    {
                        notFounded = false;
                        break;
                    }
                }
                if (notFounded)
                    _randomBag[count++] = (byte)value;
            }
            _randomBag[count] = (byte)_random.Next(7);
        }

        private void _timer_Tick(object sender, EventArgs e)
        {
            _timerTick++;

            if (_timerTick >= 100 * _gravity)
            {
                if (!IsBoardValid(_figureCol, _figureRow+1, _rotatePhase))
                {
                    _board = GetBoard(_figureCol, _figureRow, _rotatePhase);

                    _randomBagIndex++;
                    if (_randomBagIndex > 6)
                    {
                        FillRandomBag(true);
                        _randomBagIndex = 0;
                    }

                    _level = _lineCount / 8 + 1;
                    InitTetramino();

                    // check game over
                    if (!IsBoardValid(_figureCol, _figureRow, _rotatePhase))
                    {
                        _timer.Stop();
                        _isGameOver = true;
                        Invalidate();
                        return;
                    }


                    // remove full lines
                    for (var row=1; row<BOARD_HEIGHT-1; row++)
                    {
                        var counter = 0;
                        for (var col = 0; col < BOARD_WIDTH; col++)
                            counter += _board[row * BOARD_WIDTH + col];

                        if (counter == BOARD_WIDTH)
                        {
                            _lineCount++;
                            for(var i=row-1; i>0; i--)
                            {
                                for (var j = 1; j < BOARD_WIDTH - 1; j++)
                                {
                                    _board[(i+1) * BOARD_WIDTH + j] = _board[i * BOARD_WIDTH + j];
                                    _board[i * BOARD_WIDTH + j] = 0;
                                }
                            }
                        }
                    }
                }
                else
                    _figureRow++;

                _timerTick = 0;
                Invalidate();
            }
        }

        private bool IsBoardValid(int col, int row, int rotatePhase)
        {
            var board = GetBoard(col, row, rotatePhase);
            for(var i=0; i<BOARD_SIZE; i++) 
                if (board[i]>1)
                    return false;
            return true;
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            switch (keyData)
            {
                case Keys.Left:
                    if (IsBoardValid(_figureCol-1, _figureRow, _rotatePhase))
                    {
                        _figureCol--;
                        Invalidate();
                    }
                    break;
                case Keys.Right:
                    if (IsBoardValid(_figureCol + 1, _figureRow, _rotatePhase))
                    {
                        _figureCol++;
                        Invalidate();
                    }
                    break;
                case Keys.Up:
                    var rp = (_rotatePhase + 1) % 4;
                    if (IsBoardValid(_figureCol, _figureRow, rp))
                    {
                        _rotatePhase = rp;
                        Invalidate();
                    }
                    break;
                case Keys.Space:
                    _gravity = _gravity / 30;
                    break;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private byte[] GetBoard(int xFigure, int yFigure, int rotatePhase)
        {
            var board = new byte[BOARD_SIZE];
            for (var i = 0; i < BOARD_SIZE; i++)
                board[i] = _board[i];

            // figure
            for (var i = 0; i < 4; i++)
            {
                var f = _tetraminos[16 * _randomBag[_randomBagIndex] + 4 * rotatePhase + i];
                var x = xFigure + (f % 4);
                var y = yFigure + (f / 4);

                board[y * BOARD_WIDTH + x]++;
            }
            return board;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var g = e.Graphics;

            var board = GetBoard(_figureCol, _figureRow, _rotatePhase);

            // draw board
            for (var row = 1; row < BOARD_HEIGHT-1; row++)
            {
                for (var col = 1; col < BOARD_WIDTH - 1; col++)
                if (board[row*BOARD_WIDTH + col] > 0)
                    g.FillRectangle(_brush, (col-1) * CELL_SIZE, (row-1) * CELL_SIZE, CELL_SIZE, CELL_SIZE);
                else
                    g.DrawRectangle(_pen, (col-1) * CELL_SIZE, (row-1) * CELL_SIZE, CELL_SIZE, CELL_SIZE);
            }
            // draw next figure
            var offset = CELL_SIZE * (BOARD_WIDTH + 1);
            for (var i = 0; i < 4; i++)
            {
                var f = _tetraminos[16 * _randomBag[_randomBagIndex + 1] + i];
                g.FillRectangle(_brush, offset + (f%4) * CELL_SIZE, (1+(f/4)) * CELL_SIZE, CELL_SIZE, CELL_SIZE);
            }
            // level
            g.DrawString($"Level {_level}", _font, _brush, offset, 5 * CELL_SIZE);
            g.DrawString($"Lines {_lineCount}", _font, _brush, offset, 12 * CELL_SIZE);

            if (_isGameOver)
            {
                g.DrawString($"Game Over!", _fontGameOver, _brushGameOver, 2 * CELL_SIZE, 7 * CELL_SIZE);
            }
        }
    }
}
