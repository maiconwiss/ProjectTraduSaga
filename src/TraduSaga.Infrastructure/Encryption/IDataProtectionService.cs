namespace TraduSaga.Infrastructure.Encryption
{
    public interface IDataProtectionService
    {
        byte[] Protect(byte[] plaintext, string purpose);

        byte[] Unprotect(byte[] protectedData, string purpose);
    }
}
