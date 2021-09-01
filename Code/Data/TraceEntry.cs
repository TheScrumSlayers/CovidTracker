using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CovidTracker.Code.Data
{
    public class TraceEntry
    {
        public string PersonID { get; set; }
        public string LocationID { get; set; }
        // TODO: time, etc

        public TraceEntry(string personID, string locationID)
        {
            PersonID = personID;
            LocationID = locationID;
        }
    }
}
