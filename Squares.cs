
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace MonoModus;

class Squares : DrawableGameComponent
{
    private Texture2D _squareTx;

    private VertexPositionColorTexture[] _vertices;
    private Effect _effect;
    private short[] _indices;
    private int _scale; // the smaller of our width-or-height
    private int _width, _height, _totalSquares, _totalVertices, _totalIndices;
    private int _xOffset, _yOffset;
    private int _squareSize;
    private Matrix _viewAdapted;

    public Squares(Game game, int scale)
            : base(game)
    {
        _scale = scale;
    }

    protected override void LoadContent()
    {
        // Look up the resolution and format of our main backbuffer.
        PresentationParameters pp = GraphicsDevice.PresentationParameters;

        int screenWidth = pp.BackBufferWidth;
        int screenHeight = pp.BackBufferHeight;

        bool sideways = screenWidth < screenHeight;

        if (sideways)
        {
            _width = _scale;
            _squareSize = screenWidth / _width;
            _height = screenHeight / _squareSize;
        }
        else
        {
            _height = _scale;
            _squareSize = screenHeight / _height;
            _width = screenWidth / _squareSize;
        }
        _totalSquares = _width * _height;

        // shrink to fit
        while (_totalSquares > (32764 / 4))
        {
            _width--;
            _squareSize = screenWidth / _width;
            _height = screenHeight / _squareSize;
            _totalSquares = _width * _height;
        }
        _totalVertices = _totalSquares * 4;
        _totalIndices = _totalSquares * 6;

        _xOffset = (screenWidth - (_width * _squareSize)) / 2;
        _yOffset = (screenHeight - (_height * _squareSize)) / 2;

        _vertices = new VertexPositionColorTexture[_totalVertices];
        _indices = new short[_totalIndices];
        for (int i = 0; i < _totalSquares; i++)
        {
            int vertex = i * 4;
            int index = i * 6;
            _vertices[vertex + 0].TextureCoordinate = new Vector2(0f, 0f);
            _vertices[vertex + 1].TextureCoordinate = new Vector2(1f, 0f);
            _vertices[vertex + 2].TextureCoordinate = new Vector2(0f, 1f);
            _vertices[vertex + 3].TextureCoordinate = new Vector2(1f, 1f);
            _vertices[vertex + 0].Color = Color.White;
            _vertices[vertex + 1].Color = Color.White;
            _vertices[vertex + 2].Color = Color.White;
            _vertices[vertex + 3].Color = Color.White;
            _indices[index + 0] = (short)(vertex + 0);
            _indices[index + 1] = (short)(vertex + 1);
            _indices[index + 2] = (short)(vertex + 2);
            _indices[index + 3] = (short)(vertex + 2);
            _indices[index + 4] = (short)(vertex + 1);
            _indices[index + 5] = (short)(vertex + 3);
        }

        _squareTx = Game.Content.Load<Texture2D>("Textures/square");
        _effect = Game.Content.Load<Effect>("Effects/effects");
        Matrix viewTranslate = Matrix.CreateTranslation(-1f, -1f, 0f);

        Matrix viewScale = Matrix.CreateScale(2f / (float)screenWidth, 2f / (float)screenHeight, 1f);
        _viewAdapted = Matrix.Multiply(viewScale, viewTranslate);
    }

    public override void Draw(GameTime gameTime)
    {
        _effect.CurrentTechnique = _effect.Techniques["Okay"];
        _effect.Parameters["xTexture"].SetValue(_squareTx);
        _effect.Parameters["xTranslate"].SetValue(_viewAdapted);
        foreach (EffectPass pass in _effect.CurrentTechnique.Passes)
        {
            pass.Apply();
        }
        GraphicsDevice.Clear(Color.Black);
        GraphicsDevice.BlendState = BlendState.Additive;
        for (int i = 0; i < _totalSquares; i++)
        {
            int vertex = i * 4;
            float x = ((i % _width) * _squareSize) + _xOffset;
            float y = ((i / _width) * _squareSize) + _yOffset;
            _vertices[vertex + 0].Position = new Vector3((x + 0), (y + 0), 0f);
            _vertices[vertex + 1].Position = new Vector3((x + 0), (y + _squareSize), 0f);
            _vertices[vertex + 2].Position = new Vector3((x + _squareSize), (y + 0), 0f);
            _vertices[vertex + 3].Position = new Vector3((x + _squareSize), (y + _squareSize), 0f);

        }

        GraphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, _vertices, 0, _totalVertices, _indices, 0, _totalSquares * 2);
    }

}