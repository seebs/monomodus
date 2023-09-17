
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace MonoModus;

class Squares : DrawableGameComponent
{
    private Texture2D _squareTx;

    private VertexPositionColorTexture[] _vertices;
    private Effect _effect;
    private short[] _indices;
    private int _scale; // the smaller of our width-or-height
    private int _width, _height, _totalSquares, _totalVertices, _totalIndices;
    private float _xOffset, _yOffset;
    private int _squareSize;
    private float _squareHalf;
    private Matrix _viewAdapted;

    private Color[] _colors;
    private float[] _scales;
    private float[] _rotations;

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

        _xOffset = (float)((screenWidth - (_width * _squareSize)) + _squareSize) / 2;
        _yOffset = (float)((screenHeight - (_height * _squareSize)) + _squareSize) / 2;
        _squareHalf = (float)_squareSize / 2;

        _vertices = new VertexPositionColorTexture[_totalVertices];
        _indices = new short[_totalIndices];
        _colors = new Color[_totalSquares];
        _rotations = new float[_totalSquares];
        _scales = new float[_totalSquares];
        for (int i = 0; i < _totalSquares; i++)
        {
            int vertex = i * 4;
            int index = i * 6;
            _colors[i] = Color.White;
            _scales[i] = ((float)((i % _width) + 1) / (float)_width);
            _rotations[i] = (((float)i) / ((float)_totalSquares)) * (float)Math.PI;
            _vertices[vertex + 0].TextureCoordinate = new Vector2(0f, 0f);
            _vertices[vertex + 1].TextureCoordinate = new Vector2(1f, 0f);
            _vertices[vertex + 2].TextureCoordinate = new Vector2(0f, 1f);
            _vertices[vertex + 3].TextureCoordinate = new Vector2(1f, 1f);
            _indices[index + 0] = (short)(vertex + 0);
            _indices[index + 1] = (short)(vertex + 1);
            _indices[index + 2] = (short)(vertex + 2);
            _indices[index + 3] = (short)(vertex + 2);
            _indices[index + 4] = (short)(vertex + 1);
            _indices[index + 5] = (short)(vertex + 3);
        }
        Matrix viewTranslate = Matrix.CreateTranslation(-1f, -1f, 0f);
        Matrix viewScale = Matrix.CreateScale(2f / (float)screenWidth, 2f / (float)screenHeight, 1f);
        _viewAdapted = Matrix.Multiply(viewScale, viewTranslate);

        _squareTx = Game.Content.Load<Texture2D>("Textures/square");
        _effect = Game.Content.Load<Effect>("Effects/effects");
        _effect.CurrentTechnique = _effect.Techniques["Okay"];
        _effect.Parameters["xTexture"].SetValue(_squareTx);
        _effect.Parameters["xTranslate"].SetValue(_viewAdapted);

    }

    public override void Draw(GameTime gameTime)
    {
        foreach (EffectPass pass in _effect.CurrentTechnique.Passes)
        {
            pass.Apply();
        }
        GraphicsDevice.Clear(Color.Black);
        GraphicsDevice.BlendState = BlendState.Additive;
        for (int i = 0; i < _totalSquares; i++)
        {
            int vertex = i * 4;
            int x = (i % _width);
            int y = (i / _width);
            float centerX = _squareSize * (float)x + _xOffset;
            float centerY = _squareSize * (float)y + _yOffset;
            float offset = _squareHalf * _scales[i];
            (double sinD, double cosD) = Math.SinCos((double)_rotations[i]);
            float sin = (float)sinD;
            float cos = (float)cosD;
            float offsetX = (cos * offset) - (sin * offset);
            float offsetY = (cos * offset) + (sin * offset);
            _vertices[vertex + 0].Color = _colors[i];
            _vertices[vertex + 1].Color = _colors[i];
            _vertices[vertex + 2].Color = _colors[i];
            _vertices[vertex + 3].Color = _colors[i];
            _vertices[vertex + 0].Position = new Vector3(centerX - offsetX, centerY - offsetY, 0f);
            _vertices[vertex + 1].Position = new Vector3(centerX - offsetY, centerY + offsetX, 0f);
            _vertices[vertex + 2].Position = new Vector3(centerX + offsetY, centerY - offsetX, 0f);
            _vertices[vertex + 3].Position = new Vector3(centerX + offsetX, centerY + offsetY, 0f);
        }

        GraphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, _vertices, 0, _totalVertices, _indices, 0, _totalSquares * 2);
    }

}