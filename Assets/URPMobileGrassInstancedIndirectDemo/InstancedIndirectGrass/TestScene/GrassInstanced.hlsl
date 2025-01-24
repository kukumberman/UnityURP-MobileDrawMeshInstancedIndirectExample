#ifndef GRASS_SHADER_INCLUDED
#define GRASS_SHADER_INCLUDED

// https://youtu.be/bny9f4zw5JE?t=514
// https://github.com/KDSBest/Render1MillionObjectsWithUnityAndShaderGraph/blob/main/Assets/Shaders/BeltInstancing.hlsl

// https://gist.github.com/Cyanilux/4046e7bf3725b8f64761bf6cf54a16eb

StructuredBuffer<float3> _AllInstancesPosWSBuffer;
StructuredBuffer<uint> _VisibleInstancesOnlyPosWSIDBuffer;

#if UNITY_ANY_INSTANCING_ENABLED
// https://github.com/Unity-Technologies/Graphics/blob/master/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/ParticlesInstancing.hlsl
void vertInstancingSetup()
{
	uint index = _VisibleInstancesOnlyPosWSIDBuffer[unity_InstanceID];
	float3 worldPosition = _AllInstancesPosWSBuffer[index];

    float4x4 ObjectToWorld = float4x4(
		1.0, 0.0, 0.0, 0.0,
		0.0, 1.0, 0.0, 0.0,
		0.0, 0.0, 1.0, 0.0,
		worldPosition.x, worldPosition.y, worldPosition.z, 1.0
	);
	
	#ifndef SHADERGRAPH_PREVIEW
	unity_ObjectToWorld = ObjectToWorld;
	//unity_WorldToObject = inverse(ObjectToWorld);
	#endif
}
#endif

void GetInstanceID_float(out float Out)
{
	Out = 0;
#ifndef SHADERGRAPH_PREVIEW
#if UNITY_ANY_INSTANCING_ENABLED
	Out = unity_InstanceID;
#endif
#endif
}

void GetWorldPivotPosition_float(out float3 Out)
{
    float id;
    GetInstanceID_float(id);
    uint index = _VisibleInstancesOnlyPosWSIDBuffer[id];
    float3 worldPosition = _AllInstancesPosWSBuffer[index];
    Out = worldPosition;
}

void Instancing_float(float3 Position, out float3 Out)
{
    float3 worldPivot;
    GetWorldPivotPosition_float(worldPivot);
    Out = Position + worldPivot;
}

#endif
