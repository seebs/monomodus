using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace MonoModus;

class Lineart
{
    // from the old Lua version:
    // normally:
    // point 1 moves towards point 3; point 2 is fixed.
    // after point 1 reaches point 3, that location becomes point 2, point 2's
    // location becomes point 1, and a new point 3 is generated.
    // --
    // if move_both:
    // point 1 moves towards point 3; point 2 moves towards point 1's starting
    // location. when point 1 arrives, point 2 is assumed to be Very Close to
    // where point 1 started. So we don't shuffle; we just make a new point 3,
    // and continue
    //
    // Each update, we draw logical P1->P2.

    public Vector2 P1, P2, P3;
    private int _step;
    public int Color;
    public bool MoveBoth;
    private Palette _palette;
    private Multiline _multiline;
    private int _lines; // number of lines to draw from Old to New

    private Modus _game;

    public Lineart(Modus game, int lines, float thickness, int trails, int trailFrames, Palette palette)
    {
        _game = game;
        _palette = palette;
        _lines = lines;
        _multiline = new Multiline(game, _lines, thickness, trails, trailFrames, _palette);
        MoveBoth = true;
    }

    public void LoadContent(GraphicsDevice gd)
    {
        _multiline.LoadContent(gd);
    }

    public bool Update(GameTime gameTime)
    {
        if (_step >= _lines)
        {
            return true;
        }
        float howFar = (float)_step / (float)(_lines);
        Vector2 p0, p1;
        p0 = Vector2.Lerp(P1, P3, howFar);
        if (MoveBoth)
        {
            p1 = Vector2.Lerp(P2, P1, howFar);
        }
        else
        {
            p1 = P2;
        }
        int idx = _step * 2;
        _multiline.Points[idx + 0] = p0;
        _multiline.Points[idx + 1] = p1;
        _multiline.Colors[idx + 0] = Color;
        Color = (Color + 5) % _palette.Size();
        _multiline.Colors[idx + 1] = Color + (_palette.Size() / 6);
        _multiline.Alphas[idx + 0] = 1.0f;
        _multiline.Alphas[idx + 1] = 1.0f;
        _multiline.Update(gameTime);
        _step++;
        if (_step == _lines)
        {
            if (!MoveBoth)
            {
                P1 = P2;
                P2 = P3;
                // and we'll get a new P3 handed in
            }
            else
            {
                // update things to the positions they'd been moving towards
                P2 = P1;
                P1 = P3;
            }
            MoveBoth = !MoveBoth;
        }
        return _step == _lines;
    }

    public void NewTarget(Vector2 p)
    {
        _step = 0;
        P3 = p;
    }

    public void Draw(GameTime gameTime, GraphicsDevice gd)
    {
        _multiline.Draw(gameTime, gd);
    }
}

class Linearts : DrawableGameComponent
{
    private int _count;
    private float _width, _height;
    private Lineart[] _linearts;
    private Random _rng;
    private Palette _palette;
    private Vector2[,] _grid;
    private int _ticker;
    private bool _advance = false;
    private int _current;


    public Linearts(Modus game, int linearts, int points, float thickness, int trails, int trailFrames, Palette palette)
            : base(game)
    {
        _count = linearts;
        _linearts = new Lineart[_count];
        _palette = palette;
        _rng = new Random();
        _ticker = 0;
        _current = 0;
        for (int i = 0; i < _count; i++)
        {
            _linearts[i] = new Lineart(game, points, thickness, trails, trailFrames, _palette);
        }
    }

    private int colinear(int gridPos)
    {
        int row = gridPos / 3;
        int col = gridPos % 3;
        int newRow = row;
        int newCol = col;
        if (col == 1)
        {
            newCol = _rng.Next(0, 2);
            if (newCol >= col)
            {
                newCol++;
            }
        }
        else if (row == 1)
        {
            newRow = _rng.Next(0, 2);
            if (newRow >= row)
            {
                newRow++;
            }
        }
        else
        {
            // we're in a corner. 50-50 we pick a new row,
            // or a new col:
            if (_rng.Next(0, 2) == 1)
            {
                newRow = _rng.Next(0, 2);
                if (newRow >= row)
                {
                    newRow++;
                }
            }
            else
            {
                newCol = _rng.Next(0, 2);
                if (newCol >= col)
                {
                    newCol++;
                }
            }
        }
        return newRow * 3 + newCol;
    }

    private (int, int) gridIndex(int gridPos)
    {
        return (gridPos / 3, gridPos % 3);
    }

    public void advance()
    {
        _advance = true;
    }

