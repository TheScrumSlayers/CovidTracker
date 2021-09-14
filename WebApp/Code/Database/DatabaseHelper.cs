using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CovidTracker.Code.IO;
using Microsoft.Extensions.DependencyInjection;

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
            using (IServiceScope scope = Program.AppHost.Services.CreateScope()) {
                IServiceProvider services = scope.ServiceProvider;

                DatabaseContext context = services.GetRequiredService<DatabaseContext>();

                // First, try to retrieve the user from the local database.
                User user = await context.Users.FindAsync(userID);

                // If the user is found locally, simply return it.
                if (user != null)
                    return new IOReturn<User>(IOReturnStatus.Success, user);

                // Otherwise, try to retrieve the user from the external server.
                ExternalDatabaseContext externalContext = services.GetRequiredService<ExternalDatabaseContext>();
                user = await externalContext.Users.FindAsync(userID);

                if (user == null)
                    return new IOReturn<User>(IOReturnStatus.Fail, null, new Exception($"User '{userID}' was not found."));

                context.Add(user);
                await context.SaveChangesAsync();
                return new IOReturn<User>(IOReturnStatus.Success, user);
            }
        }

        ///
        ///
        ///
        ///
        ///
        /// <summary>
        /// !!!!!!!!!TEMPORARY CODE DELETE WHEN DONE!!!!!!!
        /// </summary>
        ///
        ///
        ///
        ///
        /// 
        public static void Test()
        {
            using (IServiceScope scope = Program.AppHost.Services.CreateScope()) {
                IServiceProvider services = scope.ServiceProvider;
                DatabaseContext context = services.GetRequiredService<DatabaseContext>();

                context.Users.Add(new User
                    { UserID = 12345, Name = "Joe" });
                context.SaveChanges();
            }
        }
    }
}
