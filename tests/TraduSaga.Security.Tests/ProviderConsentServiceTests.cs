using System;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Serialization;
using TraduSaga.Translation;
using TraduSaga.Translation.Configuration;

namespace TraduSaga.Security.Tests;

public class ProviderConsentServiceTests
{
    [Fact]
    public async Task EnsureGrantedAsync_PersistsAllowAndDoesNotPromptAgain()
    {
        var configuration = TranslationConfiguration.Default;
        var prompt = new ConsentPrompt(ProviderConsentDecision.Allow);
        var service = new ProviderConsentService(configuration, prompt);

        Assert.True(await service.EnsureGrantedAsync(Translators.Google));
        Assert.True(await service.EnsureGrantedAsync(Translators.Google));
        Assert.Equal(ProviderConsentState.Granted, configuration.GoogleConsent);
        Assert.Equal(1, prompt.CallCount);
    }

    [Fact]
    public async Task EnsureGrantedAsync_PersistsDenialAndBlocksFutureRequests()
    {
        var configuration = TranslationConfiguration.Default;
        var prompt = new ConsentPrompt(ProviderConsentDecision.Deny);
        var service = new ProviderConsentService(configuration, prompt);

        Assert.False(await service.EnsureGrantedAsync(Translators.Deepl));
        Assert.False(await service.EnsureGrantedAsync(Translators.Deepl));
        Assert.Equal(ProviderConsentState.Denied, configuration.DeeplConsent);
        Assert.Equal(1, prompt.CallCount);
    }

    [Fact]
    public async Task EnsureGrantedAsync_CancelLeavesUnknownAndAllowsAnotherPrompt()
    {
        var configuration = TranslationConfiguration.Default;
        var prompt = new ConsentPrompt(
            ProviderConsentDecision.Cancel,
            ProviderConsentDecision.Allow);
        var service = new ProviderConsentService(configuration, prompt);

        Assert.False(await service.EnsureGrantedAsync(Translators.Yandex));
        Assert.Equal(ProviderConsentState.Unknown, configuration.YandexConsent);
        Assert.True(await service.EnsureGrantedAsync(Translators.Yandex));
        Assert.Equal(ProviderConsentState.Granted, configuration.YandexConsent);
        Assert.Equal(2, prompt.CallCount);
    }

    [Fact]
    public async Task EnsureGrantedAsync_SerializesConcurrentPromptsForOneProvider()
    {
        var configuration = TranslationConfiguration.Default;
        var prompt = new ConsentPrompt(ProviderConsentDecision.Allow, TimeSpan.FromMilliseconds(20));
        var service = new ProviderConsentService(configuration, prompt);

        var firstRequest = service.EnsureGrantedAsync(Translators.Google);
        var secondRequest = service.EnsureGrantedAsync(Translators.Google);
        var results = await Task.WhenAll(firstRequest, secondRequest);

        Assert.All(results, Assert.True);
        Assert.Equal(1, prompt.CallCount);
    }

    [Fact]
    public void SetState_ChangesOnlyTheSelectedProvider()
    {
        var configuration = TranslationConfiguration.Default;
        var service = new ProviderConsentService(configuration, new ConsentPrompt(ProviderConsentDecision.Allow));

        service.SetState(Translators.Google, ProviderConsentState.Granted);

        Assert.Equal(ProviderConsentState.Granted, configuration.GoogleConsent);
        Assert.Equal(ProviderConsentState.Unknown, configuration.DeeplConsent);
        Assert.Equal(ProviderConsentState.Unknown, configuration.YandexConsent);
    }

    [Fact]
    public void TranslationConfiguration_PersistsConsentStatesThroughXmlSerialization()
    {
        var configuration = TranslationConfiguration.Default;
        configuration.GoogleConsent = ProviderConsentState.Granted;
        configuration.DeeplConsent = ProviderConsentState.Denied;
        var serializer = new XmlSerializer(typeof(TranslationConfiguration));

        using var writer = new StringWriter();
        serializer.Serialize(writer, configuration);
        using var reader = new StringReader(writer.ToString());
        var restored = (TranslationConfiguration)serializer.Deserialize(reader)!;

        Assert.Equal(ProviderConsentState.Granted, restored.GoogleConsent);
        Assert.Equal(ProviderConsentState.Denied, restored.DeeplConsent);
        Assert.Equal(ProviderConsentState.Unknown, restored.YandexConsent);
    }

    private sealed class ConsentPrompt : IProviderConsentPrompt
    {
        private readonly ProviderConsentDecision[] _decisions;
        private readonly TimeSpan _delay;
        private int _callCount;

        public int CallCount => _callCount;

        public ConsentPrompt(ProviderConsentDecision decision, TimeSpan? delay = null)
            : this(new[] { decision }, delay)
        {
        }

        public ConsentPrompt(params ProviderConsentDecision[] decisions)
            : this(decisions, null)
        {
        }

        private ConsentPrompt(ProviderConsentDecision[] decisions, TimeSpan? delay)
        {
            _decisions = decisions;
            _delay = delay ?? TimeSpan.Zero;
        }

        public async Task<ProviderConsentDecision> PromptAsync(Translators provider)
        {
            var call = System.Threading.Interlocked.Increment(ref _callCount) - 1;
            if (_delay > TimeSpan.Zero)
            {
                await Task.Delay(_delay);
            }

            return _decisions[Math.Min(call, _decisions.Length - 1)];
        }
    }
}
