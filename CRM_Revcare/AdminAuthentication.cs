using System;
using System.Linq;
using CRM.infrastructure.data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CRM_Revcare_
{
    public sealed class AdminAuthentication
    {
        private readonly Func<MasterCrmDbContext> createContext;
        private readonly PasswordHasher<IdentityUser> hasher = new PasswordHasher<IdentityUser>();
        public AdminAuthentication() : this(CrmDatabase.CreateContext) { }
        public AdminAuthentication(Func<MasterCrmDbContext> factory) => createContext = factory;

        public void EnsureAdminAccount()
        {
            using var strategyContext = createContext();
            var strategy = strategyContext.Database.CreateExecutionStrategy();
            strategy.Execute(() =>
            {
                using var db = createContext();
                // Ordinary logins do not need an account-provisioning transaction.
                if ((from user in db.Users
                     join membership in db.UserRoles on user.Id equals membership.UserId
                     join existingRole in db.Roles on membership.RoleId equals existingRole.Id
                     where user.NormalizedUserName == "ADMIN" && existingRole.NormalizedName == "ADMIN"
                     select user.Id).Any()) return;
                using var transaction = db.Database.BeginTransaction(System.Data.IsolationLevel.Serializable);
                var role = db.Roles.SingleOrDefault(x => x.NormalizedName == "ADMIN");
                if (role == null)
                {
                    role = new IdentityRole("Admin") { NormalizedName = "ADMIN" };
                    db.Roles.Add(role);
                    db.SaveChanges();
                }
                var admin = db.Users.SingleOrDefault(x => x.NormalizedUserName == "ADMIN");
                if (admin == null)
                {
                    admin = new IdentityUser("admin") { NormalizedUserName = "ADMIN", SecurityStamp = Guid.NewGuid().ToString() };
                    admin.PasswordHash = hasher.HashPassword(admin, "admin123");
                    db.Users.Add(admin);
                    db.SaveChanges();
                }
                if (!db.UserRoles.Any(x => x.UserId == admin.Id && x.RoleId == role.Id))
                {
                    db.UserRoles.Add(new IdentityUserRole<string> { UserId = admin.Id, RoleId = role.Id });
                    db.SaveChanges();
                }
                transaction.Commit();
            });
        }

        public bool Validate(string username, string password)
        {
            if (!string.Equals(username?.Trim(), "admin", StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(password)) return false;
            using var db = createContext();
            var user = db.Users.SingleOrDefault(x => x.NormalizedUserName == "ADMIN");
            if (user == null || string.IsNullOrEmpty(user.PasswordHash)) return false;
            if (user.LockoutEnabled && user.LockoutEnd > DateTimeOffset.UtcNow) return false;
            bool isAdmin = (from membership in db.UserRoles join role in db.Roles on membership.RoleId equals role.Id
                           where membership.UserId == user.Id && role.NormalizedName == "ADMIN" select membership).Any();
            if (!isAdmin) return false;
            return hasher.VerifyHashedPassword(user, user.PasswordHash, password) != PasswordVerificationResult.Failed;
        }
    }
}
