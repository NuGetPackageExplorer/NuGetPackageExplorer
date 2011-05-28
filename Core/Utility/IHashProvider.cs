
namespace NuGet {
    public interface IHashProvider {
        byte[] CalculateHash(byte[] data);

        bool VerifyHash(byte[] data, byte[] hash);
    }
}
