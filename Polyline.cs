
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
// However, if the normals of two consecutive line segments aren't the
// same, we need to do something fancier. Let's look at a set of three
// line segments:
//                    P7         P8
//                P6--+----------+
//                 /\ |P2        |
//                /  \+----------+ P3
//               /   /\          |
//              /   /  +---------+ P13
//             /   /  /P12
//          P5/   /  /
// P4 +------+   /  /
//    |       \ /  / 
// P0 +--------+  /
//    |      P1|\/P11
// P9 +--------+/
//             P10
//
// We could still do the first and last in fewer triangles, but the
// middle segments always have to be four, because the lines P1-P5 and
// P1-P11 aren't actually aligned, ASCII notwithstanding.
//
// So, in this:
// P0, P4, and P9 are C0. P5, P1, P10, and P11 are C1. P2, P6, P7, and P12
// are C2. This means that for each line segment, we're adding four new
// points, and reusing two of the points from the previous segment. We're
// also adding five triangles total -- the four for the segment, plus one
// for the bezel.
//
// It's just not worth trying to optimize the first-and-last segments more,
// so we'll use the same logic for them.
//
// However, this does mean that we may not be able to preload all of the
// indexes -- we don't know *which* previous points the bezel is reusing.


class Polyline
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


    public Polyline(Modus game, int points, float thickness, int trails, int trailFrames, Palette palette)
    {
        _game = game;
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
        _totalVertices = 6 * _segments;
        _totalIndices = 18 * _segments;
        _totalTriangles = 6 * _segments;
        _vertices = new VertexPositionColor[_totalVertices];
        _indices = new int[_totalIndices];

        for (int i = 0; i < _points; i++)
        {
            Points[i] = new Vector2(0, 0);
            Colors[i] = 0;
        }

        int pp3 = 0;
        int pp5 = 0;
        // [PP3] P2 +--------+ P3
        //       P0 +--------+ P1
        // [PP5] P4 +--------+ P5
        for (int i = 0; i < _segments; i++)
        {
            int vx = i * 6;
            int ix = i * 18;
            // "top" bezel
            _indices[ix + 0] = pp3;
            _indices[ix + 1] = vx + 2;
            _indices[ix + 2] = vx + 0;
            // "bottom" bezel
            _indices[ix + 3] = pp5;
            _indices[ix + 4] = vx + 0;
            _indices[ix + 5] = vx + 4;
            // top-left
            _indices[ix + 6] = vx + 0;
            _indices[ix + 7] = vx + 2;
            _indices[ix + 8] = vx + 3;
            // top-right
            _indices[ix + 9] = vx + 3;
            _indices[ix + 10] = vx + 1;
            _indices[ix + 11] = vx + 0;
            // bottom-left
            _indices[ix + 12] = vx + 4;
            _indices[ix + 13] = vx + 0;
            _indices[ix + 14] = vx + 1;
            // top-right
            _indices[ix + 15] = vx + 1;
            _indices[ix + 16] = vx + 5;
            _indices[ix + 17] = vx + 4;

            pp3 = vx + 3;
            pp5 = vx + 5;
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
        float prevTheta;
        {
            float dx = Points[1].X - Points[0].X;
            float dy = Points[1].Y - Points[0].Y;
            prevTheta = (float)Math.Atan2(dx, dy);
        }
        Vector2 prev = Points[0];
        Color prevColor = _palette.Lookup(Colors[0]);
        prevColor.A = (byte)(Alphas[0] * 255);
        float prevHx = 0, prevHy = 0;
        // shorter name
        VertexPositionColor[] v = _vertices;
        for (int i = 0; i < _segments; i++)
        {
            int vx = i * 6;
            Vector2 next = Points[i + 1];
            Color nextColor = _palette.Lookup(Colors[i + 1]);
            nextColor.A = (byte)(Alphas[i + 1] * 255);
            float dx = next.X - prev.X;
            float dy = next.Y - prev.Y;
            float theta = (float)Math.Atan2(dx, dy);

            float l2 = (dx * dx) + (dy * dy);
            v[vx + 0].Color = prevColor;
            v[vx + 1].Color = nextColor;
            v[vx + 2].Color = prevColor;
            v[vx + 3].Color = nextColor;
            v[vx + 4].Color = prevColor;
            v[vx + 5].Color = nextColor;
            if (l2 == 0)
            {
                v[vx + 0].Position = new Vector3(prev, 0);
                v[vx + 1].Position = new Vector3(prev, 0);
                v[vx + 2].Position = new Vector3(prev, 0);
                v[vx + 3].Position = new Vector3(prev, 0);
                v[vx + 4].Position = new Vector3(prev, 0);
                v[vx + 5].Position = new Vector3(prev, 0);
                prev = next;
                prevColor = nextColor;
                prevTheta = theta;
                continue;
            }
            float l = (float)Math.Sqrt(l2);
            float nx = -dy / l;
            float ny = dx / l;
            float hx = nx * _thicknessHalf;
            float hy = ny * _thicknessHalf;
            // P2 +--------+ P3
            // P0 +--------+ P1
            // P4 +--------+ P5
            v[vx + 0].Position = new Vector3(prev, 0);
            v[vx + 1].Position = new Vector3(next, 0);
            v[vx + 2].Position = new Vector3(prev.X + hx, prev.Y + hy, 0);
            v[vx + 3].Position = new Vector3(next.X + hx, next.Y + hy, 0);
            v[vx + 4].Position = new Vector3(prev.X - hx, prev.Y - hy, 0);
            v[vx + 5].Position = new Vector3(next.X - hx, next.Y - hy, 0);

            float dt = theta - prevTheta;
            if (dt < 0)
            {
                dt += (float)Math.Tau;
            }
            bool left = false;
            if (dt > (float)Math.PI)
            {
                left = true;
                dt -= (float)Math.PI;
            }
            float sharp = (float)(Math.PI / 2 - (Math.Abs(dt - (Math.PI / 2))));
            float scale = (float)(Math.Tan(sharp / 2));
            // all of this might seem weirdly backwards; why are we using
            // hy with x, and hx with y? the answer is that hx/hy are the
            // *normal* vector, with nx = -y and ny = x, scaled to unit
            // length and then by half thickness,. but we want to
            // move things along the *original* vector.
            if (left)
            {
                // we're bending "left". we want
                // to adjust our P2, and the previous entry's
                // P3, to be a point between where they'd
                // have been...
                if (vx > 0)
                {
                    v[vx - 3].Position.X -= prevHy * scale;
                    v[vx - 3].Position.Y += prevHx * scale;
                }
                v[vx + 2].Position.X += hy * scale;
                v[vx + 2].Position.Y -= hx * scale;
            }
            else if (dt < Math.PI)
            {
                // we're bending "right", so we want to
                // adjust our P4, and the previous entry's
                // P5.
                if (vx > 0)
                {
                    v[vx - 1].Position.X -= prevHy * scale;
                    v[vx - 1].Position.Y += prevHx * scale;
                }
                v[vx + 4].Position.X += hy * scale;
                v[vx + 4].Position.Y -= hx * scale;
            }
            else
            {
                // we're not bending, so we can just ignore that.
            }

            prev = next;
            prevColor = nextColor;
            prevTheta = theta;
            prevHx = hx;
            prevHy = hy;
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