using System.Threading.Tasks;

namespace arsX.Sol_Explorer.Services
{
    public interface ICredentialStore
    {
        Task SetPasswordAsync(string siteKey, string username, string password);
        Task<string?> GetPasswordAsync(string siteKey, string username);
        Task RemovePasswordAsync(string siteKey, string username);
    }
}
