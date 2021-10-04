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
        public static async Task<IOReturn<List<User>>> SearchUsers(string name, string phoneNo)
        {
            using (IServiceScope scope = Program.AppHost.Services.CreateScope()) {
                IServiceProvider services = scope.ServiceProvider;
                DatabaseContext context = services.GetRequiredService<DatabaseContext>();

                IEnumerable<User> user = context.Users;
                if (!string.IsNullOrEmpty(name)) {
                    user = user.Where(u => u.Name.ToLower() == name.ToLower());
                }
                if (!string.IsNullOrEmpty(phoneNo)) {
                    if(int.TryParse(phoneNo, out int i)) {
                        user = user.Where(u => u.PhoneNo == phoneNo.ToString());
                    }
                }

                return new IOReturn<List<User>>(IOReturnStatus.Success, user.ToList());
            }
        }

        public static async Task<IOReturn<List<Signin>>> SearchSignins(string userID, string addressLine1, string addressLine2)
        {
            using (IServiceScope scope = Program.AppHost.Services.CreateScope()) {
                IServiceProvider services = scope.ServiceProvider;
                DatabaseContext context = services.GetRequiredService<DatabaseContext>();

                IEnumerable<Signin> signin = context.Signins;
                if (!string.IsNullOrEmpty(userID)) {
                    if(int.TryParse(userID, out int id)) {
                        signin = signin.Where(s => s.UserID == id);
                    }
                }
                if (!string.IsNullOrEmpty(addressLine1)) {
                    signin = signin.Where(s => s.AddressLine1 == addressLine1);
                }
                if (!string.IsNullOrEmpty(addressLine1)) {
                    signin = signin.Where(s => s.AddressLine2 == addressLine2);
                }

                return new IOReturn<List<Signin>>(IOReturnStatus.Success, signin.ToList());
            }
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
                await CreateExampleData();
            }

            // TODO: More tests to retrieve data.
        }

        public static async Task CreateExampleData()
        {
            using (IServiceScope scope = Program.AppHost.Services.CreateScope()) {
                IServiceProvider services = scope.ServiceProvider;
                DatabaseContext context = services.GetRequiredService<DatabaseContext>();

                context.Users.AddRange(
                    new User { UserID = 1111, Name = "Joe", AddressLine1 = "Lot 15 meme road", AddressLine2 = "Big road", PhoneNo = "2346759834", Postcode = "2544", Suburb = "bruh"}, 
                    new User { UserID = 1112, Name = "Bob", AddressLine1 = "Lot 17 road", AddressLine2 = "Small road", PhoneNo = "35634653", Postcode = "3456", Suburb = "Hello"},
                    new User { UserID = 1113, Name = "Sally", AddressLine1 = "Road", AddressLine2 = "Medium road", PhoneNo = "53635523", Postcode = "4444", Suburb = "POtato"},
                    new User { UserID = 1114, Name = "Worm", AddressLine1 = "Road2", AddressLine2 = "another road", PhoneNo = "63435244", Postcode = "4445", Suburb = "Dunno" },
                    new User { UserID = 1115, Name = "Alan", AddressLine1 = "Road54", AddressLine2 = "getrgertrw", PhoneNo = "253245542", Postcode = "1029", Suburb = "Suburbia" }
                    );
                context.SaveChanges();
            }

            await RecordSignin(1111, DateTime.Now, new TerminalInfo {
                AddressLine1 = "Town", AddressLine2 = "THE town", Suburb = "AAAA", Postcode = "7777"
            });
            await RecordSignin(1115, DateTime.Now, new TerminalInfo {
                AddressLine1 = "Town", AddressLine2 = "THE town", Suburb = "AAAA", Postcode = "7777"
            });
            await RecordSignin(1112, DateTime.Now, new TerminalInfo {
                AddressLine1 = "Town", AddressLine2 = "THE town", Suburb = "AAAA", Postcode = "7777"
            });
            await RecordSignin(1114, DateTime.Now, new TerminalInfo {
                AddressLine1 = "Town", AddressLine2 = "THE town", Suburb = "AAAA", Postcode = "7777"
            });
            await RecordSignin(1113, DateTime.Now, new TerminalInfo {
                AddressLine1 = "Town", AddressLine2 = "THE town", Suburb = "AAAA", Postcode = "7777"
            });
            await RecordSignin(1111, DateTime.Now, new TerminalInfo {
                AddressLine1 = "Other town", AddressLine2 = "not the town", Suburb = "Three", Postcode = "4628"
            });
        }
    #endif
    }
}
