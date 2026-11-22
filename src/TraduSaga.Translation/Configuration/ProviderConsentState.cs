namespace TraduSaga.Translation.Configuration
{
    public enum ProviderConsentState : byte
    {
        Unknown,
        Denied,
        Granted
    }

    public enum ProviderConsentDecision : byte
    {
        Cancel,
        Deny,
        Allow
    }
}
