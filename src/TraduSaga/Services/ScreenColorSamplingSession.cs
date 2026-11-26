namespace TraduSaga.Services
{
    internal static class ScreenColorSamplingSession
    {
        public static bool IsActive { get; set; }

        public static bool ShouldReactivateChatWindow => !IsActive;
    }
}
