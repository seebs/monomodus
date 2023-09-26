using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

public class Oversaturator : DrawableGameComponent
{
    private RenderTarget2D _renderTarget, _highlights, _blur1, _blur2;
    private VertexBuffer _vertexBuffer;
    private IndexBuffer _indexBuffer;
    private Effect _effect;
    private Matrix _primaryTranslate, _fullTranslate;
    private Matrix[] _secondaryTranslates;
    private EffectParameter _textureParam, _translateParam, _scaleParam, _highlightParam, _blurParam;
    private Vector2 _screenSizeRecip;
    private int _primaryDisplay;
    private Texture2D _unused;

    private bool _debugging;

    public void Debug(bool enabled)
    {
        _debugging = enabled;
    }

    public void SetPrimary(int i)
    {
        _primaryDisplay = i;
    }


    public Oversaturator(Game game) : base(game)
    {

    }

    protected override void LoadContent()
    {

        PresentationParameters pp = GraphicsDevice.PresentationParameters;

        int width = pp.BackBufferWidth;
        int height = pp.BackBufferHeight;
        _screenSizeRecip = new Vector2(1 / (float)width, 1 / (float)height);

        // _unused = new Texture2D(GraphicsDevice, 64, 64);
        _renderTarget = new RenderTarget2D(GraphicsDevice, width, height, false,
                                                   SurfaceFormat.HdrBlendable, DepthFormat.None, 1,
                                                   RenderTargetUsage.DiscardContents);
        // _unused = new Texture2D(GraphicsDevice, 64, 64);
        _highlights = new RenderTarget2D(GraphicsDevice, width, height, false, pp.BackBufferFormat, DepthFormat.None, 1, RenderTargetUsage.PreserveContents);
        _blur1 = new RenderTarget2D(GraphicsDevice, width / 2, height / 2, false, pp.BackBufferFormat, DepthFormat.None, 1, RenderTargetUsage.PreserveContents);
        _blur2 = new RenderTarget2D(GraphicsDevice, width / 2, height / 2, false, pp.BackBufferFormat, DepthFormat.None, 1, RenderTargetUsage.PreserveContents);

        int[] _indices;
        VertexPosition[] _vertices;
        _indices = new int[6];
        _indices[0] = 0;
        _indices[1] = 1;
        _indices[2] = 2;
        _indices[3] = 2;
        _indices[4] = 1;
        _indices[5] = 3;
        _indexBuffer = new IndexBuffer(GraphicsDevice, IndexElementSize.ThirtyTwoBits, 6, BufferUsage.WriteOnly);
        _indexBuffer.SetData(_indices);
        _vertexBuffer = new VertexBuffer(GraphicsDevice, VertexPosition.VertexDeclaration, 4, BufferUsage.WriteOnly);
        _vertices = new VertexPosition[4];
        _vertices[0].Position = new Vector3(-1, -1, 0);
        _vertices[1].Position = new Vector3(-1, 1, 0);
        _vertices[2].Position = new Vector3(1, -1, 0);
        _vertices[3].Position = new Vector3(1, 1, 0);
        _vertexBuffer.SetData(_vertices);
        _effect = Game.Content.Load<Effect>("Effects/oversaturate");
        _textureParam = _effect.Parameters["xTexture"];
        _translateParam = _effect.Parameters["xTranslate"];
        _scaleParam = _effect.Parameters["xScale"];
        _highlightParam = _effect.Parameters["xHighlightTexture"];
        _blurParam = _effect.Parameters["xBlurTexture"];
        // primary: .7 screen, centered-low
        Matrix viewTranslate = Matrix.CreateTranslation(0.3f, -0.3f, 0f);
        Matrix viewScale = Matrix.CreateScale(0.7f, 0.7f, 1f);
        _primaryTranslate = Matrix.Multiply(viewScale, viewTranslate);

        // secondary: .3 screen, top left
        _secondaryTranslates = new Matrix[4];
        for (int i = 0; i < 4; i++)
        {
            if (i < 3)
            {
                float x = (float)(i - 1) * 0.7f;
                viewTranslate = Matrix.CreateTranslation(x, +0.7f, 0f);
                viewScale = Matrix.CreateScale(0.3f, 0.3f, 1f);
                _secondaryTranslates[i] = Matrix.Multiply(viewScale, viewTranslate);
            }
            else
            {
                float y = (float)(i - 3) * 0.35f;
                viewTranslate = Matrix.CreateTranslation(-0.7f, y, 0f);
                viewScale = Matrix.CreateScale(0.3f, 0.3f, 1f);
                _secondaryTranslates[i] = Matrix.Multiply(viewScale, viewTranslate);
            }
        }

        // full screen: no translate, no scale
        _fullTranslate = Matrix.Identity;

        // show the final product largest
        _primaryDisplay = 4;
    }

    public void RenderHere()
    {
        GraphicsDevice.SetRenderTarget(_renderTarget);
        GraphicsDevice.Clear(Color.Black);
    }

    private void DrawFromToUsing(RenderTarget2D src, RenderTarget2D dst, string technique, int translationIndex)
    {
        GraphicsDevice.SetRenderTarget(dst);
        _effect.CurrentTechnique = _effect.Techniques[technique];
        _textureParam.SetValue(src);
        if (translationIndex == -1)
        {
            _translateParam.SetValue(_fullTranslate);
        }
        else
        if (translationIndex == _primaryDisplay)
        {
            _translateParam.SetValue(_primaryTranslate);
        }
        else
        {
            if (translationIndex > _primaryDisplay)
            {
                _translateParam.SetValue(_secondaryTranslates[translationIndex - 1]);
            }
            else
            {
                _translateParam.SetValue(_secondaryTranslates[translationIndex]);
            }
        }
        _effect.CurrentTechnique.Passes[0].Apply();
        GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, 2);
    }
    public override void Draw(GameTime gameTime)
    {
        // we believe these will just get reused by everything hereafter
        GraphicsDevice.SetVertexBuffer(_vertexBuffer);
        GraphicsDevice.Indices = _indexBuffer;
        _scaleParam.SetValue(_screenSizeRecip);

        // switch back to opaque rendering for this part... we need to zero out parts
        // of _highlights that may have had colors previously.
        GraphicsDevice.BlendState = BlendState.Opaque;
        DrawFromToUsing(_renderTarget, _highlights, "ExtractHighlight", -1);
        DrawFromToUsing(_renderTarget, _blur1, "BlurX", -1);
        DrawFromToUsing(_blur1, _blur2, "BlurY", -1);
        _highlightParam.SetValue(_highlights);
        _blurParam.SetValue(_blur2);

        GraphicsDevice.SetRenderTarget(null);
        GraphicsDevice.Clear(Color.Gray);

        if (_debugging)
        {
            DrawFromToUsing(_renderTarget, null, "Flat", 0);
            DrawFromToUsing(_highlights, null, "Flat", 1);
            DrawFromToUsing(_blur1, null, "Flat", 2);
            DrawFromToUsing(_blur2, null, "Flat", 3);
            DrawFromToUsing(_renderTarget, null, "Combine", 4);
        }
        else
        {
            DrawFromToUsing(_renderTarget, null, "Combine", -1);
        }
    }
}