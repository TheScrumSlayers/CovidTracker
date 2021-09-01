using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CovidTracker.Code.Data
{
    public static class Database
    {
        // TODO: Database to create reports.

        public static Dictionary<string, Person> PersonDictionary { get; set; } = new Dictionary<string, Person>();
        public static Dictionary<string, Location> LocationDictionary { get; set; } = new Dictionary<string, Location>();

        // TODO: We cannot store all the data in here at one time. The SHO will begin a search, and the DB will be populated with relevant data.
        public static void InitializeQueries(/* params - person, location, time, etc. */)
        {
            // Populate the database with data relevant to the queries.
        }

        public static Person GetPerson(string key)
        {
            return PersonDictionary.TryGetValue(key, out Person val) ? val : null;
        }

        public static Location GetLocation(string key)
        {
            return LocationDictionary.TryGetValue(key, out Location val) ? val : null;
        }

    }
}
