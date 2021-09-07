﻿using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
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
            string file = FileIO.StorageDirectory + Path.DirectorySeparatorChar + rand.Next() + ".txt";
            FileIO.Write(file, $"Recieved POST from: {data}\n");

            return file;
        }
    }
}
