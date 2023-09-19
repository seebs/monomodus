using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

public class Oversaturator : DrawableGameComponent
{
    private RenderTarget2D _renderTarget;
    private VertexBuffer _vertexBuffer;
    private IndexBuffer _indexBuffer;
    private Effect _effect;
    private SpriteBatch _spriteBatch;


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
                                                   SurfaceFormat.HalfVector4, pp.DepthStencilFormat, pp.MultiSampleCount,
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
        _effect.CurrentTechnique = _effect.Techniques["Desat"];
        _effect.Parameters["xTexture"].SetValue(_renderTarget);
    }

    public void RenderHere()
    {
        GraphicsDevice.SetRenderTarget(_renderTarget);
        GraphicsDevice.Clear(Color.Black);
    }

    public override void Draw(GameTime gameTime)
    {
        GraphicsDevice.SetRenderTarget(null);
        GraphicsDevice.BlendState = BlendState.Opaque;
        foreach (EffectPass pass in _effect.CurrentTechnique.Passes)
        {
            pass.Apply();
        }

        GraphicsDevice.SetVertexBuffer(_vertexBuffer);
        GraphicsDevice.Indices = _indexBuffer;
        GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, 2);
    }
}