using Microsoft.Identity.Web;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Data;

namespace WebUI.Service
{
    public interface IUserService
    {
        Task AddNewUser(ClaimsPrincipal principal);
        Task<ScUser> GetUser(Guid id);
    }

    public class UserService : IUserService
    {
        private readonly SeamCarverContext _db;
        private string _userFolderBase;

        public UserService(SeamCarverContext db, string userFolderBase)
        {
            _db = db;
            _userFolderBase = userFolderBase;
        }

        public async Task<ScUser> GetUser(Guid id)
        {
            return await _db.Users.FindAsync(id);
        }

        public async Task AddNewUser(ClaimsPrincipal principal)
        {
            var id = principal.Claims.FirstOrDefault(c => c.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value;

            if (string.IsNullOrEmpty(id))
                throw new Exception("Claimsprincipal lacks objectidentifier claim");

            if (!Guid.TryParse(id, out Guid oid))
                throw new Exception($"{id} : objectidentifier claim value is of wrong format. GUID format required");

            if (await (GetUser(oid)) != null)
                throw new Exception($"User with id {oid} is already in the database");

            var now = DateTime.Now;
            string tid = principal.GetTenantId();
            var name = principal.GetDisplayName() ?? string.Empty;

            CreateUserFolder(oid, name, _userFolderBase, out string userFolder);

            var user = new ScUser
            {
                Id = oid,
                IdentityProvider = "",
                LocalFolder = userFolder,
                PrimaryDomain = "",
                WhenCreated = now,
                WhenChanged = now,
            };

            await _db.AddAsync(user);
            await _db.SaveChangesAsync();
        }

        /// <summary>Calculates a valid name for a new user folder and creates that folder</summary>
        private void CreateUserFolder(Guid oid, string username, string pathBase, out string folder)
        {
            var blackChars = new HashSet<char>(Path.GetInvalidPathChars());
            blackChars.Add(' ');

            var folderBuilder = new StringBuilder();

            foreach (char c in username.Take(20))
                if (blackChars.Contains(c))
                    folderBuilder.Append('_');
                else
                    folderBuilder.Append(c);

            folderBuilder.Append('.');
            folderBuilder.Append(oid.ToString("N"));

            folder = folderBuilder.ToString();

            if (!Directory.Exists(pathBase))
                throw new DirectoryNotFoundException(pathBase);

            string fullPath = Path.Combine(pathBase, folder);

            if (Directory.Exists(fullPath))
                throw new Exception($"{fullPath} : folder already exists ");

            Directory.CreateDirectory(fullPath);
        }
    }
}