    private bool isBoring(int p1, int p2, int p3)
    {
        if (p1 == p2 || p1 == p3 || p2 == p3)
        {
            return true;
        }
        int r1, c1, r2, c2, r3, c3;
        (r1, c1) = (p1 / 3, p1 % 3);
        (r2, c2) = (p2 / 3, p2 % 3);
        (r3, c3) = (p3 / 3, p3 % 3);
        if (r1 == r2 && r2 == r3)
        {
            return true;
        }
        if (c1 == c2 && c2 == c3)
        {
            return true;
        }
        return false;
    }
    private bool isBoring(Vector2 p1, Vector2 p2, int p3idx)
    {
        int r3, c3;
        (r3, c3) = (p3idx / 3, p3idx % 3);
        Vector2 p3 = _grid[r3, c3];

        if (p1 == p2 || p1 == p3 || p2 == p3)
        {
            return true;
        }
        if (p1.X == p2.X && p2.X == p3.X)
        {
            return true;
        }
        if (p1.Y == p2.Y && p2.Y == p3.Y)
        {
            return true;
        }
        return false;
    }

    protected override void LoadContent()
    {
        PresentationParameters pp = GraphicsDevice.PresentationParameters;

        int screenWidth = pp.BackBufferWidth;
        int screenHeight = pp.BackBufferHeight;
        float xScale, yScale;

        bool sideways = screenWidth < screenHeight;

        // we do some initial math in pixels, to try to get a square size
        // which is an integer number of pixels...
        if (sideways)
        {
            xScale = 1.0f;
            yScale = (float)screenWidth / (float)screenHeight;
        }
        else
        {
            yScale = 1.0f;
            xScale = (float)screenHeight / (float)screenWidth;
        }

        _width = xScale;
        _height = yScale;

        _grid = new Vector2[3, 3];
        for (int x = 0; x < 3; x++)
        {
            float xv = (x - 1) * (1 / xScale);
            for (int y = 0; y < 3; y++)
            {
                float yv = (y - 1) * (1 / yScale);
                _grid[x, y] = new Vector2(xv, yv);
            }
        }
        for (int i = 0; i < _count; i++)
        {
            int p1, p2, p3;
            p1 = _rng.Next(0, 8);
            if (p1 >= 4)
            {
                p1++;
            }
            do
            {
                p2 = _rng.Next(0, 8);
                if (p2 >= 4)
                {
                    p2++;
                }
            } while (p2 == p1);
            do
            {
                p3 = _rng.Next(0, 8);
                if (p3 >= 4)
                {
                    p3++;
                }
            } while (isBoring(p1, p2, p3));
            int r, c;
            (r, c) = gridIndex(p1);
            _linearts[i].P1 = _grid[r, c];
            (r, c) = gridIndex(p2);
            _linearts[i].P2 = _grid[r, c];
            (r, c) = gridIndex(p3);
            _linearts[i].NewTarget(_grid[r, c]);
            if (_linearts[i].P1 == _linearts[i].P2 || _linearts[i].P1 == _linearts[i].P3 || _linearts[i].P2 == _linearts[i].P3)
            {
                p3++;
            }
            _linearts[i].Color = (i * _palette.Size()) / _count;
            _linearts[i].LoadContent(GraphicsDevice);
            _linearts[i].MoveBoth = (i % 2) == 0;
        }
    }

    public override void Update(GameTime gameTime)
    {
        _ticker = (_ticker + 1) % 1;
        if (_ticker != 0)
        {
            return;
        }
        // for (int i = 0; i < _count; i++)
        // {
        int i = _current;
        if (_linearts[i].Update(gameTime))
        {
            _current = (_current + 1) % _count;
            if (_advance)
            {
                int p3, r, c;
                int tries = 0;
                do
                {

                    p3 = _rng.Next(0, 8);
                    if (p3 >= 4)
                    {
                        p3++;
                    }
                    tries++;
                    if (tries > 8)
                    {
                        p3 = -1;
                        for (int j = 0; j < 9; j++)
                        {
                            if (!isBoring(_linearts[i].P1, _linearts[i].P2, j))
                            {
                                p3 = j;
                                break;
                            }
                        }
                        if (p3 == -1)
                        {
                            p3 = 0;
                            break;
                        }
                    }
                } while (isBoring(_linearts[i].P1, _linearts[i].P2, p3));

                (r, c) = gridIndex(p3);
                _linearts[i].NewTarget(_grid[r, c]);
                if (_linearts[i].P1 == _linearts[i].P2 || _linearts[i].P1 == _linearts[i].P3 || _linearts[i].P2 == _linearts[i].P3)
                {
                    p3++;
                }
                // _advance = false;
            }
            // and then we stutter from redrawing the 0th line, which should be
            // identical to the last line of the previous thing.
        }
        // }
    }

    public override void Draw(GameTime gameTime)
    {
        for (int i = 0; i < _count; i++)
        {
            _linearts[i].Draw(gameTime, GraphicsDevice);
        }
    }
}