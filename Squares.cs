
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace MonoModus;

struct Square
{
    public int Color;
    public Vector2 Offset;
    public float Alpha, Scale, Rotation;
}

class Squares : DrawableGameComponent
{
    private Texture2D _squareTx;
    private Texture2D _unusedTx;

    private VertexPositionColorTexture[] _vertices;
    private Effect _effect;
    private IndexBuffer _indexBuffer;
    private VertexBuffer _vertexBuffer;
    private int[] _indices;
    private int _scale; // the smaller of our width-or-height
    private int _width, _height, _totalSquares, _totalVertices, _totalIndices;
    private float _xOffset, _yOffset;
    private int _squareSize;
    private float _squareHalf;
    private Matrix _viewAdapted;
    private Palette _palette;

    public Square[,] _squares;

    private Color[] _colors;
    private float[] _scales;
    private float[] _rotations;

    public Squares(Game game, int scale, Palette palette)
            : base(game)
    {
        _scale = scale;
        _palette = palette;
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
        _squares = new Square[_height, _width];
        _totalSquares = _width * _height;
        _totalVertices = _totalSquares * 4;
        _totalIndices = _totalSquares * 6;

        _xOffset = (float)((screenWidth - (_width * _squareSize)) + _squareSize) / 2;
        _yOffset = (float)((screenHeight - (_height * _squareSize)) + _squareSize) / 2;
        _squareHalf = (float)_squareSize / 2;

        _vertices = new VertexPositionColorTexture[_totalVertices];
        _indices = new int[_totalIndices];
        _colors = new Color[_totalSquares];
        _rotations = new float[_totalSquares];
        _scales = new float[_totalSquares];
        for (int i = 0; i < _totalSquares; i++)
        {
            int vertex = i * 4;
            int index = i * 6;
            int row = (i / _width);
            int col = (i % _width);
            _squares[row, col].Color = (i % _palette.Size());
            _squares[row, col].Alpha = 1.0f;
            _squares[row, col].Scale = 1.0f;
            _squares[row, col].Rotation = 0f;
            _vertices[vertex + 0].TextureCoordinate = new Vector2(0f, 0f);
            _vertices[vertex + 1].TextureCoordinate = new Vector2(1f, 0f);
            _vertices[vertex + 2].TextureCoordinate = new Vector2(0f, 1f);
            _vertices[vertex + 3].TextureCoordinate = new Vector2(1f, 1f);
            _indices[index + 0] = (vertex + 0);
            _indices[index + 1] = (vertex + 1);
            _indices[index + 2] = (vertex + 2);
            _indices[index + 3] = (vertex + 2);
            _indices[index + 4] = (vertex + 1);
            _indices[index + 5] = (vertex + 3);
        }
        _indexBuffer = new IndexBuffer(GraphicsDevice, IndexElementSize.ThirtyTwoBits, _totalIndices, BufferUsage.WriteOnly);
        _indexBuffer.SetData(_indices);
        _vertexBuffer = new VertexBuffer(GraphicsDevice, VertexPositionColorTexture.VertexDeclaration, _totalVertices, BufferUsage.WriteOnly);
        Matrix viewTranslate = Matrix.CreateTranslation(-1f, -1f, 0f);
        Matrix viewScale = Matrix.CreateScale(2f / (float)screenWidth, 2f / (float)screenHeight, 1f);
        _viewAdapted = Matrix.Multiply(viewScale, viewTranslate);
        _effect = Game.Content.Load<Effect>("Effects/effects");
    }

    public void LoadTextures()
    {
        _squareTx = Game.Content.Load<Texture2D>("Textures/square");
    }

    public override void Update(GameTime gameTime)
    {
        _rotations[10] += 0.01f;
        for (int i = 0; i < _totalSquares; i++)
        {
            int vertex = i * 4;
            int row = (i / _width);
            int col = (i % _width);
            Square s = _squares[row, col];
            float centerX = _squareSize * (float)col + _xOffset;
            float centerY = _squareSize * (float)row + _yOffset;
            float offset = _squareHalf * s.Scale;
            (double sinD, double cosD) = Math.SinCos((double)s.Rotation);
            float sin = (float)sinD;
            float cos = (float)cosD;
            float offsetX = (cos * offset) - (sin * offset);
            float offsetY = (cos * offset) + (sin * offset);
            Color c = _palette.Lookup(s.Color);
            c.A = (byte)(s.Alpha * 255);
            _vertices[vertex + 0].Color = c;
            _vertices[vertex + 1].Color = c;
            _vertices[vertex + 2].Color = c;
            _vertices[vertex + 3].Color = c;
            _vertices[vertex + 0].Position = new Vector3(centerX - offsetX, centerY - offsetY, 0f);
            _vertices[vertex + 1].Position = new Vector3(centerX - offsetY, centerY + offsetX, 0f);
            _vertices[vertex + 2].Position = new Vector3(centerX + offsetY, centerY - offsetX, 0f);
            _vertices[vertex + 3].Position = new Vector3(centerX + offsetX, centerY + offsetY, 0f);
        }
    }

    public override void Draw(GameTime gameTime)
    {
        _effect.CurrentTechnique = _effect.Techniques["Tx"];
        _effect.Parameters["xTexture"].SetValue(_squareTx);
        _effect.Parameters["xTranslate"].SetValue(_viewAdapted);
        _effect.Parameters["xAlpha"].SetValue(1.0f);
        foreach (EffectPass pass in _effect.CurrentTechnique.Passes)
        {
            pass.Apply();
        }
        GraphicsDevice.BlendState = BlendState.Additive;
        _vertexBuffer.SetData(_vertices);
        GraphicsDevice.SetVertexBuffer(_vertexBuffer);
        GraphicsDevice.Indices = _indexBuffer;

        GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, _totalSquares * 2);
    }
}