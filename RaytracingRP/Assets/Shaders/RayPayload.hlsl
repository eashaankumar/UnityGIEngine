struct RayPayload
{
    float4 primateColor;
    float4 color;
    float4 worldPos;
    uint bounceIndex;
    uint rayType;
};

struct RayPayloadShadow
{
    float shadowValue;
};