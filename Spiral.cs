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
    private int _width, _height;
    private Game _game;

    public Spiral(Game game, int points, float thickness, int trails, int trailFrames, Palette palette)
    {
        _game = game;
        _palette = palette;
        _ripples = new List<int>();
        _points = points;
        _polyline = new Polyline(game, _points, 2f, 6, 4, _palette);
    }

    public void LoadContent(GraphicsDevice gd)
    {
        PresentationParameters pp = gd.PresentationParameters;

        _width = pp.BackBufferWidth;
        _height = pp.BackBufferHeight;
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
            _ripples[i]--;
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
        double baseTheta = Math.Atan2((double)dx, (double)dy);
        double fullRadius = Math.Sqrt((double)(dx * dx + dy * dy));
        double partRadius = fullRadius / (double)l;
        int color = Color;
        double theta = baseTheta;
        for (int i = l - 1; i >= 0; i--)
        {
            (double sin, double cos) = Math.SinCos(theta);
            double r = partRadius * i;
            double rippled = r * (double)(40 + ripples[i]) / (double)40;
            _polyline.Points[i].X = Center.X + (float)(sin * rippled);
            _polyline.Points[i].Y = Center.Y + (float)(cos * rippled);
            _polyline.Colors[i] = color + (ripples[i] * _points / 5);
            color = (color + 1) % _palette.Size();
            _polyline.Alphas[i] = 1.0f;
            if (r > 0)
            {
                theta -= thetaPerSegment * Math.Sqrt(fullRadius / r);
            }
        }
        Target += Velocity;
        bool bounced = false;
        if (Target.X < 0)
        {
            bounced = true;
            Target.X *= -1;
            Velocity.X *= -1;
        }
        else if (Target.X > (float)_width)
        {
            bounced = true;
            Target.X = (float)_width - (Target.X - (float)_width);
            Velocity.X *= -1;
        }
        if (Target.Y < 0)
        {
            bounced = true;
            Target.Y *= -1;
            Velocity.Y *= -1;
        }
        else if (Target.Y > (float)_height)
        {
            bounced = true;
            Target.Y = (float)_height - (Target.Y - (float)_height);
            Velocity.Y *= -1;
        }

        if (bounced)
        {
            _ripples.Add(l - 1);
        }
        _polyline.Update(gameTime);
    }

    public void Draw(GameTime gameTime, GraphicsDevice gd)
    {
        _polyline.Draw(gameTime, gd);
    }
}

class Spirals : DrawableGameComponent
{
    private int _count;
    private int _width, _height;
    private Spiral[] _spirals;
    private Random _rng;
    private Vector2 _center;
    private Palette _palette;


    public Spirals(Game game, int spirals, int points, float thickness, int trails, int trailFrames, Palette palette)
            : base(game)
    {
        _count = spirals;
        _spirals = new Spiral[_count];
        _palette = palette;
        _rng = new Random();
        for (int i = 0; i < _count; i++)
        {
            _spirals[i] = new Spiral(game, points, thickness, trails, trailFrames, _palette);
        }
    }

    protected override void LoadContent()
    {
        PresentationParameters pp = GraphicsDevice.PresentationParameters;

        _width = pp.BackBufferWidth;
        _height = pp.BackBufferHeight;
        _center.X = _width / 2;
        _center.Y = _height / 2;
        for (int i = 0; i < _count; i++)
        {
            _spirals[i].Center = _center;

            _spirals[i].Target.X = (float)_rng.Next(0, _width);
            _spirals[i].Target.Y = (float)_rng.Next(0, _height);
            _spirals[i].Velocity.X = (1.5f - (float)_rng.Next(1, 3)) * (float)_rng.Next(5, 9);
            _spirals[i].Velocity.Y = (1.5f - (float)_rng.Next(1, 3)) * (float)_rng.Next(5, 9);
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
}