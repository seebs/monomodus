using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

public class Oversaturator : DrawableGameComponent
{
    private RenderTarget2D _renderTarget, _highlights;
    private VertexBuffer _vertexBuffer;
    private IndexBuffer _indexBuffer;
    private Effect _effect;
    private SpriteBatch _spriteBatch;
    private Matrix _primaryTranslate, _fullTranslate;
    private Matrix[] _secondaryTranslates;
    private EffectParameter _textureParam, _translateParam;

    private bool _debugging;

    public void Debug(bool enabled)
    {
        _debugging = enabled;
    }


    public Oversaturator(Game game) : base(game)
    {

    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        PresentationParameters pp = GraphicsDevice.PresentationParameters;

        int width = pp.BackBufferWidth;
        int height = pp.BackBufferHeight;

        _renderTarget = new RenderTarget2D(GraphicsDevice, width, height, false,
                                                   SurfaceFormat.HalfVector4, DepthFormat.None, 1,
                                                   RenderTargetUsage.PreserveContents);
        _highlights = new RenderTarget2D(GraphicsDevice, width, height, false,
pp.BackBufferFormat, DepthFormat.None, 1,
RenderTargetUsage.PreserveContents);
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

        // primary: .7 screen, centered-low
        Matrix viewTranslate = Matrix.CreateTranslation(0f, -0.3f, 0f);
        Matrix viewScale = Matrix.CreateScale(0.7f, 0.7f, 1f);
        _primaryTranslate = Matrix.Multiply(viewScale, viewTranslate);

        // secondary: .3 screen, top left
        _secondaryTranslates = new Matrix[3];
        for (int i = 0; i < 3; i++)
        {
            float x = (float)(i - 1) * 0.7f;
            viewTranslate = Matrix.CreateTranslation(x, +0.7f, 0f);
            viewScale = Matrix.CreateScale(0.3f, 0.3f, 1f);
            _secondaryTranslates[i] = Matrix.Multiply(viewScale, viewTranslate);
        }

        // full screen: no translate, no scale
        _fullTranslate = Matrix.Identity;
    }

    public void RenderHere()
    {
        GraphicsDevice.SetRenderTarget(_renderTarget);
        GraphicsDevice.Clear(Color.Black);
    }

    private void DrawFromToUsing(RenderTarget2D src, RenderTarget2D dst, string technique, Matrix translation)
    {
        GraphicsDevice.SetRenderTarget(dst);
        _effect.CurrentTechnique = _effect.Techniques[technique];
        _textureParam.SetValue(src);
        _translateParam.SetValue(translation);
        _effect.CurrentTechnique.Passes[0].Apply();
        GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, 2);
    }
    public override void Draw(GameTime gameTime)
    {
        // we believe these will just get reused by everything hereafter
        GraphicsDevice.SetVertexBuffer(_vertexBuffer);
        GraphicsDevice.Indices = _indexBuffer;

        // switch back to opaque rendering for this part... we need to zero out parts
        // of _highlights that may have had colors previously.
        GraphicsDevice.BlendState = BlendState.Opaque;
        DrawFromToUsing(_renderTarget, _highlights, "ExtractHighlight", _fullTranslate);

        GraphicsDevice.SetRenderTarget(null);
        GraphicsDevice.Clear(Color.Gray);

        if (_debugging)
        {
            DrawFromToUsing(_renderTarget, null, "Flat", _secondaryTranslates[0]);
            DrawFromToUsing(_highlights, null, "Flat", _secondaryTranslates[2]);
            DrawFromToUsing(_renderTarget, null, "Desat", _primaryTranslate);
        }
        else
        {
            DrawFromToUsing(_renderTarget, null, "Desat", _fullTranslate);
        }
    }
}