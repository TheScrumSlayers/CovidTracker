using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using CovidTracker.Code.IO;

namespace CovidTracker.Code
{
    [ApiController]
    [Route("api/Login")]
    public class LoginController : ControllerBase
    {
        [HttpPost]
        public string Login([FromBody] string data)
        {
            Random rand = new Random(Environment.TickCount);

            // NOTE: Test code. Not final!
            // As a test, log the data to a file.
            FileIO.Write(FileIO.StorageDirectory + "\\" + rand.Next(), $"Recieved post from: {data}");

            return null;
        }
    }
}
