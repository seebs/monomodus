
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace MonoModus;

struct Square
{
    public int Color;
    public Vector2 Offset;
    public Vector2 Velocity;
    public float Alpha, Scale, Rotation;
}

class Squares : DrawableGameComponent
{
    private Texture2D _squareTx;

    private VertexPositionColorTexture[] _vertices;
    private Effect _effect;
    private IndexBuffer _indexBuffer;
    private VertexBuffer _vertexBuffer;
    private int[] _indices;
    private int _scale; // the smaller of our width-or-height
    private int _width, _height, _totalSquares, _totalVertices, _totalIndices;
    private float _xOffset, _yOffset;
    private int _squarePx;
    private float _squareSize;
    private float _squareHalf;
    private Matrix _viewAdapted;
    private Palette _palette;
    private Modus _game;

    public Square[,] S;

    public Squares(Modus game, int scale, Palette palette)
            : base(game)
    {
        _game = game;
        _scale = scale;
        _palette = palette;
    }

    public (int row, int col, bool ok) SquareAt(Vector2 pos)
    {
        int col = (int)Math.Floor((double)((pos.X - _xOffset) / _squareSize) + 0.5);
        int row = (int)Math.Floor((double)((pos.Y - _yOffset) / _squareSize) + 0.5);
        return (row, col, (row >= 0 && row < _height && col >= 0 && col < _width));
    }

    // handle velocity-and-snap-back for a square
    private void squareSnap(int row, int col)
    {
        Square s = S[row, col];
        // if we're off center, adjust our velocity towards the center:
        // move in the direction we're moving...
        s.Velocity *= 0.95f;
        s.Velocity -= s.Offset / 4;
        s.Offset += s.Velocity;
        S[row, col] = s;
    }

    protected override void LoadContent()
    {
        // Look up the resolution and format of our main backbuffer.
        PresentationParameters pp = GraphicsDevice.PresentationParameters;

        int screenWidth = pp.BackBufferWidth;
        int screenHeight = pp.BackBufferHeight;
        float smaller;
        float xScale, yScale;

        bool sideways = screenWidth < screenHeight;

        // we do some initial math in pixels, to try to get a square size
        // which is an integer number of pixels...
        if (sideways)
        {
            _width = _scale;
            _squarePx = screenWidth / _width;
            _height = screenHeight / _squarePx;
            smaller = (float)screenWidth;

            xScale = 1.0f;
            yScale = (float)screenWidth / (float)screenHeight;
        }
        else
        {
            _height = _scale;
            _squarePx = screenHeight / _height;
            _width = screenWidth / _squarePx;
            smaller = (float)screenHeight;

            yScale = 1.0f;
            xScale = (float)screenHeight / (float)screenWidth;
        }
        S = new Square[_height, _width];
        _totalSquares = _width * _height;
        _totalVertices = _totalSquares * 4;
        _totalIndices = _totalSquares * 6;

        // we want to be offset by half the size of the grid, but also we want to
        // center on a hypothetical median square, which means we want to be half
        // a square off, which we do by being a square off before we compute the
        // half.
        _xOffset = (float)((_width - 1) * _squarePx) / -2;
        _yOffset = (float)((_height - 1) * _squarePx) / -2;

        // okay, but actually what we *want* to do is do everything in units,
        // so that the smaller dimension of the screen is -1/+1. (not sure why
        // this is coming out with /smaller instead of /(smaller/2)...)
        _squareSize = (float)_squarePx / (smaller / 2);
        _xOffset /= (smaller / 2);
        _yOffset /= (smaller / 2);
        _squareHalf = (float)_squareSize / 2;

        _vertices = new VertexPositionColorTexture[_totalVertices];
        _indices = new int[_totalIndices];
        for (int i = 0; i < _totalSquares; i++)
        {
            int vertex = i * 4;
            int index = i * 6;
            int row = (i / _width);
            int col = (i % _width);
            S[row, col].Color = (i % _palette.Size());
            S[row, col].Alpha = 1.0f;
            S[row, col].Scale = 1.0f;
            S[row, col].Rotation = 0f;
            S[row, col].Offset = new Vector2(0f, 0f);
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
        _viewAdapted = Matrix.CreateScale(xScale, yScale, 1.0f);
        _effect = Game.Content.Load<Effect>("Effects/effects");
    }

    public void LoadTextures()
    {
        _squareTx = Game.Content.Load<Texture2D>("Textures/square");
    }

    public override void Update(GameTime gameTime)
    {
        for (int i = 0; i < _totalSquares; i++)
        {
            int vertex = i * 4;
            int row = (i / _width);
            int col = (i % _width);
            Square s = S[row, col];
            if (s.Offset != Vector2.Zero || s.Velocity != Vector2.Zero)
            {
                squareSnap(row, col);
                s = S[row, col];
            }
            float centerX = _squareSize * ((float)col + s.Offset.X) + _xOffset;
            float centerY = _squareSize * ((float)row + s.Offset.Y) + _yOffset;
            float offset = _squareHalf * s.Scale;
            (double sinD, double cosD) = Math.SinCos((double)s.Rotation);
            float sin = (float)sinD;
            float cos = (float)cosD;
            float cornerX = (cos * offset) - (sin * offset);
            float cornerY = (cos * offset) + (sin * offset);
            Color c = _palette.Lookup(s.Color);
            c.A = (byte)(s.Alpha * 255);
            _vertices[vertex + 0].Color = c;
            _vertices[vertex + 1].Color = c;
            _vertices[vertex + 2].Color = c;
            _vertices[vertex + 3].Color = c;
            _vertices[vertex + 0].Position = new Vector3(centerX - cornerX, centerY - cornerY, 0f);
            _vertices[vertex + 1].Position = new Vector3(centerX - cornerY, centerY + cornerX, 0f);
            _vertices[vertex + 2].Position = new Vector3(centerX + cornerY, centerY - cornerX, 0f);
            _vertices[vertex + 3].Position = new Vector3(centerX + cornerX, centerY + cornerY, 0f);
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