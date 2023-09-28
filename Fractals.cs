using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace MonoModus;

// Time to do iterative function systems again. This is the
// line kind, where we start with a line from [0, 0] to [1, 0],
// and a set of line segments that start at [0, 0] and go to [1, 0],
// and we iterate replacing the initial line with the line segments,
// then replacing each of them with the same segments again, and
// so on.
//
// Our "pattern" defines a Polyline, with the restrictions that
// the first point's location is always [0,0] and the last is
// always[1,0]. So for instance, we might have the typical snowflake:
//
//
//       /\
//      /  \        
//  ---/    \---
//
// So, we have defined 5 points, providing 4 line segments. When
// we iterate again, we end up with 20 points, providing 16 line
// segments -- because the last point of the first line segment
// and the first point of the second line segment are at the same
// location, but one has the color of the last point in
// the pattern, and one has the color of the first. However, we
// don't want to try to compute the fractal expansion between
// those points! So when we have our 20 points, we don't generate
// 5 new points for each of the 19 pairs, we generate 5 points for
// each of the 16 non-empty line segments, and then that produces
// 4 line segments per 5 points, or 80 total line segments.
//
// Which is a pain. So let's cheat: Let's require that the starting
// and ending *color* be the same too. Now, when we iterate, we just
// produce 17 points and 16 line segments, and next time, it'll be
// 65 points and 64 line segments, and everything's a lot easier.


class Fractal
{
    private Fractal _parent;
    private Polyline _base;
    private Palette _palette;
    private Fastline _line;
    private int _points;
    private Modus _game;

    public Fractal(Modus game, Fractal parent, int points, float thickness, int trails, int trailFrames, Palette palette)
    {
        _game = game;
        _palette = palette;
        _parent = parent;
        _points = points;
        _line = new Fastline(game, _points, thickness, trails, trailFrames, _palette);
    }

    public void SetBase(Polyline b)
    {
        _base = b;
    }

    public void LoadContent(GraphicsDevice gd)
    {
        PresentationParameters pp = gd.PresentationParameters;

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
        _line.LoadContent(gd);
        for (int i = 0; i < _points; i++)
        {
            _line.Alphas[i] = 1.0f;
        }
    }
    public void Update(GameTime gameTime)
    {
        // can only update relative to a parent
        if (_parent == null)
        {
            // ... except we can still ask our line to
            // render itself
            _line.Update(gameTime);
            return;
        }
        Vector2[] ppoints = _parent._line.Points;
        int[] pcolors = _parent._line.Colors;
        Vector2 prev = ppoints[0];
        int l = _parent._points;
        int n = 0;
        _line.Points[n] = Vector2.Zero;
        _line.Colors[n] = pcolors[0] + _base.Colors[0];
        n++;
        for (int i = 1; i < l; i++)
        {
            Vector2 next = ppoints[i];
            int pcolor = pcolors[i];
            Vector2 delta = next - prev;
            float len = delta.Length();
            float s, c;
            s = delta.Y / len;
            c = delta.X / len;
            float afA, afB, afC, afD, afE, afF;
            // A B C
            // D E F
            afA = len * c;
            afD = len * s;
            afB = len * -s;
            afE = afA;
            afC = prev.X;
            afF = prev.Y;
            for (int j = 1; j < _base.Points.Length; j++)
            {
                Vector2 p = _base.Points[j];
                float x = (afA * p.X) + (afB * p.Y) + afC;
                float y = (afD * p.X) + (afE * p.Y) + afF;
                _line.Points[n] = new Vector2(x, y);
                _line.Colors[n] = pcolor + _base.Colors[j];
                n++;
            }
            prev = next;
        }
        _line.Update(gameTime);
    }

    public void Draw(GameTime gameTime, GraphicsDevice gd)
    {
        _line.Draw(gameTime, gd);
    }

    public void Reset()
    {
        _line.Points[0] = new Vector2(0, 0);
        _line.Points[1] = new Vector2(1, 0);
        _line.Colors[0] = 1;
        _line.Colors[1] = 1;
    }

    public void LoadTextures(GraphicsDevice gd)
    {
        _line.LoadTextures(gd);
    }
}

class Fractals : DrawableGameComponent
{
    private int _depth, _points;
    private float _width, _height;
    private Fractal[] _fractals;
    private Random _rng;
    private Palette _palette;
    private Polyline _base;
    private Modus _game;
    private float _theta;


    public Fractals(Modus game, int depth, int points, float thickness, int trails, int trailFrames, Palette palette)
            : base(game)
    {
        _game = game;
        _depth = depth;
        _points = points;
        _fractals = new Fractal[_depth];
        _palette = palette;
        _rng = new Random();
        int multiplier = _points - 1;
        int perLine = 2;
        Fractal prev = null;
        for (int i = 0; i < _depth; i++)
        {
            _fractals[i] = new Fractal(game, prev, perLine, thickness, trails, trailFrames, _palette);
            prev = _fractals[i];
            perLine = ((perLine - 1) * multiplier) + 1;
            thickness *= 0.9f;
        }
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

        // we don't really need a PolyLine, and we never LoadContent on this,
        // or Update, or Draw it. we just use it because it's a familiar data
        // structure.
        _base = new Polyline(_game, _points, 0.1f, 1, 1, _palette);
        _base.LoadContent(GraphicsDevice);
        _base.Points[0] = new Vector2(0f, 0f);
        _base.Colors[0] = 4;
        _base.Points[1] = new Vector2(0.03f, 0.15f);
        _base.Colors[1] = 1;
        _base.Points[2] = new Vector2(0.97f, -0.15f);
        _base.Colors[2] = 1;
        // for (int i = 1; i < _points - 1; i++)
        // {
        //     _base.Points[i] = new Vector2((float)i / (float)_points, (float)_rng.NextDouble() / 3);
        //     _base.Colors[i] = 1;
        // }
        _base.Points[_points - 1] = new Vector2(1f, 0f);
        _base.Colors[_points - 1] = 8;

        for (int i = 0; i < _depth; i++)
        {
            _fractals[i].LoadContent(GraphicsDevice);
            _fractals[i].SetBase(_base);
        }
        _fractals[0].Reset();
    }

    public override void Update(GameTime gameTime)
    {
        _theta += 0.02f;
        double s, c;
        (s, c) = Math.SinCos((double)_theta);
        _base.Points[1].X = 0.03f + (float)c * .05f;
        _base.Points[1].Y = 0.15f + (float)s * .05f;
        _base.Points[2].X = 0.97f + (float)c * .05f;
        _base.Points[2].Y = -0.15f + (float)s * .05f;
        for (int i = 1; i < _depth; i++)
        {
            _fractals[i].Update(gameTime);
        }
    }

    public override void Draw(GameTime gameTime)
    {
        // skip 0 (it's just a line, it's boring)
        for (int i = 1; i < _depth; i++)
        {
            _fractals[i].Draw(gameTime, GraphicsDevice);
        }
    }

    public void LoadTextures(GraphicsDevice gd)
    {
        for (int i = 0; i < _depth; i++)
        {
            _fractals[i].LoadTextures(gd);
        }
    }
}