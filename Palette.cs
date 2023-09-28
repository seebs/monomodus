using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

class Palette
{
    static Color[] rainbow = {
        new Color(240, 0, 0),
        new Color(240, 100, 0),
        new Color(220, 220, 0),
        new Color(0, 200, 0),
        new Color(0, 0, 255),
        new Color(180, 0, 200),
        new Color(240, 0, 0),
    };

    public static Color[] RBColors = {
                new Color(240, 0, 0, 0),
        new Color(240, 100, 0, 0),
        new Color(220, 220, 0, 0),
        new Color(0, 200, 0, 0),
        new Color(0, 0, 255, 0),
        new Color(180, 0, 200, 0),
                new Color(240, 0, 0, 255),
        new Color(240, 100, 0, 255),
        new Color(220, 220, 0, 255),
        new Color(0, 200, 0, 255),
        new Color(0, 0, 255, 255),
        new Color(180, 0, 200, 255),
    };
    private Color[] _colors;
    public Palette(int shades)
    {
        _colors = new Color[shades * 6];
        for (int i = 0; i < _colors.Length; i++)
        {
            Color prev = rainbow[i / shades];
            Color next = rainbow[(i / shades) + 1];
            int scale = i % shades;
            int antiscale = shades - scale;
            int r = ((int)next.R) * scale + ((int)prev.R) * antiscale;
            int g = ((int)next.G) * scale + ((int)prev.G) * antiscale;
            int b = ((int)next.B) * scale + ((int)prev.B) * antiscale;
            _colors[i] = new Color(r / shades, g / shades, b / shades);
        }
    }

    public int Size()
    {
        return _colors.Length;
    }

    public Color Lookup(int i)
    {
        if (i < 0)
        {
            return _colors[(i % _colors.Length) + _colors.Length];
        }
        else
        {
            return _colors[(i % _colors.Length)];
        }
    }
}