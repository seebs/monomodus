using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace MonoModus;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;


    private Squares _squares;

    private Polyline[] _spirals;
    private Vector2[] _spiralTargets;
    private Vector2[] _spiralDeltas;
    private int[] _spiralColors;
    private Vector2 _center;
    private int _width, _height;
    private Random _rng;

    private Oversaturator _oversaturator;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);


        _rng = new Random();
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        Palette rainbow = new Palette(12);
        _spirals = new Polyline[3];
        _spiralTargets = new Vector2[3];
        _spiralDeltas = new Vector2[3];
        _spiralColors = new int[3];
        for (int i = 0; i < 3; i++)
        {
            _spirals[i] = new Polyline(this, 200, 2, 2, rainbow);
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
            _spiralDeltas[i].X = (1.5f - (float)_rng.Next(1, 3)) * (float)_rng.Next(10, 18);
            _spiralDeltas[i].Y = (1.5f - (float)_rng.Next(1, 3)) * (float)_rng.Next(10, 18);
            _spiralColors[i] = 24 * i;
        }
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();
        for (int i = 0; i < 3; i++)
        {
            int l = _spirals[i].Points.Length;
            double thetaPerSegment = (Math.PI * 6) / (double)l;
            _spiralColors[i]++;
            _spiralColors[i] %= 72;
            float dx = _spiralTargets[i].X - _center.X;
            float dy = _spiralTargets[i].Y - _center.Y;
            if (dx == 0 && dy == 0)
            {
                // just in case!
                for (int j = 0; j < l; j++)
                {
                    _spirals[i].Points[i] = _center;
                }
                continue;
            }
            double baseTheta = Math.Atan2((double)dx, (double)dy);
            double radius = Math.Sqrt((double)(dx * dx + dy * dy));
            int color = _spiralColors[i];
            for (int j = 0; j < l; j++)
            {
                double theta = ((double)j * thetaPerSegment) + baseTheta;
                (double sin, double cos) = Math.SinCos(theta);
                _spirals[i].Points[j].X = _center.X + (float)((sin * radius * j) / (double)l);
                _spirals[i].Points[j].Y = _center.Y + (float)((cos * radius * j) / (double)l);
                _spirals[i].Colors[j] = color;
                color = (color + 1) % 72;
                _spirals[i].Alphas[j] = 1.0f;
            }
            _spiralTargets[i] += _spiralDeltas[i];
            if (_spiralTargets[i].X < 0)
            {
                _spiralTargets[i].X *= -1;
                _spiralDeltas[i].X *= -1;
            }
            else if (_spiralTargets[i].X > (float)_width)
            {
                _spiralTargets[i].X = (float)_width - (_spiralTargets[i].X - (float)_width);
                _spiralDeltas[i].X *= -1;
            }
            if (_spiralTargets[i].Y < 0)
            {
                _spiralTargets[i].Y *= -1;
                _spiralDeltas[i].Y *= -1;
            }
            else if (_spiralTargets[i].Y > (float)_width)
            {
                _spiralTargets[i].Y = (float)_width - (_spiralTargets[i].Y - (float)_width);
                _spiralDeltas[i].Y *= -1;
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
