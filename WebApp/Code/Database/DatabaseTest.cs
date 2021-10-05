#if DEBUG

using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CovidTracker.Code.Database
{
    public static class DatabaseTest
    {
        public static string[] UserNames = new string[] { 
            "Tom Wills", "Jacob Orange", "Sally Potato", "Micheal Green", "Terry Igor", "Jeffery Jefferson",
            "Nameless McNoName", "Bobby", "Anthony El' Yelangno", "Grace Greyhair", "Alice Lanwonder", "Jeff Bob Jr."
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

        public static void GenerateTestData() 
        {
            using (IServiceScope scope = Program.AppHost.Services.CreateScope()) {
                IServiceProvider services = scope.ServiceProvider;
                DatabaseContext context = services.GetRequiredService<DatabaseContext>(); 
            }
        }

    }
}

#endif