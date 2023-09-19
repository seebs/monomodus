using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace MonoModus;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;

    private Texture2D _squareTx;

    private Effect _effect;

    private Squares _squares;

    private Polyline _polyline;

    private Oversaturator _oversaturator;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        Palette rainbow = new Palette(10);
        _squares = new Squares(this, 10);
        _polyline = new Polyline(this, 3, 2, 6, rainbow);
        _oversaturator = new Oversaturator(this);
        Components.Add(_squares);
        Components.Add(_polyline);
        Components.Add(_oversaturator);
    }

    protected override void Initialize()
    {
        // TODO: Add your initialization logic here
        base.Initialize();
    }

    protected override void LoadContent()
    {
        // TODO: use this.Content to load your game content here
        _squareTx = Content.Load<Texture2D>("Textures/square");
        _effect = Content.Load<Effect>("Effects/effects");
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

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
