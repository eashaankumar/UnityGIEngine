struct RayPayload
{
    float3 primateNormal;
    float3 bounceRayOrigin;
    float3 bounceRayDir;
    float4 primateColor;
    float4 color;
    float4 worldPos;
    float4 energy;
    uint bounceIndex;
    uint rayType;
    uint rngState;
    uint didHitSpecular;
};

struct RayPayloadShadow
{
    float shadowValue;
};
