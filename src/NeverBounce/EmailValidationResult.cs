using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Timestamps;

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
        [JsonPropertyOrder(-2)]
        public Timestamp Time { get; set; } = new Timestamp();

        /// <summary>
        /// Indicates if the email address is valid.
        /// </summary>
        [JsonPropertyOrder(-1)]
        public bool Valid { get; set; } = false;

        /// <summary>
        /// Additional response data.
        /// </summary>
        [JsonPropertyOrder(990)]
        public EmailValidationFlags Flags { get; set; } = new EmailValidationFlags();

        /// <summary>
        /// Exception data.
        /// </summary>
        [JsonPropertyOrder(991)]
        public Exception Exception { get; set; } = null;

        /// <summary>
        /// Raw response data.
        /// </summary>
        [JsonPropertyOrder(992)]
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
        /// <param name="result">NeverBounce result.</param>
        /// <returns>Email validation result.</returns>
        public static EmailValidationResult FromNeverBounceResult(NeverBounceResult result)
        {
            EmailValidationResult ret = new EmailValidationResult();
            ret.Raw = result;

            if (result != null)
            {
                if (!String.IsNullOrEmpty(result.Status)
                    && !String.IsNullOrEmpty(result.Result))
                {
                    if (result.Flags != null && result.Flags.Count > 0)
                    {
                        foreach (string flag in result.Flags)
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

                    if (result.Status.Equals("success"))
                    {
                        if (result.Result.Equals("valid") || result.Result.Equals("catchall"))
                        {
                            ret.Valid = true;
                        }
                    }

                    if (ret.Flags.SmtpConnectable != null && ret.Flags.SmtpConnectable.Value)
                    {
                        if (result.Result.Equals("unknown") || result.Result.Equals("catchall"))
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
