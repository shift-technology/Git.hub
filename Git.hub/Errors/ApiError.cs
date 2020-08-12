using System;
using System.Collections.Generic;
using System.Text;

namespace Git.hub.Errors
{
    public class ApiError
    {
        public string message { get; set; }

        public Error[] errors { get; set; }

        public string documentation_url { get; set; }
    }

    public class Error
    {
        public string resource { get; set; }
        public string code { get; set; }
        public string message { get; set; }
    }
}
