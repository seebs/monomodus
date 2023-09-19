#if OPENGL
    #define VS_SHADERMODEL vs_3_0
    #define PS_SHADERMODEL ps_3_0
#else
    #define VS_SHADERMODEL vs_4_0_level_9_1
    #define PS_SHADERMODEL ps_4_0_level_9_1
#endif

struct FlatToPixel
{
    float4 Position : SV_POSITION;
    float2 texCoord : TEXCOORD0;
};

struct PixelToFrame
{
    float4 Color : COLOR0;
};

//------- Texture Samplers --------

Texture xTexture : register(s0);
sampler TextureSampler = sampler_state { texture = <xTexture>; magfilter = LINEAR; minfilter = LINEAR; mipfilter=LINEAR; AddressU = CLAMP; AddressV = CLAMP;};

FlatToPixel FlatVS(float4 inPos : POSITION)
{
	FlatToPixel Output = (FlatToPixel)0;
    Output.Position = inPos;
    float2 coord = (((float2) inPos + 1) / 2);
    Output.texCoord = coord;    
	return Output;    
}

PixelToFrame FlatPS(FlatToPixel PSIn) 
{
	PixelToFrame Output = (PixelToFrame)0;
	
	Output.Color = tex2D(TextureSampler, PSIn.texCoord);

	return Output;
}

PixelToFrame DesatPS(FlatToPixel PSIn) 
{
	PixelToFrame Output = (PixelToFrame)0;
	
    float4 c = tex2D(TextureSampler, PSIn.texCoord);
	float scale = max(max(c.r, c.g), max(c.b, 1));
    Output.Color.rgb = c.rgb / scale;
    if (scale > 1) {
        Output.Color += scale / 10;
    }
    Output.Color.a = c.a;

	return Output;
}


technique Flat
{
	pass Pass0
	{   
		VertexShader = compile VS_SHADERMODEL FlatVS();
		PixelShader  = compile PS_SHADERMODEL FlatPS();	
	}
}

technique Desat
{
	pass Pass0
	{   
		VertexShader = compile VS_SHADERMODEL FlatVS();
		PixelShader  = compile PS_SHADERMODEL DesatPS();	
	}
}