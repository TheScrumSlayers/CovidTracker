using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CovidTracker.Code.IO
{
    public class IOReturn
    {
        public IOReturnStatus Status { get; }
        public Exception Exception { get; }

        public IOReturn(IOReturnStatus status, Exception except = null)
        {
            Status = status;
            Exception = except;
        }
    }

    public class IOReturn<T> : IOReturn
    {
        public T Value { get; }

        public IOReturn(IOReturnStatus status, T val, Exception except = null) : base(status, except)
        {
            Value = val;
        }
    }

    public enum IOReturnStatus
    {
        Success,
        Fail
    }
}
