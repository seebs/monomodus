
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace MonoModus;

// Fastline is like Multiline, but chained like Polyline, but without
// the fancy math.

class Fastline
{
    private ColorCoordinated[] _vertices;
    private int _trailIndex;
    private DynamicVertexBuffer[] _vertexBuffers;
    private IndexBuffer _indexBuffer;
    private Effect _effect;
    private int[] _indices;
    private int _points; // number of points; segments will be one lower
    private int _segments;
    private int _trails, _trailFrames;
    private int _totalVertices, _totalIndices, _totalTriangles;
    private float _thickness;
    private float _thicknessHalf;
    private Matrix _viewAdapted;
    private Modus _modus;

    public int[] Colors;
    public Vector2[] Points;
    private Palette _palette;
    public float[] Alphas;


    public Fastline(Modus game, int points, float thickness, int trails, int trailFrames, Palette palette)
    {
        _modus = game;
        _points = points;
        _segments = _points - 1;
        _thickness = thickness;
        _thicknessHalf = _thickness / 2;
        _palette = palette;
        _trails = trails * trailFrames;
        _trailFrames = trailFrames;
        _trailIndex = 0;
    }

    public void LoadContent(GraphicsDevice gd)
    {
        // Look up the resolution and format of our main backbuffer.
        PresentationParameters pp = gd.PresentationParameters;

        int screenWidth = pp.BackBufferWidth;
        int screenHeight = pp.BackBufferHeight;
        float xScale, yScale;

        bool sideways = screenWidth < screenHeight;

        // we do some initial math in pixels, to try to get a square size
        // which is an integer number of pixels...
        if (sideways)
        {
            xScale = 1.0f;
            yScale = (float)screenWidth / (float)screenHeight;
        }
        else
        {
            yScale = 1.0f;
            xScale = (float)screenHeight / (float)screenWidth;
        }

        Colors = new int[_points];
        Points = new Vector2[_points];
        Alphas = new float[_points];
        _totalVertices = 4 * _segments;
        _totalIndices = 6 * _segments;
        _totalTriangles = 2 * _segments;
        _vertices = new ColorCoordinated[_totalVertices];
        _indices = new int[_totalIndices];

        for (int i = 0; i < _points; i++)
        {
            Points[i] = new Vector2(0, 0);
            Colors[i] = 0;
        }

        //   P0  +--------+ P1
        // [old] +--------+ [new]
        //   P2  +--------+ P3
        for (int i = 0; i < _segments; i++)
        {
            int vx = i * 4;
            int ix = i * 6;
            // top-right
            _indices[ix + 0] = vx + 0;
            _indices[ix + 1] = vx + 1;
            _indices[ix + 2] = vx + 3;
            // bottom-left
            _indices[ix + 3] = vx + 3;
            _indices[ix + 4] = vx + 2;
            _indices[ix + 5] = vx + 0;
        }

        _indexBuffer = new IndexBuffer(gd, IndexElementSize.ThirtyTwoBits, _totalIndices, BufferUsage.WriteOnly);
        _indexBuffer.SetData(_indices);
        _vertexBuffers = new DynamicVertexBuffer[_trails];
        for (int i = 0; i < _trails; i++)
        {
            _vertexBuffers[i] = new DynamicVertexBuffer(gd, ColorCoordinated.VertexDeclaration, _totalVertices, BufferUsage.WriteOnly);
        }
        _viewAdapted = Matrix.CreateScale(xScale, yScale, 1f);
    }

    public void Update(GameTime gameTime)
    {
        // shorter name
        ColorCoordinated[] v = _vertices;
        Vector2 prev = Points[0];
        Vector2 prevColor = new Vector2((float)Colors[0], Alphas[0]);
        for (int i = 1; i < _points; i++)
        {
            int vx = (i - 1) * 4;
            Vector2 next = Points[i];
            Vector2 nextColor = new Vector2((float)Colors[i], Alphas[i]);
            Vector2 delta = next - prev;
            float l = delta.Length();

            v[vx + 0].ColorCoord = prevColor;
            v[vx + 1].ColorCoord = nextColor;
            v[vx + 2].ColorCoord = prevColor;
            v[vx + 3].ColorCoord = nextColor;
            if (l == 0)
            {
                v[vx + 0].Position = prev;
                v[vx + 1].Position = prev;
                v[vx + 2].Position = prev;
                v[vx + 3].Position = prev;
                continue;
            }
            float nx = -delta.Y / l;
            float ny = delta.X / l;
            float hx = nx * _thicknessHalf;
            float hy = ny * _thicknessHalf;
            //   P0  +--------+ P1
            // [old] +--------+ [new]
            //   P2  +--------+ P3
            v[vx + 0].Position = new Vector2(prev.X + hx, prev.Y + hy);
            v[vx + 1].Position = new Vector2(next.X + hx, next.Y + hy);
            v[vx + 2].Position = new Vector2(prev.X - hx, prev.Y - hy);
            v[vx + 3].Position = new Vector2(next.X - hx, next.Y - hy);
            prev = next;
            prevColor = nextColor;
        }
        _vertexBuffers[_trailIndex % _trails].SetData(_vertices, 0, _totalVertices, SetDataOptions.Discard);
        _trailIndex++;
        // we never reuse 0.._trails because those can tell
        // us we've not yet initialized all the trails
        if (_trailIndex > (_trails * 2))
        {
            _trailIndex -= _trails;
        }
    }

    public void Draw(GameTime gameTime, GraphicsDevice gd)
    {
        _effect.CurrentTechnique = _effect.Techniques["Flat"];
        _effect.Parameters["xTranslate"].SetValue(_viewAdapted);
        EffectParameter alphaParam = _effect.Parameters["xAlpha"];
        _effect.Parameters["xPaletteSize"].SetValue((float)_palette.Size());
        gd.BlendState = BlendState.Additive;
        // same index buffer for everything
        gd.Indices = _indexBuffer;
        // we maintain three times as many trails as we're drawing, and draw only
        // every third one.
        for (int i = _trailFrames - 1; i < _trails; i += _trailFrames)
        {
            int idx = _trailIndex - _trails + i;
            if (idx < 0)
            {
                continue;
            }
            float alpha = (float)Math.Sqrt((double)((float)(i + 1) / (float)_trails));
            alphaParam.SetValue(alpha);
            foreach (EffectPass pass in _effect.CurrentTechnique.Passes)
            {
                pass.Apply();
            }
            gd.SetVertexBuffer(_vertexBuffers[idx % _trails]);
            gd.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, _totalTriangles);
        }
    }

    public void LoadTextures(GraphicsDevice gd)
    {
        _effect = _modus.Effect;
    }
}