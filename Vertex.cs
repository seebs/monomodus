
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace MonoModus;

public struct ColorCoordinated
{
    public Vector2 Position;
    public Vector2 ColorCoord;

    public ColorCoordinated(Vector2 position, Vector2 colorCoord)
    {
        this.Position = position;
        this.ColorCoord = colorCoord;
    }

    public readonly static VertexDeclaration VertexDeclaration = new VertexDeclaration
    (
        new VertexElement(0, VertexElementFormat.Vector2, VertexElementUsage.Position, 0),
        new VertexElement(sizeof(float) * 2, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0)
    );
}

public struct ColorCoordinatedTx
{
    public Vector2 Position;
    public Vector2 ColorCoord;
    public Vector2 TxCoord;

    public ColorCoordinatedTx(Vector2 position, Vector2 colorCoord, Vector2 txCoord)
    {
        this.Position = position;
        this.ColorCoord = colorCoord;
        this.TxCoord = txCoord;
    }

    public readonly static VertexDeclaration VertexDeclaration = new VertexDeclaration
    (
        new VertexElement(0, VertexElementFormat.Vector2, VertexElementUsage.Position, 0),
        new VertexElement(sizeof(float) * 2, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
        new VertexElement(sizeof(float) * 4, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 1)
    );
}