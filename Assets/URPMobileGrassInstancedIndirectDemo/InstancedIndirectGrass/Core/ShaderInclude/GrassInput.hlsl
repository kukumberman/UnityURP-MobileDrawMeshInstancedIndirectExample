struct Attributes
{
    float4 positionOS : POSITION;
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    half3 color : COLOR;
};

CBUFFER_START(UnityPerMaterial)
float3 _PivotPosWS;
float3 _BoundSize;

float _GrassWidth;
float _GrassHeight;

float _WindAIntensity;
float _WindAFrequency;
float2 _WindATiling;
float2 _WindAWrap;

float _WindBIntensity;
float _WindBFrequency;
float2 _WindBTiling;
float2 _WindBWrap;

float _WindCIntensity;
float _WindCFrequency;
float2 _WindCTiling;
float2 _WindCWrap;

half3 _BaseColor;
float4 _BaseColorTexture_ST;
half3 _GroundColor;

half _RandomNormal;

StructuredBuffer<float3> _AllInstancesPosWSBuffer;
StructuredBuffer<uint> _VisibleInstancesOnlyPosWSIDBuffer;
CBUFFER_END

sampler2D _GrassBendingRT;
sampler2D _BaseColorTexture;
