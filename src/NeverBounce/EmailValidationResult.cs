using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NeverBounce
{
    /// <summary>
    /// Email validation result.
    /// </summary>
    public class EmailValidationResult
    {
        #region Public-Members

        /// <summary>
        /// Timestamps.
        /// </summary>
        [JsonProperty(Order = -2)]
        public Timestamps Time { get; set; } = new Timestamps();

        /// <summary>
        /// Indicates if the email address is valid.
        /// </summary>
        [JsonProperty(Order = -1)]
        public bool Valid { get; set; } = false;

        /// <summary>
        /// Additional response data.
        /// </summary>
        [JsonProperty(Order = 990)]
        public EmailValidationFlags Flags { get; set; } = new EmailValidationFlags();

        /// <summary>
        /// Exception data.
        /// </summary>
        [JsonProperty(Order = 991)]
        public Exception Exception { get; set; } = null;

        /// <summary>
        /// Raw response data.
        /// </summary>
        [JsonProperty(Order = 992)]
        public object Raw { get; set; } = null;

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the object.
        /// </summary>
        public EmailValidationResult()
        {

        }

        /// <summary>
        /// Instantiate the object.
        /// </summary>
        /// <param name="resp">NeverBounce result.</param>
        /// <returns>Email validation result.</returns>
        public static EmailValidationResult FromNeverBounceResult(JObject resp)
        {
            EmailValidationResult ret = new EmailValidationResult();
            ret.Raw = resp;

            if (resp != null)
            {
                if (resp.ContainsKey("status")
                    && resp.ContainsKey("result")
                    && resp["status"] != null
                    && resp["result"] != null
                    && !String.IsNullOrEmpty(resp["status"].ToString())
                    && !String.IsNullOrEmpty(resp["result"].ToString()))
                {
                    if (resp.ContainsKey("flags") && resp["flags"] != null)
                    {
                        JArray jArray = (JArray)(resp["flags"]);
                        List<string> flags = jArray.ToObject<List<string>>();

                        foreach (string flag in flags)
                        {
                            if (String.IsNullOrEmpty(flag)) continue;
                            else if (flag.Equals("contains_alias"))
                            {
                                ret.Flags.ContainsAlias = true;
                            }
                            else if (flag.Equals("has_dns"))
                            {
                                ret.Flags.HasDns = true;
                            }
                            else if (flag.Equals("has_dns_mx"))
                            {
                                ret.Flags.HasDnsMx = true;
                            }
                            else if (flag.Equals("free_email_host"))
                            {
                                ret.Flags.IsFreeService = true;
                            }
                            else if (flag.Equals("smtp_connectable"))
                            {
                                ret.Flags.SmtpConnectable = true;
                            }
                            else if (flag.Equals("profanity"))
                            {
                                ret.Flags.ContainsProfanity = true;
                                ret.Valid = false;
                            }
                            else if (flag.Equals("disposable_email"))
                            {
                                ret.Flags.IsDisposableAddress = true;
                                ret.Valid = false;
                            }

                            ret.Flags.AllFlags.Add(flag);
                        }
                    }

                    if (resp["status"].ToString().Equals("success"))
                    {
                        if (resp["result"].ToString().Equals("valid") || resp["result"].ToString().Equals("catchall"))
                        {
                            ret.Valid = true;
                        }
                    }

                    if (ret.Flags.SmtpConnectable != null && ret.Flags.SmtpConnectable.Value)
                    {
                        if (resp["result"].ToString().Equals("unknown") || resp["result"].ToString().Equals("catchall"))
                        {
                            ret.Valid = true;
                        }
                    }
                }
            }

            return ret;
        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion

        #region Public-Embedded-Classes

        /// <summary>
        /// Email validation flags.
        /// </summary>
        public class EmailValidationFlags
        {
            /// <summary>
            /// Flag to indicate if the address contains an alias.
            /// </summary>
            public bool? ContainsAlias { get; set; } = null;

            /// <summary>
            /// A DNS record for the domain was found.
            /// </summary>
            public bool? HasDns { get; set; } = null;

            /// <summary>
            /// An MX record was found for the domain.
            /// </summary>
            public bool? HasDnsMx { get; set; } = null;

            /// <summary>
            /// Email is associated with a domain that provides email as a free service.
            /// </summary>
            public bool? IsFreeService { get; set; } = null;

            /// <summary>
            /// Mail server is connectable using SMTP.
            /// </summary>
            public bool? SmtpConnectable { get; set; } = null;

            /// <summary>
            /// Email contains profanity.
            /// </summary>
            public bool? ContainsProfanity { get; set; } = null;

            /// <summary>
            /// Email is from a domain that provides disposable addresses.
            /// </summary>
            public bool? IsDisposableAddress { get; set; } = null;

            /// <summary>
            /// All flags.
            /// </summary>
            public List<string> AllFlags { get; set; } = new List<string>();

            /// <summary>
            /// Instantiate the object.
            /// </summary>
            public EmailValidationFlags()
            {

            }
        }

        #endregion
    }
}
