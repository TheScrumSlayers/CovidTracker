#if DEBUG

using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CovidTracker.Code.Database
{
    /// <summary>
    /// The class initializes a bunch of mockup data for the system. It is not included in the release build.
    /// </summary>
    public static class DatabaseTest
    {
        public static Random Rand = new Random();

        public static string[] FirstNames = new string[] {
            "Tom", "Jacob", "Sally", "Micheal", "Terry", "Jeffery",
            "Nameless", "Bobby", "Anthony", "Grace", "Alice", "Jeff", 
            "John", "Zezima", "Scarlet", "Greg", "Joe", "Alfred", "Karen",
            "Jesse", "Jessie", "Jason", "Stacy", "Jack"
        };

        public static string[] MiddleNames = new string[] {
            "Tom", "Jacob", "Sally", "Micheal", "Terry", "Jeffery",
            "Nameless", "Bobby", "Anthony", "Grace", "Alice", "Jeff",
            "John", "Zezima", "Scarlet", "Greg", "Joe", "Alfred", "Karen",
            "Jesse", "Jessie", "Jason", "Stacy", "Jack"
        };

        public static string[] LastNames = new string[] {
            "Wills", "Orange", "Potato", "Green", "Igor", "Jefferson",
            "McNoName", "El' Yelangno", "Greyhair", "Lanwonder", "Bob Jr.",
            "Big", "Smalls", "Blue", "Emerald", "Longbeard", "Island",
            "Jr.", "Oop", "Welsh", "Swims", "Walks", "Crawls", "Jogs",
            "Carn", "Barner", "Beans"
        };

        public static string[] AddressLine1s = new string[] {
            "Lot 12", "Building F", "Lot 18 flat", "Lot 10", "Lot 9", "Flat #7"
        };

        public static string[] AddressLine2s = new string[] {
            "Burger street", "17 medium road", "fair drive", "mt. coughmore", "Orange & purple street"
        };

        public static string[] Suburbs = new string[] {
            "Mawson lakes", "Adelaide", "Birdwood", "mt. pleasant"
        };

        public static async Task GenerateTestData() 
        {
            using (IServiceScope scope = Program.AppHost.Services.CreateScope()) {
                IServiceProvider services = scope.ServiceProvider;
                DatabaseContext context = services.GetRequiredService<DatabaseContext>();

                List<User> users = new List<User>();

                // Create users.
                int index = 0;
                for(int i = 0; i < 1000; i++) {
                    string name = FirstNames[Rand.Next(0, FirstNames.Length)];
                    if(Rand.Next(0, 100) > 20) {
                        name += " " + MiddleNames[Rand.Next(0, MiddleNames.Length)];
                    }
                    name += " " + LastNames[Rand.Next(0, LastNames.Length)];

                    User user = new User() {
                        UserID = 1111+index,
                        Name = name,
                        PhoneNo = Rand.Next(1000000, 9999999).ToString(),
                        AddressLine1 = AddressLine1s[Rand.Next(0, AddressLine1s.Length)],
                        AddressLine2 = AddressLine2s[Rand.Next(0, AddressLine2s.Length)],
                        Suburb = Suburbs[Rand.Next(0, Suburbs.Length)],
                        Postcode = Rand.Next(1000, 5000).ToString()
                    };
                    users.Add(user);
                    await context.Users.AddAsync(user);
                    ++index;
                }
                await context.SaveChangesAsync();

                for (int i = 0; i < 150; i++) {
                    string addressLine1 = AddressLine1s[Rand.Next(0, AddressLine1s.Length)];
                    string addressLine2 = AddressLine2s[Rand.Next(0, AddressLine2s.Length)];
                    string suburb = Suburbs[Rand.Next(0, Suburbs.Length)];
                    string postcode = Rand.Next(1000, 5000).ToString();
                    string phone = Rand.Next(1000000, 9999999).ToString();

                    int amt = Rand.Next(1, 15);
                    for(int y = 0; y < amt; y++) {
                        Signin signin = new Signin() {
                            UserID = users[Rand.Next(0, users.Count-1)].UserID,
                            Time = RandomDateTime(),
                            PhoneNo = phone,
                            AddressLine1 = addressLine1,
                            AddressLine2 = addressLine2,
                            Suburb = suburb,
                            Postcode = postcode
                        };

                        await context.Signins.AddAsync(signin);
                    }
                }
                await context.SaveChangesAsync();
            }
        }

        public static DateTime RandomDateTime()
        {
            DateTime start = new DateTime(2020, 1, 1);
            int range = (DateTime.Today - start).Days;
            return start.AddDays(Rand.Next(range));
        }

    }
}

#endif