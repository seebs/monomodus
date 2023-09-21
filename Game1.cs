using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace MonoModus;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;


    private Squares _squares;

    private Polyline[] _spirals;
    private Vector2[] _spiralTargets;
    private Vector2[] _spiralDeltas;
    private Palette _rainbow;
    private int[] _spiralColors;
    private Vector2 _center;
    private int _width, _height;
    private int _complexity; // palette depth and # of line segments
    private Random _rng;

    private Oversaturator _oversaturator;

    private static int[] _ripplePattern = { -1, -2, 0, 2, 1, 0, -1, 0, 1 };
    private List<int>[] _ripples;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        _graphics.PreferredBackBufferWidth = 1200;  // set this value to the desired width of your window
        _graphics.PreferredBackBufferHeight = 900;   // set this value to the desired height of your window
        _graphics.ApplyChanges();


        _rng = new Random();
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        _complexity = 400;
        _rainbow = new Palette(_complexity / 2);
        _spirals = new Polyline[3];
        _spiralTargets = new Vector2[3];
        _spiralDeltas = new Vector2[3];
        _spiralColors = new int[3];
        _ripples = new List<int>[3];
        for (int i = 0; i < 3; i++)
        {
            _spirals[i] = new Polyline(this, _complexity, 1.5f, 6, 4, _rainbow);
            _ripples[i] = new List<int>();
            Components.Add(_spirals[i]);
        }

        _squares = new Squares(this, 10);
        // Components.Add(_squares);

        _oversaturator = new Oversaturator(this);
        Components.Add(_oversaturator);
    }

    protected override void Initialize()
    {
        // TODO: Add your initialization logic here
        base.Initialize();
    }

    protected override void LoadContent()
    {
        PresentationParameters pp = GraphicsDevice.PresentationParameters;

        _width = pp.BackBufferWidth;
        _height = pp.BackBufferHeight;
        _center.X = _width / 2;
        _center.Y = _height / 2;
        for (int i = 0; i < 3; i++)
        {
            _spiralTargets[i].X = (float)_rng.Next(0, _width);
            _spiralTargets[i].Y = (float)_rng.Next(0, _height);
            _spiralDeltas[i].X = (1.5f - (float)_rng.Next(1, 3)) * (float)_rng.Next(5, 9);
            _spiralDeltas[i].Y = (1.5f - (float)_rng.Next(1, 3)) * (float)_rng.Next(5, 9);
            _spiralColors[i] = (i * _rainbow.Size()) / 3;
        }
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();
        int[] ripples = new int[_spirals[0].Points.Length];
        for (int i = 0; i < 3; i++)
        {
            int l = _spirals[i].Points.Length;
            for (int j = 0; j < l; j++)
            {
                ripples[j] = 0;
            }
            for (int j = 0; j < _ripples[i].Count; j++)
            {
                int p = _ripples[i][j];
                for (int k = 0; k < _ripplePattern.Length; k++)
                {
                    ripples[p] += _ripplePattern[k];
                    p--;
                    if (p < 0)
                    {
                        break;
                    }
                }
                _ripples[i][j]--;
                if (_ripples[i][j] <= 0)
                {
                    _ripples[i].RemoveAt(j);
                    --j;
                }
            }
            Polyline s = _spirals[i];
            double thetaPerSegment = (Math.PI * 4) / (double)l;
            _spiralColors[i]++;
            _spiralColors[i] %= _rainbow.Size();
            float dx = _spiralTargets[i].X - _center.X;
            float dy = _spiralTargets[i].Y - _center.Y;
            if (dx == 0 && dy == 0)
            {
                // just in case!
                for (int j = 0; j < l; j++)
                {
                    s.Points[i] = _center;
                }
                continue;
            }
            double baseTheta = Math.Atan2((double)dx, (double)dy);
            double fullRadius = Math.Sqrt((double)(dx * dx + dy * dy));
            double partRadius = fullRadius / (double)l;
            int color = _spiralColors[i];
            double theta = baseTheta;
            for (int j = l - 1; j >= 0; j--)
            {
                (double sin, double cos) = Math.SinCos(theta);
                double r = partRadius * j;
                double rippled = r * (double)(40 + ripples[j]) / (double)40;
                s.Points[j].X = _center.X + (float)(sin * rippled);
                s.Points[j].Y = _center.Y + (float)(cos * rippled);
                s.Colors[j] = color + (ripples[j] * _complexity / 5);
                color = (color + 1) % _rainbow.Size();
                s.Alphas[j] = 1.0f;
                if (r > 0)
                {
                    theta -= thetaPerSegment * Math.Sqrt(fullRadius / r);
                }
            }
            _spiralTargets[i] += _spiralDeltas[i];
            bool bounced = false;
            if (_spiralTargets[i].X < 0)
            {
                bounced = true;
                _spiralTargets[i].X *= -1;
                _spiralDeltas[i].X *= -1;
            }
            else if (_spiralTargets[i].X > (float)_width)
            {
                bounced = true;
                _spiralTargets[i].X = (float)_width - (_spiralTargets[i].X - (float)_width);
                _spiralDeltas[i].X *= -1;
            }
            if (_spiralTargets[i].Y < 0)
            {
                bounced = true;
                _spiralTargets[i].Y *= -1;
                _spiralDeltas[i].Y *= -1;
            }
            else if (_spiralTargets[i].Y > (float)_height)
            {
                bounced = true;
                _spiralTargets[i].Y = (float)_height - (_spiralTargets[i].Y - (float)_height);
                _spiralDeltas[i].Y *= -1;
            }

            if (bounced)
            {
                _ripples[i].Add(l - 1);
            }
        }
        // TODO: Add your update logic here

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);

        // TODO: Add your drawing code here
        _oversaturator.RenderHere();
        base.Draw(gameTime);
    }
}
