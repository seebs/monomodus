
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
    private DynamicVertexBuffer _vertexBuffer;
    private IndexBuffer _indexBuffer;
    private Effect _effect;
    private int[] _indices;
    private int _depth;
    private int[] _points; // number of points; segments will be one lower
    private int _totalPoints;
    private int _segments;
    // the scale of our display
    private float _xScale, _yScale;
    private int _totalVertices, _totalIndices, _totalTriangles, _currentVertices;
    private float[] _thickness;
    private float[] _thicknessHalf;
    private Matrix _viewAdapted;
    private Modus _modus;

    public Vector2[] Points;
    private Palette _palette;
    public Vector2[] ColorAlphas;

    public Memory<Vector2> PointRef(int depth)
    {
        int start = 0;
        int size = 0;
        for (int i = 0; i <= depth; i++)
        {
            start += size;
            size = _points[i];
        }
        return new Memory<Vector2>(Points, start, size);
    }

    public Memory<Vector2> ColorRef(int depth)
    {
        int start = 0;
        int size = 0;
        for (int i = 0; i <= depth; i++)
        {
            start += size;
            size = _points[i];
        }
        return new Memory<Vector2>(ColorAlphas, start, size);
    }


    public Fastline(Modus game, int[] points, float[] thickness, Palette palette)
    {
        _modus = game;
        _points = points;
        _segments = 0;
        _totalPoints = 0;
        _depth = _points.Length;
        for (int i = 0; i < _depth; i++)
        {
            _totalPoints += _points[i];
            _segments += _points[i] - 1;
        }
        _thickness = thickness;
        _thicknessHalf = new float[_depth];
        for (int i = 0; i < _depth; i++)
        {
            _thicknessHalf[i] = _thickness[i] / 2;
        }
        Points = new Vector2[_totalPoints];
        ColorAlphas = new Vector2[_totalPoints];
        _palette = palette;
    }

    public void LoadContent(GraphicsDevice gd)
    {
        // Look up the resolution and format of our main backbuffer.
        PresentationParameters pp = gd.PresentationParameters;

        int screenWidth = pp.BackBufferWidth;
        int screenHeight = pp.BackBufferHeight;

        bool sideways = screenWidth < screenHeight;

        // we do some initial math in pixels, to try to get a square size
        // which is an integer number of pixels...
        if (sideways)
        {
            _xScale = 1.0f;
            _yScale = (float)screenWidth / (float)screenHeight;
        }
        else
        {
            _yScale = 1.0f;
            _xScale = (float)screenHeight / (float)screenWidth;
        }
        _totalVertices = 4 * _segments;
        _totalIndices = 6 * _segments;
        _totalTriangles = 2 * _segments;
        _vertices = new ColorCoordinated[_totalVertices];
        _indices = new int[_totalIndices];

        for (int i = 0; i < _totalPoints; i++)
        {
            Points[i] = new Vector2(0, 0);
            ColorAlphas[i] = new Vector2(0, 1);
        }
        Points[1].X = 1;

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
        _vertexBuffer = new DynamicVertexBuffer(gd, ColorCoordinated.VertexDeclaration, _totalVertices, BufferUsage.WriteOnly);
        _viewAdapted = Matrix.CreateScale(_xScale, _yScale, 1f);
        _effect = _modus.Effect;
    }

    public void Update(GameTime gameTime, int from, int to, bool adaptView)
    {
        // shorter name
        ColorCoordinated[] v = _vertices;
        float minX, minY, maxX, maxY;

        int vx = 0;
        int n = 0;
        if (to > _depth)
        {
            to = _depth;
        }
        for (int i = 0; i < from; i++)
        {
            n += _points[i];
        }
        minX = Points[n].X;
        maxX = Points[n].X;
        minY = Points[n].Y;
        maxY = Points[n].Y;
        for (int i = from; i < to; i++)
        {
            Vector2 prev = Points[n];
            Vector2 prevColor = ColorAlphas[n];
            float th = _thicknessHalf[i];
            n++;
            if (prev.X < minX)
            {
                minX = prev.X;
            }
            if (prev.X > maxX)
            {
                maxX = prev.X;
            }
            if (prev.Y < minY)
            {
                minY = prev.Y;
            }
            if (prev.Y > maxY)
            {
                maxY = prev.Y;
            }
            for (int j = 1; j < _points[i]; j++)
            {
                Vector2 next = Points[n];
                Vector2 nextColor = ColorAlphas[n];
                n++;
                Vector2 delta = next - prev;
                float l = th / delta.Length();
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
                if (next.X < minX)
                {
                    minX = next.X;
                }
                if (next.X > maxX)
                {
                    maxX = next.X;
                }
                if (next.Y < minY)
                {
                    minY = next.Y;
                }
                if (next.Y > maxY)
                {
                    maxY = next.Y;
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
        }
        if (adaptView)
        {
            // target display is 2/_xScale by 2/_yScale and one of the scales is 1,
            // and the other is the inverse of the aspect ratio. e.g., for 4:3,
            // the dimension that's 4 gets an 0.75 scale. (So that +/- 1 is the
            // largest square possible on the display.)
            //
            // Whatever scale we use, we continue to want the _xScale/_yScale
            // multipliers so that we get the right aspect ratio.
            //
            // if xSize*xScale > ySize*yScale, we want to use a ratio derived
            // from making xSize fit. (the more xSize goes up, after that, the
            // smaller we want to be, and we don't care what ySize does.)
            //
            // 
            float xSize = maxX - minX;
            minX -= xSize * .02f;
            maxX += xSize * .02f;
            xSize = maxX - minX;
            float ySize = maxY - minY;
            minY -= ySize * .02f;
            maxY += ySize * .02f;
            ySize = maxY - minY;
            float xRatio = xSize * _xScale;
            float yRatio = ySize * _yScale;
            float ratio;
            float xOffset, yOffset;
            if (xRatio > yRatio)
            {
                // compute something so that xSize * ratio * xScale comes out to 2...
                // xSize * ratio * xScale = 2
                // ratio = 2 / (xSize * xScale)
                // => ratio = 2 / xRatio
                // we then end up using an X-axis scale of (ratio * _xScale), and Y-axis
                // of (ratio * _yScale). if you have two points which are separated
                // by xSize, and you multiply by (2/xSize*xScale)*xScale, you get
                // xSize * 2/xSize, which is 2, so they are 2 units apart, which
                // puts them at opposite ends of the screen.
                ratio = 2 / xRatio;
                // we always want minX to come out to -1, *not* -1/_xScale, because
                // the translation is being applied after all the scaling. since
                // every point is being multiplied by ratio*xScale, we want
                // xOffset to be -1 - (minX * ratio * xScale).
                xOffset = -1 - (minX * ratio * _xScale);
                // if xRatio > yRatio, y won't actually reach -1. instead,
                // we want to think about how much of the adjusted final space
                // we fill, subtract that from 2, and add half of that to
                // be centered.
                float adjustedYsize = ySize * ratio * _yScale;
                float spaceLeft = (2 - adjustedYsize);
                yOffset = -1 - (minY * ratio * _yScale) + (spaceLeft / 2);
            }
            else
            {
                ratio = 2 / yRatio;
                yOffset = -1 - (minY * ratio * _yScale);
                float adjustedXsize = xSize * ratio * _xScale;
                float spaceLeft = (2 - adjustedXsize);
                xOffset = -1 - (minX * ratio * _xScale) + (spaceLeft / 2);
            }
            Matrix scaleMatrix = Matrix.CreateScale(ratio * _xScale, ratio * _yScale, 1);

            Matrix translateMatrix = Matrix.CreateTranslation(xOffset, yOffset, 0);
            // but what about translation? given this scale we've got, we now want to
            // move xMin to -1, 
            _viewAdapted = Matrix.Multiply(scaleMatrix, translateMatrix);
        }
        _vertexBuffer.SetData(_vertices, 0, vx, SetDataOptions.None);
        _currentVertices = vx;
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
        alphaParam.SetValue(1.0f);
        _effect.CurrentTechnique.Passes[0].Apply();
        gd.SetVertexBuffer(_vertexBuffer);
        gd.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, _currentVertices / 2);
    }


    public void LoadTextures(GraphicsDevice gd)
    {
        _effect = _modus.Effect;
    }
}