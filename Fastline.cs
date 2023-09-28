
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
        int vx = 0;
        for (int i = 1; i < _points; i++)
        {
            Vector2 next = Points[i];
            Vector2 nextColor = new Vector2((float)Colors[i], Alphas[i]);
            Vector2 delta = next - prev;
            float l = _thicknessHalf / delta.Length();


            if (l == 0)
            {
                // set positions, but not colors, because the degenerate positions
                // mean the colors don't matter
                v[vx + 0].Position = prev;
                v[vx + 1].Position = prev;

                v[vx + 2].Position = prev;

                v[vx + 3].Position = prev;

                vx += 4;
                continue;
            }
            Vector2 h = new Vector2(-delta.Y * l, delta.X * l);

            //   P0  +--------+ P1
            // [old] +--------+ [new]
            //   P2  +--------+ P3
            v[vx + 0].Position = prev + h;
            v[vx + 0].ColorCoord = prevColor;
            v[vx + 1].Position = next + h;
            v[vx + 1].ColorCoord = nextColor;
            v[vx + 2].Position = prev - h;
            v[vx + 2].ColorCoord = prevColor;
            v[vx + 3].Position = next - h;
            v[vx + 3].ColorCoord = nextColor;

            prev = next;
            prevColor = nextColor;
            vx += 4;
        }
        _vertexBuffers[_trailIndex % _trails].SetData(_vertices, 0, _totalVertices, SetDataOptions.None);
        _trailIndex++;
        // we never reuse 0.._trails because those can tell
        // us we've not yet initialized all the trails
        if (_trailIndex > 1)
        {
            _trailIndex = 0;
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

        int idx = 1 - _trailIndex;
        if (idx >= 0)
        {
            alphaParam.SetValue(1.0f);
            _effect.CurrentTechnique.Passes[0].Apply();
            gd.SetVertexBuffer(_vertexBuffers[idx]);
            gd.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, _totalTriangles);
        }

    }


    public void LoadTextures(GraphicsDevice gd)
    {
        _effect = _modus.Effect;
    }
}