using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chord.Core.API.Chorus.Models
{
    public class ErrorResponse
    {
        public ErrorData[] message;
        public string error;
        public int statusCode;
    }
    public class ErrorData
    {
        public string code;
        public string expected;
        public string received;
        public string[] path;
        public string message;
    }
}
