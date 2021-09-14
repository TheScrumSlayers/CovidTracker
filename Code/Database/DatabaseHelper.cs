using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CovidTracker.Code.IO;

namespace CovidTracker.Code.Database
{
    /// <summary>
    /// Class which provides methods to easily interact with the databases.
    /// </summary>
    public static class DatabaseHelper
    {
        /// <summary>
        /// Record a user sign in.
        /// </summary>
        /// <param name="userID">User unique identification.</param>
        /// <param name="time">Time of the sign in.</param>
        /// <param name="info">Terminal-specific information such as location.</param>
        /// <returns></returns>
        public static async Task<IOReturn> RecordSignin(int userID, DateTime time, TerminalInfo info)
        {
            // Attempt to retrieve user info.
            IOReturn<User> userOp = await RetrieveUser(userID);
            if (userOp.Status == IOReturnStatus.Fail)
                return new IOReturn(IOReturnStatus.Fail, userOp.Exception);

            // Now we have user info. Record a signin.
            User user = userOp.Value;
            await using (DatabaseContext context = new DatabaseContext()) {
                context.Add(new Signin {
                    SigninID = 0, // TODO: Auto increment.
                    UserID = user.UserID,
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
            await using DatabaseContext context = new DatabaseContext();
            User user = await context.Users.FindAsync(userID);

            // If the user is found locally, simply return it.
            if (user != null)
                return new IOReturn<User>(IOReturnStatus.Success, user);

            // Otherwise, try to retrieve the user from the external server.
            await using ExternalDatabaseContext externalContext = new ExternalDatabaseContext();
            user = await context.Users.FindAsync(userID);
            
            // If the user isn't found in the local or external databases, throw an error.
            if (user == null)
                return new IOReturn<User>(IOReturnStatus.Fail, null, new Exception($"User '{userID}' was not found."));

            // If the external user was retrieved, add it to our database and return it.
            context.Add(user);
            await context.SaveChangesAsync();
            return new IOReturn<User>(IOReturnStatus.Success, user);
        }
    }
}
