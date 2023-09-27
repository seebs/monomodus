
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace MonoModus;

// A single line segment is easy: You find the line's normal and pick
// points half a thickness out from it in that direction from both ends,
// giving you four points, which are a quad. So, given P0/P1 as the
// actual ends of the segment:
//
// P2 +--------+ P3
// P0 +--------+ P1
// P4 +--------+ P5
//
// we have two triangles: P2-P3-P5, P5-P4-P2. Easy!
//
// For a Multiline, we just always treat pairs of points as
// defining segments.

class Multiline
{
    private VertexPositionColor[] _vertices;
    private int _trailIndex;
    private VertexBuffer[] _vertexBuffers;
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
    private Modus _game;

    public int[] Colors;
    public Vector2[] Points;
    private Palette _palette;
    public float[] Alphas;


    public Multiline(Modus game, int segments, float thickness, int trails, int trailFrames, Palette palette)
    {
        _game = game;
        _segments = segments;
        _points = _segments * 2;
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
        _vertices = new VertexPositionColor[_totalVertices];
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
        _vertexBuffers = new VertexBuffer[_trails];
        for (int i = 0; i < _trails; i++)
        {
            _vertexBuffers[i] = new VertexBuffer(gd, VertexPositionColor.VertexDeclaration, _totalVertices, BufferUsage.WriteOnly);
        }
        _viewAdapted = Matrix.CreateScale(xScale, yScale, 1f);

        _effect = _game.Content.Load<Effect>("Effects/effects");
        _effect.CurrentTechnique = _effect.Techniques["Flat"];
        _effect.Parameters["xTranslate"].SetValue(_viewAdapted);
    }

    public void Update(GameTime gameTime)
    {
        // shorter name
        VertexPositionColor[] v = _vertices;
        for (int i = 0; i < _points; i += 2)
        {
            int vx = i * 2;
            Vector2 prev = Points[i];
            Vector2 next = Points[i + 1];
            Color prevColor = _palette.Lookup(Colors[i + 0]);
            Color nextColor = _palette.Lookup(Colors[i + 1]);
            nextColor.A = (byte)(Alphas[i + 1] * 255);
            prevColor.A = (byte)(Alphas[i + 0] * 255);
            float dx = next.X - prev.X;
            float dy = next.Y - prev.Y;

            float l2 = (dx * dx) + (dy * dy);
            v[vx + 0].Color = prevColor;
            v[vx + 1].Color = nextColor;
            v[vx + 2].Color = prevColor;
            v[vx + 3].Color = nextColor;
            if (l2 == 0)
            {
                v[vx + 0].Position = new Vector3(prev, 0);
                v[vx + 1].Position = new Vector3(prev, 0);
                v[vx + 2].Position = new Vector3(prev, 0);
                v[vx + 3].Position = new Vector3(prev, 0);
                continue;
            }
            float l = (float)Math.Sqrt(l2);
            float nx = -dy / l;
            float ny = dx / l;
            float hx = nx * _thicknessHalf;
            float hy = ny * _thicknessHalf;
            //   P0  +--------+ P1
            // [old] +--------+ [new]
            //   P2  +--------+ P3
            v[vx + 0].Position = new Vector3(prev.X + hx, prev.Y + hy, 0);
            v[vx + 1].Position = new Vector3(next.X + hx, next.Y + hy, 0);
            v[vx + 2].Position = new Vector3(prev.X - hx, prev.Y - hy, 0);
            v[vx + 3].Position = new Vector3(next.X - hx, next.Y - hy, 0);
        }
        _vertexBuffers[_trailIndex % _trails].SetData(_vertices);
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
}