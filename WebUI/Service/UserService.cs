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
        /// <summary> 
        /// Creates an entry in the apps user table making the principal a recognized user. 
        /// Also creates a work folder for the user. 
        /// Throws exception if the principal does have not a valid objectidentifier claim or if the users folder already exists. 
        /// </summary>                
        Task AddNewUser(ClaimsPrincipal principal);
        Task<ScUser> GetUser(Guid id);
        /// <summary>id must be a GUID </summary>
        Task<ScUser> GetUser(string id);
        /// <summary>
        /// Removes the principals entry from the user table unmarking the principals as the apps user. 
        /// Remove the users work folder if exists. 
        /// Throws exception if the principal does not have a valid objectidentifier claim 
        /// or if the users folder can not be deleted
        /// </summary>
        /// <param name="principal"></param>
        /// <returns></returns>
        Task RemoveUser(ClaimsPrincipal principal);
        /// <summary>
        /// Sets hasAccount = "true" claim. Removes hasAccount = "false" if present
        /// </summary>
        /// <param name="principal">User with hasAccount = "false" claim</param>
        void SetHasAccountClaim(ClaimsPrincipal principal);
        /// <summary>
        /// Retreives users local folder from DB and sets "LocalFolder" claim to users folder.
        /// Throws if user is not present in the DB
        /// </summary>
        /// <param name="principal">Application users principal</param>
        Task SetLocalFolderClaim(ClaimsPrincipal principal);
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

        public async Task<ScUser> GetUser(Guid id) => await _db.Users.FindAsync(id);
        public async Task<ScUser> GetUser(string id) => await GetUser(Guid.Parse(id));

        public async Task AddNewUser(ClaimsPrincipal principal)
        {
            Guid oid = GetPrincipalObjectId(principal);

            if (await (GetUser(oid)) != null)
                throw new Exception($"User with id {oid} is already in the database");

            var now = DateTime.Now;
            string tid = principal.GetTenantId();
            var name = principal.GetDisplayName() ?? string.Empty;

            CreateUserFolder(oid, name, _userFolderBase, out string userFolder);

            var user = new ScUser
            {
                Id = oid,
                IdentityProvider = principal.Claims.First(c => c.Type == ClaimTypes.NameIdentifier)?.Issuer,
                LocalFolder = userFolder,
                TenantId = principal.GetTenantId(),
                WhenCreated = now,
                WhenChanged = now,
            };

            await _db.AddAsync(user);
            await _db.SaveChangesAsync();
        }

        private Guid GetPrincipalObjectId(ClaimsPrincipal principal)
        {
            var id = principal.Claims.FirstOrDefault(c => c.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value;

            if (string.IsNullOrEmpty(id))
                throw new Exception("Claimsprincipal lacks objectidentifier claim");

            if (!Guid.TryParse(id, out Guid oid))
                throw new Exception($"{id} : objectidentifier claim value is of wrong format. GUID format required");
            return oid;
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

        public async Task RemoveUser(ClaimsPrincipal principal)
        {
            Guid oid = GetPrincipalObjectId(principal);
            var user = await GetUser(oid);

            if (user != null)
            {

                _db.Remove(user);
                await _db.SaveChangesAsync();

                string userFolder = Path.Combine(_userFolderBase, user.LocalFolder);
                Directory.Delete(userFolder, recursive: true);
            }
        }

        public void SetHasAccountClaim(ClaimsPrincipal principal)
        {
            var identity = (ClaimsIdentity)principal.Identity;
            var claimToRemove = identity.FindFirst(c => c.Type == "hasAccount" && c.Value == "false");
            
            if (claimToRemove != null)
                identity.RemoveClaim(claimToRemove);

            identity.AddClaim(new Claim("hasAccount", "true"));
        }

        public async Task SetLocalFolderClaim(ClaimsPrincipal principal)
        {
            var oid = principal.GetObjectId();

            if (oid == null)
                throw new ArgumentException("A valid objectidentifier claim must be set", nameof(principal));

            var user = await GetUser(oid);

            if (user == null)
                throw new ArgumentException("user not found in DB", nameof(principal));

            var identity = (ClaimsIdentity)principal.Identity;            

            identity.AddClaim(new Claim("LocalFolder", user.LocalFolder));
        }
    }
}
