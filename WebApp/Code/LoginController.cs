using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using CovidTracker.Code.Database;
using CovidTracker.Code.IO;

namespace CovidTracker.Code
{
    [ApiController]
    [Route("api/Login")]
    public class LoginController : ControllerBase
    {
        [HttpPost]
        public async Task<string> Login([FromBody] SigninPostData data)
        {
            int parsed;
            try {
                parsed = int.Parse(data.UserID);
            } catch (Exception) {
                return "Error: The QR code is invalid.";
            }

            IOReturn ret = await DatabaseHelper.RecordSignin(parsed, data.DateTime, data.TerminalInfo);
            
            return ret.Status == IOReturnStatus.Success 
                ? "SUCCESS" 
                : "Error: User was not found.";
        }
    }

    public class SigninPostData
    {
        public string UserID { get; set; }
        public DateTime DateTime { get; set; }
        public TerminalInfo TerminalInfo { get; set; }
    }

    public class TerminalInfo
    {
        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }
        public string Suburb { get; set; }
        public string Postcode { get; set; }
    }
}
