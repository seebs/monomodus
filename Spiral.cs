using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace MonoModus;

class Spiral
{
    private static int[] _ripplePattern = { -1, -2, 0, 2, 1, 0, -1, 0, 1 };
    public Vector2 Center;
    public Vector2 Target;
    public Vector2 Velocity;
    public int Color;
    private Palette _palette;
    private Polyline _polyline;
    private List<int> _ripples;
    private int _points;
    private Modus _game;
    private Vector2 _bounds;
    private float _spinny;

    public Spiral(Modus game, int points, float spinny, float thickness, int trails, int trailFrames, Palette palette)
    {
        _game = game;
        _palette = palette;
        _ripples = new List<int>();
        _points = points;
        _spinny = spinny;
        _polyline = new Polyline(game, _points, thickness, trails, trailFrames, _palette);
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
        _bounds = new Vector2(x: 1 / xScale, y: 1 / yScale);
        _polyline.LoadContent(gd);
    }
    public void Update(GameTime gameTime)
    {
        int[] ripples = new int[_points];
        int l = _points;
        for (int i = 0; i < l; i++)
        {
            ripples[i] = 0;
        }
        for (int i = 0; i < _ripples.Count; i++)
        {
            int p = _ripples[i];
            for (int j = 0; j < _ripplePattern.Length; j++)
            {
                ripples[p] += _ripplePattern[j];
                p--;
                if (p < 0)
                {
                    break;
                }
            }
            _ripples[i] -= (_points / 200) + 1;
            if (_ripples[i] <= 0)
            {
                _ripples.RemoveAt(i);
                --i;
            }
        }
        double thetaPerSegment = (Math.PI * 4) / (double)l;
        Color = (Color + 1) % _palette.Size();
        float dx = Target.X - Center.X;
        float dy = Target.Y - Center.Y;
        if (dx == 0 && dy == 0)
        {
            // just in case!
            for (int i = 0; i < l; i++)
            {
                _polyline.Points[i] = Center;
            }
            return;
        }
        double baseTheta = Math.Atan2((double)dy, (double)dx);
        double fullRadius = Math.Sqrt((double)(dx * dx + dy * dy));
        double partRadius = fullRadius / (double)l;
        int color = Color;
        double theta = baseTheta;
        for (int i = l - 1; i >= 0; i--)
        {
            (double sin, double cos) = Math.SinCos(theta);
            double r = partRadius * i;
            double rippled = r * (double)(40 + ripples[i]) / (double)40;
            Vector2 prev = _polyline.Points[i];
            _polyline.Points[i].X = Center.X + (float)(cos * rippled);
            _polyline.Points[i].Y = Center.Y + (float)(sin * rippled);
            _polyline.Colors[i] = color + (ripples[i] * _points / 5);
            if (ripples[i] != 0)
            {
                _game.Notice(_polyline.Points[i], _polyline.Points[i] - prev, (float)Math.Sqrt(r / fullRadius) * (float)Math.Abs((double)ripples[i]), _polyline.Colors[i]);
            }
            color = (color + 1) % _palette.Size();
            _polyline.Alphas[i] = 1.0f;
            if (r > 0)
            {
                theta += (double)_spinny * thetaPerSegment * Math.Sqrt(fullRadius / r);
            }
        }
        Target += Velocity;
        bool bounced = false;
        if ((float)Math.Abs((double)Target.X) > _bounds.X)
        {
            bounced = true;
            if (Target.X > 0)
            {
                Target.X = (2 * _bounds.X) - Target.X;
            }
            else
            {
                Target.X = (-2 * _bounds.X) - Target.X;
            }
            Velocity.X *= -1;
        }
        if ((float)Math.Abs((double)Target.Y) > _bounds.Y)
        {
            bounced = true;
            if (Target.Y > 0)
            {
                Target.Y = (2 * _bounds.Y) - Target.Y;
            }
            else
            {
                Target.Y = (-2 * _bounds.Y) - Target.Y;
            }
            Velocity.Y *= -1;
        }

        if (bounced)
        {
            _ripples.Add(l - 1);
        }
        // _game.Notice(Target, Velocity, 1, Color);
        _polyline.Update(gameTime);
    }

    public void Draw(GameTime gameTime, GraphicsDevice gd)
    {
        _polyline.Draw(gameTime, gd);
    }

    public void LoadTextures(GraphicsDevice gd)
    {
        _polyline.LoadTextures(gd);
    }
}

class Spirals : DrawableGameComponent
{
    private int _count;
    private float _width, _height;
    private Spiral[] _spirals;
    private Random _rng;
    private Vector2 _center;
    private Palette _palette;


    public Spirals(Modus game, int spirals, int points, float thickness, int trails, int trailFrames, Palette palette)
            : base(game)
    {
        _count = spirals;
        _spirals = new Spiral[_count];
        _palette = palette;
        _rng = new Random();
        for (int i = 0; i < _count; i++)
        {
            float spinny = 1.0f;
            _spirals[i] = new Spiral(game, points, spinny, thickness, trails, trailFrames, _palette);
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

        _center.X = 0;
        _center.Y = 0;
        for (int i = 0; i < _count; i++)
        {
            _spirals[i].Center = _center;

            _spirals[i].Target.X = (float)_rng.NextDouble(); //  * _width * 2 - _width;
            _spirals[i].Target.Y = (float)_rng.NextDouble(); // * _height * 2 - _height;
            _spirals[i].Velocity.X = (1.5f - (float)_rng.Next(1, 3)) * (float)(_rng.NextDouble() + 1) / 96;
            _spirals[i].Velocity.Y = (1.5f - (float)_rng.Next(1, 3)) * (float)(_rng.NextDouble() + 1) / 96;
            _spirals[i].Color = (i * _palette.Size()) / _count;
            _spirals[i].LoadContent(GraphicsDevice);
        }
    }

    public override void Update(GameTime gameTime)
    {
        for (int i = 0; i < _count; i++)
        {
            _spirals[i].Update(gameTime);
        }
    }

    public override void Draw(GameTime gameTime)
    {
        for (int i = 0; i < _count; i++)
        {
            _spirals[i].Draw(gameTime, GraphicsDevice);
        }
    }

    public void LoadTextures(GraphicsDevice gd)
    {
        for (int i = 0; i < _count; i++)
        {
            _spirals[i].LoadTextures(gd);
        }
    }
}