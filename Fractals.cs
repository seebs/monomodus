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
    private Memory<Vector2> _linePoints;
    private Memory<Vector2> _lineColors;
    private int _points;
    private Modus _game;

    public Fractal(Modus game, Fractal parent, int points, Palette palette)
    {
        _game = game;
        _palette = palette;
        _parent = parent;
        _points = points;
    }

    public void SetBase(Polyline b)
    {
        _base = b;
    }

    public void LoadContent(Memory<Vector2> points, Memory<Vector2> colors)
    {
        _linePoints = points;
        _lineColors = colors;
    }
    public void Update(GameTime gameTime)
    {
        // can only update relative to a parent
        if (_parent == null)
        {
            return;
        }
        Span<Vector2> ppoints = _parent._linePoints.Span;
        Span<Vector2> pcolors = _parent._lineColors.Span;
        Span<Vector2> points = _linePoints.Span;
        Span<Vector2> colors = _lineColors.Span;
        Vector2 prev = ppoints[0];
        int l = _parent._points;
        int n = 0;
        points[n] = Vector2.Zero;
        colors[n].X = pcolors[0].X + (float)_base.Colors[0];
        n++;
        for (int i = 1; i < l; i++)
        {
            Vector2 next = ppoints[i];
            float pcolor = pcolors[i].X;
            Vector2 delta = next - prev;
            float afA, afB, afC, afD, afE, afF;
            // A B C
            // D E F
            afA = delta.X;
            afD = delta.Y;
            afB = -delta.Y;
            afE = delta.X;
            afC = prev.X;
            afF = prev.Y;
            for (int j = 1; j < _base.Points.Length; j++)
            {
                Vector2 p = _base.Points[j];
                float x = (afA * p.X) + (afB * p.Y) + afC;
                float y = (afD * p.X) + (afE * p.Y) + afF;
                points[n] = new Vector2(x, y);
                colors[n].X = pcolor + _base.Colors[j];
                n++;
            }
            prev = next;
        }
    }

    public void Reset()
    {
        Span<Vector2> points = _linePoints.Span;
        Span<Vector2> colors = _lineColors.Span;
        points[0] = new Vector2(0, 0);
        points[1] = new Vector2(1, 0);
        colors[0].X = 1;
        colors[1].X = 1;
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
    private Fastline _line;


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
        int[] partialPoints = new int[_depth];
        float[] thicknesses = new float[_depth];
        for (int i = 0; i < _depth; i++)
        {
            partialPoints[i] = perLine;
            thicknesses[i] = thickness;
            _fractals[i] = new Fractal(game, prev, perLine, _palette);
            prev = _fractals[i];
            perLine = ((perLine - 1) * multiplier) + 1;
            thickness *= 0.9f;
        }
        _line = new Fastline(game, partialPoints, thicknesses, _palette);
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
        _base.Colors[0] = 1;
        _base.Points[1] = new Vector2(0.03f, 0.15f);
        _base.Colors[1] = 2;
        _base.Points[2] = new Vector2(0.97f, -0.15f);
        _base.Colors[2] = 2;
        // for (int i = 1; i < _points - 1; i++)
        // {
        //     _base.Points[i] = new Vector2((float)i / (float)_points, (float)_rng.NextDouble() / 3);
        //     _base.Colors[i] = 1;
        // }
        _base.Points[_points - 1] = new Vector2(1f, 0f);
        _base.Colors[_points - 1] = 1;

        for (int i = 0; i < _depth; i++)
        {
            Memory<Vector2> points = _line.PointRef(i);
            Memory<Vector2> colors = _line.ColorRef(i);
            _fractals[i].LoadContent(points, colors);
            _fractals[i].SetBase(_base);
        }
        _fractals[0].Reset();
        _line.LoadContent(GraphicsDevice);
    }

    public override void Update(GameTime gameTime)
    {
        _theta += 0.01f;
        float s, c;
        (s, c) = MathF.SinCos(_theta);
        _base.Points[1].X = 0.03f + c * .2f;
        _base.Points[1].Y = 0.15f + s * .2f;
        // _base.Points[2].X = 0.97f + c * .05f;
        // _base.Points[2].Y = -0.15f + s * .05f;
        for (int i = 1; i < _depth; i++)
        {
            _fractals[i].Update(gameTime);
        }
        _line.Update(gameTime, 1, _depth, true);
    }

    public override void Draw(GameTime gameTime)
    {
        _line.Draw(gameTime, GraphicsDevice);
    }

    public void LoadTextures(GraphicsDevice gd)
    {
        // nothing to do here
        _line.LoadContent(GraphicsDevice);
    }
}