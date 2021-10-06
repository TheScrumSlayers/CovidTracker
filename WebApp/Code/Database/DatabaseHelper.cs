using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CovidTracker.Code.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace CovidTracker.Code.Database
{
    /// <summary>
    /// Class which provides methods to easily interact with the databases.
    /// </summary>
    public static class DatabaseHelper
    {
        /// <summary>
        /// Queries the database for contacted / at-risk persons. Requires authentication, as it's accessed by the frontend.
        /// </summary>
        /// <param name="password">Authentication password.</param>
        /// <param name="users">List of contacted users.</param>
        /// <param name="depth">Amount of people to recursively include.</param>
        /// <param name="beforeDate">Before date.</param>
        /// <param name="afterDate">After date.</param>
        /// <returns>List of users who likely came into contact with any of the contacted users.</returns>
        public static async Task<IOReturn<List<User>>> GenerateReport(byte[] password, HashSet<User> users, int depth, DateTime beforeDate, DateTime afterDate)
        {
            FileIO.ReadBytes(FileIO.StorageDirectory + "\\Passwords\\Password.txt");
            if (!await VerifyPassword(password)) {
                return new IOReturn<List<User>>(IOReturnStatus.Fail, null, new Exception("Authentication failed."));
            }

            HashSet<User> reportUsers = new HashSet<User>();
            using (IServiceScope scope = Program.AppHost.Services.CreateScope()) {
                IServiceProvider services = scope.ServiceProvider;
                DatabaseContext context = services.GetRequiredService<DatabaseContext>();

                foreach (User user in users) {                    
                    IOReturn<HashSet<User>> ret = await GetContactedUsers(context, user, depth, beforeDate, afterDate);
                    HashSet<User> tmp = ret.Value;
                    reportUsers.AddRange(tmp);
                }
            }

            return new IOReturn<List<User>>(IOReturnStatus.Success, reportUsers.ToList());
        }

        /// <summary>
        /// Queries the database for contacted / at-risk persons.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="user"></param>
        /// <param name="depth"></param>
        /// <param name="beforeDate"></param>
        /// <param name="afterDate"></param>
        /// <returns></returns>
        private static async Task<IOReturn<HashSet<User>>> GetContactedUsers(DatabaseContext context, User user, int depth, DateTime beforeDate, DateTime afterDate)
        {
            HashSet<User> reportUsers = new HashSet<User>();

            // Add the exposed user.
            User reported = user;
            reportUsers.Add(reported);

            // Iterate over the locations where the person has visited.
            if (depth > 0) {
                List<Signin> signins = context.Signins.Where(s => s.UserID == user.UserID && s.Time <= beforeDate && s.Time >= afterDate).ToList();
                foreach(Signin signin in signins) {
                    string adrLine1 = signin.AddressLine1;
                    string adrLine2 = signin.AddressLine2;
                    string suburb = signin.Suburb;
                    string code = signin.Postcode;

                    // If depth is still > 0, iterate over each location recursively.
                    List<Signin> signinsContact = context.Signins.Where(
                        s => s.AddressLine1 == adrLine1
                        && s.AddressLine2 == adrLine2
                        && s.Suburb == suburb
                        && s.Postcode == code
                        && s.Time <= beforeDate
                        && s.Time >= afterDate).ToList();

                    // Recursively report valid contacts until the depth reaches 0.
                    foreach(Signin contact in signinsContact) {
                        User contactUser = context.Users.Where(u => u.UserID == contact.UserID).FirstOrDefault();
                        reportUsers.Add(contactUser);

                        IOReturn<HashSet<User>> ret = await GetContactedUsers(context, contactUser, depth - 1, beforeDate, afterDate);
                        HashSet<User> tmp = ret.Value;
                        reportUsers.AddRange(tmp);
                    }
                }
            }
            return new IOReturn<HashSet<User>>(IOReturnStatus.Success, reportUsers);
        }

        /// <summary>
        /// Searches the users with several filters. Requires authentication as this is used by the frontend.
        /// </summary>
        /// <param name="password">Authentication password.</param>
        /// <param name="id">User ID, optional.</param>
        /// <param name="name">Name, optional.</param>
        /// <param name="phoneNo">Phone number, optional.</param>
        /// <returns>List of users matching the criteria.</returns>
        public static async Task<IOReturn<List<User>>> SearchUsers(byte[] password, int? id, string name, ulong? phoneNo)
        {
            if (!await VerifyPassword(password)) {
                return new IOReturn<List<User>>(IOReturnStatus.Fail, null, new Exception("Authentication failed."));
            }

            using (IServiceScope scope = Program.AppHost.Services.CreateScope()) {
                IServiceProvider services = scope.ServiceProvider;
                DatabaseContext context = services.GetRequiredService<DatabaseContext>();

                IEnumerable<User> user = context.Users;
                if (id.HasValue) {
                    user = user.Where(u => u.UserID == id.Value);
                }
                if (!string.IsNullOrEmpty(name)) {
                    user = user.Where(u => u.Name.ToLower().Contains(name.ToLower()));
                }
                if (phoneNo.HasValue) {
                    user = user.Where(u => u.PhoneNo == phoneNo.Value.ToString());
                }

                return new IOReturn<List<User>>(IOReturnStatus.Success, user.ToList());
            }
        }

        /// <summary>
        /// Verifies a password formatted as a byte array.
        /// </summary>
        /// <param name="password">Password.</param>
        /// <returns>True if the password matches one on the server.</returns>
        public static async Task<bool> VerifyPassword(byte[] password)
        {
            IOReturn<byte[]> ret = await Task.Run(() => FileIO.ReadBytes(FileIO.StorageDirectory + "\\Passwords\\Password.txt"));
            if(ret.Status == IOReturnStatus.Fail) {
                return false;
            }
            return password.SequenceEqual(ret.Value);
        }

        /// <summary>
        /// Record a user sign in.
        /// </summary>
        /// <param name="userID">User unique identification.</param>
        /// <param name="time">Time of the sign in.</param>
        /// <param name="info">Terminal-specific information such as location.</param>
        /// <returns></returns>
        public static async Task<IOReturn> RecordSignin(int userID, DateTime time, TerminalInfo info)
        {
        #if DEBUG
            await Test();
        #endif

            using (IServiceScope scope = Program.AppHost.Services.CreateScope()) {
                IServiceProvider services = scope.ServiceProvider;
                DatabaseContext context = services.GetRequiredService<DatabaseContext>();

                // Attempt to retrieve user info.
                IOReturn<User> userOp = await RetrieveUser(userID);
                if (userOp.Status == IOReturnStatus.Fail)
                    return new IOReturn(IOReturnStatus.Fail, userOp.Exception);

                // Now we have user info. Record a signin.
                User user = userOp.Value;
                context.Add(new Signin {
                    UserID = user.UserID,
                    PhoneNo = user.PhoneNo,
                    Time = time,
                    AddressLine1 = info.AddressLine1,
                    AddressLine2 = info.AddressLine2,
                    Suburb = info.Suburb,
                    Postcode = info.Postcode
                });
                await context.SaveChangesAsync();
            }

            return new IOReturn(IOReturnStatus.Success);
            // TODO: Local storage if server isn't avaliable.
        }

        /// <summary>
        /// Attempts to retrieve a user from the system.
        /// </summary>
        /// <param name="userID">User ID to retrieve.</param>
        /// <returns>IOReturn object, which may fail if the user cannot be found.</returns>
        public static async Task<IOReturn<User>> RetrieveUser(int userID)
        {
            // First, try to retrieve the user from the local database.
            IOReturn<User> ret = await RetrieveLocallyStoredUser(userID);
            if (ret.Status == IOReturnStatus.Success) {
                return ret;
            }

            using (IServiceScope scope = Program.AppHost.Services.CreateScope()) {
                IServiceProvider services = scope.ServiceProvider;
                DatabaseContext context = services.GetRequiredService<DatabaseContext>();

                // Otherwise if that fails, try to retrieve the user from the external server.
                ExternalDatabaseContext externalContext = services.GetRequiredService<ExternalDatabaseContext>();
                User user = await externalContext.Users.FindAsync(userID);

                if (user == null)
                    return new IOReturn<User>(IOReturnStatus.Fail, null, new Exception($"User '{userID}' was not found."));

                context.Add(user);
                await context.SaveChangesAsync();
                return new IOReturn<User>(IOReturnStatus.Success, user);
            }
        }

        /// <summary>
        /// Retrieves a user from the local, non-client database.
        /// </summary>
        /// <param name="userID">User ID.</param>
        /// <returns>User from the local database if they exist.</returns>
        public static async Task<IOReturn<User>> RetrieveLocallyStoredUser(int userID)
        {
            using (IServiceScope scope = Program.AppHost.Services.CreateScope()) {
                IServiceProvider services = scope.ServiceProvider;
                DatabaseContext context = services.GetRequiredService<DatabaseContext>();

                User user = await context.Users.FindAsync(userID);

                return user == null 
                    ? new IOReturn<User>(IOReturnStatus.Fail, null) 
                    : new IOReturn<User>(IOReturnStatus.Success, user);
            }
        }

        // !!!!!!!!!TEMPORARY CODE DELETE WHEN DONE!!!!!!!
    #if DEBUG
        public static async Task Test()
        {
            // Create example data if it doesn't exist.
            IOReturn<User> ret = await RetrieveLocallyStoredUser(1111);
            if (ret.Status == IOReturnStatus.Fail) {
                await DatabaseTest.GenerateTestData();
            }

            // TODO: More tests to retrieve data.
        }
    #endif
    }
}

public static class Extensions
{
    public static void AddRange<T>(this HashSet<T> source, IEnumerable<T> toAdd)
    {
        foreach (T item in toAdd) {
            source.Add(item);
        }
    }
}
