using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using FNS.Models;

namespace FNS.Repository
{
    public interface IUserLogin
    {
        public DataTable LoginUser(string email = "", string password = "");
        public DataTable RegisterUser(string email = "", string password = "", string firstName = "", string lastName = "", string username = "", string gender = "", DateTime dob = default(DateTime));
        public DataTable GetUserStreakByEmail(string email = "");
        DataTable UpdateUserProfile(string email, string firstName, string lastName, string goal, string? profileImagePath = null,
            double? weight = null, double? height = null, int? age = null, string? diet = null,
            string? lifestyle = null, string? bloodType = null, double? sleepPatterns = null, string? gender = null);
        DataTable UpdateProfileImagePath(string email, string? profileImagePath);
        string GetProfileImagePath(string email);
        bool UserExistsByEmail(string email);
        bool SavePasswordResetToken(string email, string token, DateTime expiresAt);
        string GetEmailByValidResetToken(string token);
        bool ResetPassword(string token, string newPassword);
        bool UpgradeUserToPremium(string email);
        DataTable FindOrCreateGoogleUser(string email, string firstName, string lastName, string profileImageUrl);
    }
}
