using System;
using System.Threading.Tasks;
using NeverBounce;
using GetSomeInput;

namespace Test
{
    internal class Program
    {
        static NeverBounceClient _Client = null;
        static SerializationHelper _Serializer = new SerializationHelper();
        static int? _TimeoutMs = 2000;

        static async Task Main(string[] args)
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
                    Console.WriteLine("  ?               help, this menu");
                    Console.WriteLine("  q               quit");
                    Console.WriteLine("  cls             clear the screen");
                    Console.WriteLine("  retries         set the NeverBounce client retry count");
                    Console.WriteLine("  [value]         verify an email address");
                    Console.WriteLine("  async [value]   verify an email address asynchronously");
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
                else if (userInput.StartsWith("async ") && userInput.Length > 6)
                {
                    await VerifyEmailAsync(userInput.Substring(6));
                }
                else
                {
                    VerifyEmail(userInput);
                }
            }
        }

        static void VerifyEmail(string userInput)
        {
            EmailValidationResult result = _Client.Verify(userInput, null, _TimeoutMs);
            Console.WriteLine(_Serializer.SerializeJson(result, true));
        }

        static async Task VerifyEmailAsync(string userInput)
        {
            EmailValidationResult result = await _Client.VerifyAsync(userInput, null, _TimeoutMs);
            Console.WriteLine(_Serializer.SerializeJson(result, true));
        }
    }
}
