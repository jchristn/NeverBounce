using System;
using System.Threading;
using System.Threading.Tasks;
using RestWrapper;
using Timestamps;

namespace NeverBounce
{
    /// <summary>
    /// NeverBounce client.
    /// </summary>
    public class NeverBounceClient
    {
        #region Public-Members

        /// <summary>
        /// Method to invoke to send log messages.
        /// </summary>
        public Action<string> Logger { get; set; } = null;

        /// <summary>
        /// API key for NeverBounce.
        /// </summary>
        public string ApiKey
        { 
            get
            {
                return _ApiKey;
            }
            set
            {
                if (String.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(ApiKey));
                _ApiKey = value;
            }
        }

        /// <summary>
        /// Endpoint to which requests should be directed.
        /// Must be of the form (http||https)://[hostname]/.
        /// </summary>
        public string Endpoint
        {
            get
            {
                return _Endpoint;
            }
            set
            {
                if (String.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(Endpoint));
                Uri uriResult;
                bool success = Uri.TryCreate(value, UriKind.Absolute, out uriResult)
                    && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);

                if (!success)
                {
                    throw new ArgumentException("The supplied URL is invalid.");
                }

                _Endpoint = value;
            }
        }

        /// <summary>
        /// Header to prepend to log messages.
        /// </summary>
        public string _Header = "[NeverBounce] ";

        /// <summary>
        /// Number of times to retry if a failure result is detected.
        /// </summary>
        public int RetryAttempts
        {
            get
            {
                return _RetryAttempts;
            }
            set
            {
                if (value < 1) throw new ArgumentOutOfRangeException(nameof(RetryAttempts));
                _RetryAttempts = value; 
            }
        }

        #endregion

        #region Private-Members

        private SerializationHelper _Serializer = new SerializationHelper();
        private string _ApiKey = null;
        private string _Endpoint = "https://api.neverbounce.com/v4";
        private int _RetryAttempts = 1;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public NeverBounceClient(string apiKey)
        {
            if (String.IsNullOrEmpty(apiKey)) throw new ArgumentNullException(nameof(apiKey));

            _ApiKey = apiKey;
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Verify an email address.
        /// </summary>
        /// <param name="email">Email address.</param>
        /// <param name="ts">Timestamps.</param>
        /// <param name="timeoutMs">Timeout in milliseconds.</param>
        /// <param name="includeRawResponse">Flag to indicate if the server's raw response should be included.</param>
        /// <param name="retryAttempts">Number of attempts already completed.</param>
        /// <returns>True if verified.</returns>
        public EmailValidationResult Verify(
            string email, 
            Timestamp ts = null,
            int? timeoutMs = null, 
            bool includeRawResponse = false,
            int retryAttempts = 0)
        {
            return VerifyAsync(email, ts, timeoutMs, includeRawResponse, retryAttempts).Result;
        }

        /// <summary>
        /// Verify an email address.
        /// </summary>
        /// <param name="email">Email address.</param>
        /// <param name="ts">Timestamps.</param>
        /// <param name="timeoutMs">Timeout in milliseconds.</param>
        /// <param name="includeRawResponse">Flag to indicate if the server's raw response should be included.</param>
        /// <param name="retryAttempts">Number of attempts already completed.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>True if verified.</returns>
        public async Task<EmailValidationResult> VerifyAsync(
            string email, 
            Timestamp ts = null, 
            int? timeoutMs = null,
            bool includeRawResponse = false,
            int retryAttempts = 0,
            CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(email)) throw new ArgumentNullException(nameof(email));
            if (retryAttempts < 0) throw new ArgumentException("Retry attempts must be zero or greater.");

            Logger?.Invoke(_Header + "requesting validation (" + retryAttempts + "/" + _RetryAttempts + ") for: " + email);

            if (ts == null) ts = new Timestamp();

            if (email.Contains("+")) email = email.Replace("+", "%2B");

            string url = _Endpoint + "/single/check?key=" + _ApiKey + "&email=" + email;

            Logger?.Invoke(_Header + "using URL: " + url.Replace(_ApiKey, "[redacted]"));

            RestRequest req = new RestRequest(url);
            if (timeoutMs != null && timeoutMs.Value > 0) req.TimeoutMilliseconds = timeoutMs.Value;

            RestResponse resp = await req.SendAsync(token).ConfigureAwait(false);
            NeverBounceResult nbr = null;
            EmailValidationResult ret = new EmailValidationResult
            {
                Time = ts
            };

            if (resp != null && resp.StatusCode == 200)
            {
                Logger?.Invoke(_Header + "retrieved " + resp.ContentLength + " bytes with status " + resp.StatusCode + Environment.NewLine + resp.DataAsString);

                try
                {
                    nbr = _Serializer.DeserializeJson<NeverBounceResult>(resp.DataAsString);
                    ret = EmailValidationResult.FromNeverBounceResult(nbr);
                }
                catch (Exception e)
                {
                    Logger?.Invoke(_Header + "exception processing for email " + email + ": " + e.Message);
                    ret.Exception = e;
                }
            }
            else if (resp != null)
            {
                Logger?.Invoke(_Header + "unable to validate email " + email + ", status " + resp.StatusCode + Environment.NewLine + resp.DataAsString);
            }
            else
            {
                Logger?.Invoke(_Header + "unable to validate email " + email + " (unable to retrieve response from server)");
            }

            ret.Time.End = DateTime.Now.ToUniversalTime();

            if (!ret.Valid)
            {
                if (retryAttempts < RetryAttempts)
                {
                    retryAttempts += 1;
                    return Verify(email, ts, retryAttempts);
                }
            }

            if (!includeRawResponse) ret.Raw = null;
            return ret;
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}
