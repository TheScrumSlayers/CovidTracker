using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CovidTracker.Code.Data;

namespace CovidTracker.Code.IO
{
    public static class TraceIO
    {
        private static List<TraceEntry> buffer = new List<TraceEntry>();

        // TODO: Class which writes trace data to hdd and/or network. Design ideas:
        // File structure is bin/Data/...
        // Inner folders could be separated by month to reduce calculations when doing queries.
        // Files would be separated by day.

        public static IOReturn WritePendingBuffer()
        {
            if (buffer.Count == 0)
                return new IOReturn(IOReturnStatus.Success);

            buffer.Clear();
            // TODO: Remember, if the file exists - merge existing data with new data, THEN overwrite the file.
            throw new NotImplementedException();
        }

    }
}
