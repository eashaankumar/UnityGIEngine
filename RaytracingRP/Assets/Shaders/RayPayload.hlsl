struct RayPayload
{
    float3 primateNormal;
    float3 bounceRayOrigin;
    float3 bounceRayDir;
    float4 primateColor;
    float4 color;
    float4 worldPos;
    uint bounceIndex;
    uint rayType;
    uint rngState;
};

struct RayPayloadShadow
{
    float shadowValue;
};
