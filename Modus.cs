using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace MonoModus;

public class Modus : Game
{
    private GraphicsDeviceManager _graphics;


    private Squares _squares;

    private Spirals _spirals;
    private Linearts _linearts;
    private Fractals _fractals;
    private Palette _bigRainbow, _medRainbow, _smallRainbow;
    private int _complexity; // palette depth and # of line segments
    private Random _rng;

    private Oversaturator _oversaturator;

    private KeyboardState _prevKB;
    private bool _debugging;

    public Modus()
    {
        _graphics = new GraphicsDeviceManager(this);
        _graphics.PreferredBackBufferWidth = 1200;  // set this value to the desired width of your window
        _graphics.PreferredBackBufferHeight = 900;   // set this value to the desired height of your window
        _graphics.ApplyChanges();


        _rng = new Random();
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        _complexity = 400;
        _bigRainbow = new Palette(_complexity / 2);
        _smallRainbow = new Palette(1);
        _medRainbow = new Palette(4);
        _spirals = new Spirals(this, 3, _complexity, 0.005f, 6, 4, _bigRainbow);
        // Components.Add(_spirals);

        _squares = new Squares(this, 60, _bigRainbow);
        Components.Add(_squares);

        // _linearts = new Linearts(this, 3, 36, .005f, 1, 1, _bigRainbow);
        // Components.Add(_linearts);
        _fractals = new Fractals(this, 12, 4, 0.005f, 1, 1, _medRainbow);
        Components.Add(_fractals);

        _oversaturator = new Oversaturator(this);
        Components.Add(_oversaturator);
    }

    public void Notice(Vector2 pos, Vector2 velocity, float intensity, int c)
    {
        (int row, int col, bool ok) = _squares.SquareAt(pos);
        if (ok)
        {
            _squares.S[row, col].Color = c;
            _squares.S[row, col].Alpha += 0.1f * intensity;
            _squares.S[row, col].Scale += 0.1f * intensity;
            if (_squares.S[row, col].Scale > 1)
            {
                _squares.S[row, col].Scale = 1.0f;
            }
            // _squares.S[row, col].Offset += velocity * 2;
        }
    }

    protected override void Initialize()
    {
        // TODO: Add your initialization logic here
        base.Initialize();
        // start empty
        for (int row = 0; row < _squares.S.GetLength(0); row++)
        {
            for (int col = 0; col < _squares.S.GetLength(1); col++)
            {
                _squares.S[row, col].Alpha = 0;
            }
        }
    }

    protected override void LoadContent()
    {
        _oversaturator.Debug(false);
        _squares.LoadTextures();
    }

    protected override void Update(GameTime gameTime)
    {
        KeyboardState kb = Keyboard.GetState();
        if (kb.IsKeyDown(Keys.D) && !_prevKB.IsKeyDown(Keys.D))
        {
            _debugging = !_debugging;
            _oversaturator.Debug(_debugging);
        }
        if (kb.IsKeyDown(Keys.D0) && !_prevKB.IsKeyDown(Keys.D0))
        {
            _oversaturator.SetPrimary(0);
        }
        if (kb.IsKeyDown(Keys.D1) && !_prevKB.IsKeyDown(Keys.D1))
        {
            _oversaturator.SetPrimary(1);
        }
        if (kb.IsKeyDown(Keys.D2) && !_prevKB.IsKeyDown(Keys.D2))
        {
            _oversaturator.SetPrimary(2);
        }
        if (kb.IsKeyDown(Keys.D3) && !_prevKB.IsKeyDown(Keys.D3))
        {
            _oversaturator.SetPrimary(3);
        }
        if (kb.IsKeyDown(Keys.D4) && !_prevKB.IsKeyDown(Keys.D4))
        {
            _oversaturator.SetPrimary(4);
        }
        if (kb.IsKeyDown(Keys.N) && !_prevKB.IsKeyDown(Keys.N))
        {
            _linearts.advance();
        }
        _prevKB = kb;
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || kb.IsKeyDown(Keys.Escape))
        {
            Exit();
        }

        for (int row = 0; row < _squares.S.GetLength(0); row++)
        {
            for (int col = 0; col < _squares.S.GetLength(1); col++)
            {
                _squares.S[row, col].Alpha *= 0.995f;
                if (_squares.S[row, col].Scale > 0.5f)
                {
                    _squares.S[row, col].Scale *= 0.999f;
                }
            }
        }

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
