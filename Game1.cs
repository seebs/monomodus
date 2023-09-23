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

    private Spirals _spirals;
    private Palette _bigRainbow, _smallRainbow;
    private int _complexity; // palette depth and # of line segments
    private Random _rng;

    private Oversaturator _oversaturator;

    private KeyboardState _prevKB;
    private bool _debugging;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        _graphics.PreferredBackBufferWidth = 900;  // set this value to the desired width of your window
        _graphics.PreferredBackBufferHeight = 600;   // set this value to the desired height of your window
        _graphics.ApplyChanges();


        _rng = new Random();
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        _complexity = 400;
        _bigRainbow = new Palette(_complexity / 2);
        _smallRainbow = new Palette(1);
        _spirals = new Spirals(this, 3, _complexity, 0.005f, 6, 4, _bigRainbow);
        Components.Add(_spirals);

        _squares = new Squares(this, 7, _smallRainbow);
        Components.Add(_squares);

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
        _prevKB = kb;
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || kb.IsKeyDown(Keys.Escape))
        {
            Exit();
        }

        // for (int row = 0; row < _squares.S.GetLength(0); row++)
        // {
        //     for (int col = 0; col < _squares.S.GetLength(1); col++)
        //     {
        //         float a = _squares.S[row, col].Alpha;
        //         a -= 0.001f;
        //         if (a < 0)
        //         {
        //             a = 0;
        //         }
        //         _squares.S[row, col].Alpha = a;
        //     }
        // }

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
