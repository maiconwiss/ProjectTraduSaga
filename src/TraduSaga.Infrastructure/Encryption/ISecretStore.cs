namespace TraduSaga.Infrastructure.Encryption
{
    public interface ISecretStore
    {
        string GetSecret(string name);

        void SetSecret(string name, string value);

        void DeleteSecret(string name);
    }
}
