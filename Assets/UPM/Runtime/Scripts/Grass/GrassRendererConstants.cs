public static class GrassRendererConstants
{
    public const string ComputeShaderKernelMain = "CSMain";

    public static class ProfilerSample
    {
        public const string FrustumCulling = "CPU cell frustum culling (heavy)";
        public const string ComputeShaderDispatch = "ComputeShader.Dispatch (cullingComputeShader)";
    }

    public static class MaterialParam
    {
        public const string VPMatrix = "_VPMatrix";
        public const string DrawDistance = "_MaxDrawDistance";
        public const string CullingThreshold = "_Threshold";
        public const string StartOffset = "_StartOffset";

        public const string PivotPos = "_PivotPosWS";
        public const string BoundsSize = "_BoundSize";
        public const string AllInstanceBuffer = "_AllInstancesPosWSBuffer";
        public const string VisibleIndexBuffer = "_VisibleInstancesOnlyPosWSIDBuffer";
    }
}
