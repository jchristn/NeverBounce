using System;
using NeverBounce;
using GetSomeInput;
using Newtonsoft.Json;

namespace Test
{
    internal class Program
    {
        static NeverBounceClient _Client = null;

        static void Main(string[] args)
        { 
            string apiKey = Inputty.GetString("API key:", null, false);
            _Client = new NeverBounceClient(apiKey);

            while (true)
            {
                string userInput = Inputty.GetString("Command [? for help]:", null, false);
                userInput = userInput.ToLower().Trim();
                if (String.IsNullOrEmpty(userInput)) continue;

                if (userInput.Equals("?"))
                {
                    Console.WriteLine("");
                    Console.WriteLine("Available commands:");
                    Console.WriteLine("  ?         help, this menu");
                    Console.WriteLine("  q         quit");
                    Console.WriteLine("  cls       clear the screen");
                    Console.WriteLine("  retries   set the NeverBounce client retry count");
                    Console.WriteLine("  [value]   verify an email address");
                    Console.WriteLine("");
                }
                else if (userInput.Equals("q") || userInput.Equals("Q"))
                {
                    break;
                }
                else if (userInput.Equals("c") || userInput.Equals("cls"))
                {
                    Console.Clear();
                }
                else if (userInput.Equals("retries"))
                {
                    Console.WriteLine("Current retry count: " + _Client.RetryAttempts);
                    int newAmount = Inputty.GetInteger("New retry count:", _Client.RetryAttempts, true, false);
                    _Client.RetryAttempts = newAmount;
                }
                else
                {
                    EmailValidationResult result = _Client.Verify(userInput);
                    Console.WriteLine(SerializeJson(result, true));
                }
            }
        }

        private static string SerializeJson(object obj, bool pretty)
        {
            if (obj == null) return null;
            string json;

            if (pretty)
            {
                json = JsonConvert.SerializeObject(
                  obj,
                  Newtonsoft.Json.Formatting.Indented,
                  new JsonSerializerSettings
                  {
                      NullValueHandling = NullValueHandling.Ignore,
                      DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                  });
            }
            else
            {
                json = JsonConvert.SerializeObject(obj,
                  new JsonSerializerSettings
                  {
                      NullValueHandling = NullValueHandling.Ignore,
                      DateTimeZoneHandling = DateTimeZoneHandling.Utc
                  });
            }

            return json;
        }
    }
}
