namespace Test.Shared
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Threading;
    using System.Threading.Tasks;
    using NeverBounce;
    using Touchstone.Core;

    /// <summary>
    /// Touchstone test suite descriptors for the NeverBounce client library.
    /// These descriptors contain no console output; the runner is responsible for output.
    /// Every network-dependent case stands up an in-process mock server bound to 127.0.0.1
    /// so the tests never touch the real NeverBounce service.
    /// </summary>
    public static class NeverBounceSuites
    {
        #region Response-Payloads

        private const string ValidJson = "{\"status\":\"success\",\"result\":\"valid\",\"flags\":[\"has_dns\",\"has_dns_mx\"]}";
        private const string InvalidJson = "{\"status\":\"success\",\"result\":\"invalid\",\"flags\":[]}";
        private const string CatchallJson = "{\"status\":\"success\",\"result\":\"catchall\",\"flags\":[\"has_dns\"]}";
        private const string DisposableJson = "{\"status\":\"success\",\"result\":\"valid\",\"flags\":[\"disposable_email\"]}";
        private const string ProfanityJson = "{\"status\":\"success\",\"result\":\"valid\",\"flags\":[\"profanity\"]}";
        private const string SmtpUnknownJson = "{\"status\":\"success\",\"result\":\"unknown\",\"flags\":[\"smtp_connectable\"]}";
        private const string SmtpCatchallJson = "{\"status\":\"success\",\"result\":\"catchall\",\"flags\":[\"smtp_connectable\"]}";
        private const string AllFlagsJson = "{\"status\":\"success\",\"result\":\"valid\",\"flags\":[\"contains_alias\",\"has_dns\",\"has_dns_mx\",\"free_email_host\",\"smtp_connectable\",\"unknown_flag\",\"\"]}";
        private const string AuthFailureJson = "{\"status\":\"auth_failure\",\"result\":\"\",\"message\":\"invalid api key\"}";

        #endregion

        #region All

        /// <summary>
        /// All suites, exposed so every runner can consume them with a single call.
        /// </summary>
        public static IReadOnlyList<TestSuiteDescriptor> All
        {
            get
            {
                return new List<TestSuiteDescriptor>
                {
                    ClientConfigSuite(),
                    VerifySuite(),
                    ResultMappingSuite(),
                    SerializationSuite()
                };
            }
        }

        #endregion

        #region Helpers

        private static NeverBounceClient BuildClient(MockNeverBounceServer server, int retryAttempts = 1)
        {
            NeverBounceClient client = new NeverBounceClient("test-api-key");
            client.Endpoint = server.Endpoint;
            client.RetryAttempts = retryAttempts;
            return client;
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
        }

        #endregion

        #region ClientConfig-Suite

        /// <summary>
        /// Configuration, constructor, and property-validation cases.
        /// </summary>
        public static TestSuiteDescriptor ClientConfigSuite()
        {
            return new TestSuiteDescriptor(
                suiteId: "ClientConfig",
                displayName: "Client Configuration",
                cases: new List<TestCaseDescriptor>
                {
                    new TestCaseDescriptor("ClientConfig", "CtorValid", "Constructor accepts a valid API key",
                        executeAsync: _ =>
                        {
                            using (NeverBounceClient client = new NeverBounceClient("abc"))
                            {
                                Assert(client.ApiKey == "abc", "API key not stored");
                            }
                            return Task.CompletedTask;
                        }),

                    new TestCaseDescriptor("ClientConfig", "CtorNullThrows", "Constructor with null API key throws",
                        executeAsync: _ =>
                        {
                            try { new NeverBounceClient(null!); throw new Exception("Expected ArgumentNullException"); }
                            catch (ArgumentNullException) { }
                            return Task.CompletedTask;
                        }),

                    new TestCaseDescriptor("ClientConfig", "CtorEmptyThrows", "Constructor with empty API key throws",
                        executeAsync: _ =>
                        {
                            try { new NeverBounceClient(string.Empty); throw new Exception("Expected ArgumentNullException"); }
                            catch (ArgumentNullException) { }
                            return Task.CompletedTask;
                        }),

                    new TestCaseDescriptor("ClientConfig", "ApiKeySetValid", "ApiKey setter accepts a valid value",
                        executeAsync: _ =>
                        {
                            using (NeverBounceClient client = new NeverBounceClient("abc"))
                            {
                                client.ApiKey = "def";
                                Assert(client.ApiKey == "def", "API key not updated");
                            }
                            return Task.CompletedTask;
                        }),

                    new TestCaseDescriptor("ClientConfig", "ApiKeySetNullThrows", "ApiKey setter with null throws",
                        executeAsync: _ =>
                        {
                            using (NeverBounceClient client = new NeverBounceClient("abc"))
                            {
                                try { client.ApiKey = null!; throw new Exception("Expected ArgumentNullException"); }
                                catch (ArgumentNullException) { }
                            }
                            return Task.CompletedTask;
                        }),

                    new TestCaseDescriptor("ClientConfig", "ApiKeySetEmptyThrows", "ApiKey setter with empty string throws",
                        executeAsync: _ =>
                        {
                            using (NeverBounceClient client = new NeverBounceClient("abc"))
                            {
                                try { client.ApiKey = string.Empty; throw new Exception("Expected ArgumentNullException"); }
                                catch (ArgumentNullException) { }
                            }
                            return Task.CompletedTask;
                        }),

                    new TestCaseDescriptor("ClientConfig", "EndpointDefault", "Default endpoint is the v4 production URL",
                        executeAsync: _ =>
                        {
                            using (NeverBounceClient client = new NeverBounceClient("abc"))
                            {
                                Assert(client.Endpoint == "https://api.neverbounce.com/v4", "Unexpected default endpoint");
                            }
                            return Task.CompletedTask;
                        }),

                    new TestCaseDescriptor("ClientConfig", "EndpointSetHttp", "Endpoint setter accepts an http URL",
                        executeAsync: _ =>
                        {
                            using (NeverBounceClient client = new NeverBounceClient("abc"))
                            {
                                client.Endpoint = "http://127.0.0.1:8080/";
                                Assert(client.Endpoint == "http://127.0.0.1:8080/", "http endpoint not stored");
                            }
                            return Task.CompletedTask;
                        }),

                    new TestCaseDescriptor("ClientConfig", "EndpointSetHttps", "Endpoint setter accepts an https URL",
                        executeAsync: _ =>
                        {
                            using (NeverBounceClient client = new NeverBounceClient("abc"))
                            {
                                client.Endpoint = "https://example.com/api";
                                Assert(client.Endpoint == "https://example.com/api", "https endpoint not stored");
                            }
                            return Task.CompletedTask;
                        }),

                    new TestCaseDescriptor("ClientConfig", "EndpointSetNullThrows", "Endpoint setter with null throws",
                        executeAsync: _ =>
                        {
                            using (NeverBounceClient client = new NeverBounceClient("abc"))
                            {
                                try { client.Endpoint = null!; throw new Exception("Expected ArgumentNullException"); }
                                catch (ArgumentNullException) { }
                            }
                            return Task.CompletedTask;
                        }),

                    new TestCaseDescriptor("ClientConfig", "EndpointSetInvalidThrows", "Endpoint setter with a non-URL throws",
                        executeAsync: _ =>
                        {
                            using (NeverBounceClient client = new NeverBounceClient("abc"))
                            {
                                try { client.Endpoint = "not a url"; throw new Exception("Expected ArgumentException"); }
                                catch (ArgumentException) { }
                            }
                            return Task.CompletedTask;
                        }),

                    new TestCaseDescriptor("ClientConfig", "EndpointSetBadSchemeThrows", "Endpoint setter with a non-http(s) scheme throws",
                        executeAsync: _ =>
                        {
                            using (NeverBounceClient client = new NeverBounceClient("abc"))
                            {
                                try { client.Endpoint = "ftp://example.com/"; throw new Exception("Expected ArgumentException"); }
                                catch (ArgumentException) { }
                            }
                            return Task.CompletedTask;
                        }),

                    new TestCaseDescriptor("ClientConfig", "RetryAttemptsDefault", "RetryAttempts defaults to one",
                        executeAsync: _ =>
                        {
                            using (NeverBounceClient client = new NeverBounceClient("abc"))
                            {
                                Assert(client.RetryAttempts == 1, "Expected default RetryAttempts of 1");
                            }
                            return Task.CompletedTask;
                        }),

                    new TestCaseDescriptor("ClientConfig", "RetryAttemptsSetValid", "RetryAttempts setter accepts a positive value",
                        executeAsync: _ =>
                        {
                            using (NeverBounceClient client = new NeverBounceClient("abc"))
                            {
                                client.RetryAttempts = 5;
                                Assert(client.RetryAttempts == 5, "Retry attempts not stored");
                            }
                            return Task.CompletedTask;
                        }),

                    new TestCaseDescriptor("ClientConfig", "RetryAttemptsZeroThrows", "RetryAttempts setter with zero throws",
                        executeAsync: _ =>
                        {
                            using (NeverBounceClient client = new NeverBounceClient("abc"))
                            {
                                try { client.RetryAttempts = 0; throw new Exception("Expected ArgumentOutOfRangeException"); }
                                catch (ArgumentOutOfRangeException) { }
                            }
                            return Task.CompletedTask;
                        }),

                    new TestCaseDescriptor("ClientConfig", "RetryAttemptsNegativeThrows", "RetryAttempts setter with a negative value throws",
                        executeAsync: _ =>
                        {
                            using (NeverBounceClient client = new NeverBounceClient("abc"))
                            {
                                try { client.RetryAttempts = -3; throw new Exception("Expected ArgumentOutOfRangeException"); }
                                catch (ArgumentOutOfRangeException) { }
                            }
                            return Task.CompletedTask;
                        }),

                    new TestCaseDescriptor("ClientConfig", "LoggerAssignable", "Logger property can be assigned",
                        executeAsync: _ =>
                        {
                            using (NeverBounceClient client = new NeverBounceClient("abc"))
                            {
                                client.Logger = s => { };
                                Assert(client.Logger != null, "Logger not assignable");
                            }
                            return Task.CompletedTask;
                        }),

                    new TestCaseDescriptor("ClientConfig", "DisposeIdempotent", "Dispose can be called more than once",
                        executeAsync: _ =>
                        {
                            NeverBounceClient client = new NeverBounceClient("abc");
                            client.Dispose();
                            client.Dispose();
                            return Task.CompletedTask;
                        })
                });
        }

        #endregion

        #region Verify-Suite

        /// <summary>
        /// End-to-end verification cases exercised against the in-process mock server.
        /// </summary>
        public static TestSuiteDescriptor VerifySuite()
        {
            return new TestSuiteDescriptor(
                suiteId: "Verify",
                displayName: "Email Verification",
                cases: new List<TestCaseDescriptor>
                {
                    new TestCaseDescriptor("Verify", "ValidEmail", "Valid result marks the email valid",
                        executeAsync: async ct =>
                        {
                            using (MockNeverBounceServer server = MockNeverBounceServer.Constant(200, ValidJson))
                            using (NeverBounceClient client = BuildClient(server))
                            {
                                EmailValidationResult result = await client.VerifyAsync("good@example.com", token: ct).ConfigureAwait(false);
                                Assert(result.Valid, "Expected valid");
                                Assert(server.RequestCount == 1, "Expected exactly one request");
                                Assert(result.Flags.HasDns == true, "Expected HasDns flag");
                                Assert(result.Flags.HasDnsMx == true, "Expected HasDnsMx flag");
                            }
                        }),

                    new TestCaseDescriptor("Verify", "InvalidEmail", "Invalid result marks the email invalid",
                        executeAsync: async ct =>
                        {
                            using (MockNeverBounceServer server = MockNeverBounceServer.Constant(200, InvalidJson))
                            using (NeverBounceClient client = BuildClient(server))
                            {
                                EmailValidationResult result = await client.VerifyAsync("bad@example.com", token: ct).ConfigureAwait(false);
                                Assert(!result.Valid, "Expected invalid");
                            }
                        }),

                    new TestCaseDescriptor("Verify", "CatchallEmail", "Catchall result marks the email valid",
                        executeAsync: async ct =>
                        {
                            using (MockNeverBounceServer server = MockNeverBounceServer.Constant(200, CatchallJson))
                            using (NeverBounceClient client = BuildClient(server))
                            {
                                EmailValidationResult result = await client.VerifyAsync("catch@example.com", token: ct).ConfigureAwait(false);
                                Assert(result.Valid, "Expected valid for catchall");
                            }
                        }),

                    new TestCaseDescriptor("Verify", "DisposableEmail", "Disposable flag forces invalid",
                        executeAsync: async ct =>
                        {
                            using (MockNeverBounceServer server = MockNeverBounceServer.Constant(200, DisposableJson))
                            using (NeverBounceClient client = BuildClient(server, retryAttempts: 1))
                            {
                                EmailValidationResult result = await client.VerifyAsync("temp@example.com", token: ct).ConfigureAwait(false);
                                Assert(!result.Valid, "Expected invalid for disposable");
                                Assert(result.Flags.IsDisposableAddress == true, "Expected IsDisposableAddress flag");
                            }
                        }),

                    new TestCaseDescriptor("Verify", "ProfanityEmail", "Profanity flag forces invalid",
                        executeAsync: async ct =>
                        {
                            using (MockNeverBounceServer server = MockNeverBounceServer.Constant(200, ProfanityJson))
                            using (NeverBounceClient client = BuildClient(server, retryAttempts: 1))
                            {
                                EmailValidationResult result = await client.VerifyAsync("rude@example.com", token: ct).ConfigureAwait(false);
                                Assert(!result.Valid, "Expected invalid for profanity");
                                Assert(result.Flags.ContainsProfanity == true, "Expected ContainsProfanity flag");
                            }
                        }),

                    new TestCaseDescriptor("Verify", "SmtpConnectableUnknown", "smtp_connectable + unknown result is valid",
                        executeAsync: async ct =>
                        {
                            using (MockNeverBounceServer server = MockNeverBounceServer.Constant(200, SmtpUnknownJson))
                            using (NeverBounceClient client = BuildClient(server))
                            {
                                EmailValidationResult result = await client.VerifyAsync("maybe@example.com", token: ct).ConfigureAwait(false);
                                Assert(result.Valid, "Expected valid when smtp_connectable and result unknown");
                                Assert(result.Flags.SmtpConnectable == true, "Expected SmtpConnectable flag");
                            }
                        }),

                    new TestCaseDescriptor("Verify", "SmtpConnectableCatchall", "smtp_connectable + catchall result is valid",
                        executeAsync: async ct =>
                        {
                            using (MockNeverBounceServer server = MockNeverBounceServer.Constant(200, SmtpCatchallJson))
                            using (NeverBounceClient client = BuildClient(server))
                            {
                                EmailValidationResult result = await client.VerifyAsync("maybe2@example.com", token: ct).ConfigureAwait(false);
                                Assert(result.Valid, "Expected valid when smtp_connectable and result catchall");
                            }
                        }),

                    new TestCaseDescriptor("Verify", "AllFlagsParsed", "All recognized flags are parsed",
                        executeAsync: async ct =>
                        {
                            using (MockNeverBounceServer server = MockNeverBounceServer.Constant(200, AllFlagsJson))
                            using (NeverBounceClient client = BuildClient(server))
                            {
                                EmailValidationResult result = await client.VerifyAsync("flags@example.com", token: ct).ConfigureAwait(false);
                                Assert(result.Valid, "Expected valid");
                                Assert(result.Flags.ContainsAlias == true, "Expected ContainsAlias");
                                Assert(result.Flags.HasDns == true, "Expected HasDns");
                                Assert(result.Flags.HasDnsMx == true, "Expected HasDnsMx");
                                Assert(result.Flags.IsFreeService == true, "Expected IsFreeService");
                                Assert(result.Flags.SmtpConnectable == true, "Expected SmtpConnectable");
                                // unknown_flag is preserved in AllFlags; the empty flag is skipped.
                                Assert(result.Flags.AllFlags.Contains("unknown_flag"), "Expected unknown flag preserved");
                                Assert(!result.Flags.AllFlags.Contains(string.Empty), "Empty flag should be skipped");
                            }
                        }),

                    new TestCaseDescriptor("Verify", "PlusEncoded", "Plus sign in the address is percent-encoded",
                        executeAsync: async ct =>
                        {
                            using (MockNeverBounceServer server = MockNeverBounceServer.Constant(200, ValidJson))
                            using (NeverBounceClient client = BuildClient(server))
                            {
                                await client.VerifyAsync("user+tag@example.com", token: ct).ConfigureAwait(false);
                                Assert(server.LastRawUrl != null && server.LastRawUrl.Contains("%2B"),
                                    "Expected plus sign to be encoded as %2B");
                                Assert(server.LastRawUrl != null && !server.LastRawUrl.Contains("user+tag"),
                                    "Raw plus sign should not appear in the URL");
                            }
                        }),

                    new TestCaseDescriptor("Verify", "IncludeRawResponse", "Raw response is retained when requested",
                        executeAsync: async ct =>
                        {
                            using (MockNeverBounceServer server = MockNeverBounceServer.Constant(200, ValidJson))
                            using (NeverBounceClient client = BuildClient(server))
                            {
                                EmailValidationResult result = await client.VerifyAsync("raw@example.com", includeRawResponse: true, token: ct).ConfigureAwait(false);
                                Assert(result.Raw != null, "Expected raw response to be retained");
                            }
                        }),

                    new TestCaseDescriptor("Verify", "ExcludeRawResponse", "Raw response is dropped by default",
                        executeAsync: async ct =>
                        {
                            using (MockNeverBounceServer server = MockNeverBounceServer.Constant(200, ValidJson))
                            using (NeverBounceClient client = BuildClient(server))
                            {
                                EmailValidationResult result = await client.VerifyAsync("noraw@example.com", includeRawResponse: false, token: ct).ConfigureAwait(false);
                                Assert(result.Raw == null, "Expected raw response to be dropped");
                            }
                        }),

                    new TestCaseDescriptor("Verify", "RetriesExhausted", "Invalid results are retried the configured number of times",
                        executeAsync: async ct =>
                        {
                            using (MockNeverBounceServer server = MockNeverBounceServer.Constant(200, InvalidJson))
                            using (NeverBounceClient client = BuildClient(server, retryAttempts: 3))
                            {
                                EmailValidationResult result = await client.VerifyAsync("retry@example.com", token: ct).ConfigureAwait(false);
                                Assert(!result.Valid, "Expected invalid");
                                Assert(server.RequestCount == 4, "Expected 1 initial + 3 retries = 4 requests, got " + server.RequestCount);
                            }
                        }),

                    new TestCaseDescriptor("Verify", "RetryThenSuccess", "Retry stops once a valid result is returned",
                        executeAsync: async ct =>
                        {
                            using (MockNeverBounceServer server = new MockNeverBounceServer((idx, url) =>
                                new MockResponse { StatusCode = 200, Body = idx == 0 ? InvalidJson : ValidJson }))
                            using (NeverBounceClient client = BuildClient(server, retryAttempts: 3))
                            {
                                EmailValidationResult result = await client.VerifyAsync("eventual@example.com", token: ct).ConfigureAwait(false);
                                Assert(result.Valid, "Expected valid after retry");
                                Assert(server.RequestCount == 2, "Expected 2 requests, got " + server.RequestCount);
                            }
                        }),

                    new TestCaseDescriptor("Verify", "RetryParameterAtMaxNoRetry", "A retryAttempts argument already at the limit performs no retry",
                        executeAsync: async ct =>
                        {
                            using (MockNeverBounceServer server = MockNeverBounceServer.Constant(200, InvalidJson))
                            using (NeverBounceClient client = BuildClient(server, retryAttempts: 3))
                            {
                                EmailValidationResult result = await client.VerifyAsync("nomore@example.com", retryAttempts: 3, token: ct).ConfigureAwait(false);
                                Assert(!result.Valid, "Expected invalid");
                                Assert(server.RequestCount == 1, "Expected a single request when starting at the retry limit, got " + server.RequestCount);
                            }
                        }),

                    new TestCaseDescriptor("Verify", "ValidFirstTryNoRetry", "A valid first response is not retried",
                        executeAsync: async ct =>
                        {
                            using (MockNeverBounceServer server = MockNeverBounceServer.Constant(200, ValidJson))
                            using (NeverBounceClient client = BuildClient(server, retryAttempts: 3))
                            {
                                EmailValidationResult result = await client.VerifyAsync("once@example.com", token: ct).ConfigureAwait(false);
                                Assert(result.Valid, "Expected valid");
                                Assert(server.RequestCount == 1, "Expected exactly one request for a valid first try, got " + server.RequestCount);
                            }
                        }),

                    new TestCaseDescriptor("Verify", "NonSuccessStatusCode", "Non-200 status yields an invalid result without throwing",
                        executeAsync: async ct =>
                        {
                            using (MockNeverBounceServer server = MockNeverBounceServer.Constant(400, AuthFailureJson))
                            using (NeverBounceClient client = BuildClient(server, retryAttempts: 1))
                            {
                                EmailValidationResult result = await client.VerifyAsync("err@example.com", token: ct).ConfigureAwait(false);
                                Assert(!result.Valid, "Expected invalid on non-200");
                            }
                        }),

                    new TestCaseDescriptor("Verify", "ChunkedResponse", "Chunked transfer-encoding responses are reassembled",
                        executeAsync: async ct =>
                        {
                            using (MockNeverBounceServer server = MockNeverBounceServer.Constant(200, ValidJson, chunked: true))
                            using (NeverBounceClient client = BuildClient(server))
                            {
                                EmailValidationResult result = await client.VerifyAsync("chunk@example.com", token: ct).ConfigureAwait(false);
                                Assert(result.Valid, "Expected valid from chunked response");
                            }
                        }),

                    new TestCaseDescriptor("Verify", "MalformedJson", "Malformed JSON is captured as an exception",
                        executeAsync: async ct =>
                        {
                            using (MockNeverBounceServer server = MockNeverBounceServer.Constant(200, "{ this is not json"))
                            using (NeverBounceClient client = BuildClient(server, retryAttempts: 1))
                            {
                                EmailValidationResult result = await client.VerifyAsync("bad@example.com", token: ct).ConfigureAwait(false);
                                Assert(!result.Valid, "Expected invalid on malformed json");
                                Assert(result.Exception != null, "Expected exception captured");
                            }
                        }),

                    new TestCaseDescriptor("Verify", "AuthFailureStatus", "auth_failure status yields an invalid result",
                        executeAsync: async ct =>
                        {
                            using (MockNeverBounceServer server = MockNeverBounceServer.Constant(200, AuthFailureJson))
                            using (NeverBounceClient client = BuildClient(server, retryAttempts: 1))
                            {
                                EmailValidationResult result = await client.VerifyAsync("auth@example.com", token: ct).ConfigureAwait(false);
                                Assert(!result.Valid, "Expected invalid on auth_failure");
                            }
                        }),

                    new TestCaseDescriptor("Verify", "LoggerInvoked", "Logger receives messages during verification",
                        executeAsync: async ct =>
                        {
                            List<string> messages = new List<string>();
                            using (MockNeverBounceServer server = MockNeverBounceServer.Constant(200, ValidJson))
                            using (NeverBounceClient client = BuildClient(server))
                            {
                                client.Logger = m => { lock (messages) messages.Add(m); };
                                await client.VerifyAsync("log@example.com", token: ct).ConfigureAwait(false);
                                Assert(messages.Count > 0, "Expected logger to be invoked");
                            }
                        }),

                    new TestCaseDescriptor("Verify", "ApiKeyRedactedInLog", "API key is redacted in log output",
                        executeAsync: async ct =>
                        {
                            List<string> messages = new List<string>();
                            using (MockNeverBounceServer server = MockNeverBounceServer.Constant(200, ValidJson))
                            using (NeverBounceClient client = BuildClient(server))
                            {
                                client.Logger = m => { lock (messages) messages.Add(m); };
                                await client.VerifyAsync("redact@example.com", token: ct).ConfigureAwait(false);
                                lock (messages)
                                {
                                    foreach (string m in messages)
                                        Assert(!m.Contains("test-api-key"), "API key leaked into log: " + m);
                                }
                            }
                        }),

                    new TestCaseDescriptor("Verify", "SyncWrapper", "Synchronous Verify wrapper returns a result",
                        executeAsync: _ =>
                        {
                            using (MockNeverBounceServer server = MockNeverBounceServer.Constant(200, ValidJson))
                            using (NeverBounceClient client = BuildClient(server))
                            {
                                EmailValidationResult result = client.Verify("sync@example.com");
                                Assert(result.Valid, "Expected valid from sync wrapper");
                            }
                            return Task.CompletedTask;
                        }),

                    new TestCaseDescriptor("Verify", "CustomTimeout", "A custom timeout is honored for a fast response",
                        executeAsync: async ct =>
                        {
                            using (MockNeverBounceServer server = MockNeverBounceServer.Constant(200, ValidJson))
                            using (NeverBounceClient client = BuildClient(server))
                            {
                                EmailValidationResult result = await client.VerifyAsync("timeout@example.com", timeoutMs: 5000, token: ct).ConfigureAwait(false);
                                Assert(result.Valid, "Expected valid within timeout");
                            }
                        }),

                    new TestCaseDescriptor("Verify", "DisposedThrows", "Verify after Dispose throws ObjectDisposedException",
                        executeAsync: async ct =>
                        {
                            NeverBounceClient client = new NeverBounceClient("abc");
                            client.Dispose();
                            try
                            {
                                await client.VerifyAsync("x@example.com", token: ct).ConfigureAwait(false);
                                throw new Exception("Expected ObjectDisposedException");
                            }
                            catch (ObjectDisposedException) { }
                        }),

                    new TestCaseDescriptor("Verify", "NullEmailThrows", "Verify with null email throws ArgumentNullException",
                        executeAsync: async ct =>
                        {
                            using (NeverBounceClient client = new NeverBounceClient("abc"))
                            {
                                try
                                {
                                    await client.VerifyAsync(null!, token: ct).ConfigureAwait(false);
                                    throw new Exception("Expected ArgumentNullException");
                                }
                                catch (ArgumentNullException) { }
                            }
                        }),

                    new TestCaseDescriptor("Verify", "EmptyEmailThrows", "Verify with empty email throws ArgumentNullException",
                        executeAsync: async ct =>
                        {
                            using (NeverBounceClient client = new NeverBounceClient("abc"))
                            {
                                try
                                {
                                    await client.VerifyAsync(string.Empty, token: ct).ConfigureAwait(false);
                                    throw new Exception("Expected ArgumentNullException");
                                }
                                catch (ArgumentNullException) { }
                            }
                        }),

                    new TestCaseDescriptor("Verify", "NegativeRetryThrows", "Verify with a negative retry count throws ArgumentException",
                        executeAsync: async ct =>
                        {
                            using (NeverBounceClient client = new NeverBounceClient("abc"))
                            {
                                try
                                {
                                    await client.VerifyAsync("x@example.com", retryAttempts: -1, token: ct).ConfigureAwait(false);
                                    throw new Exception("Expected ArgumentException");
                                }
                                catch (ArgumentException) { }
                            }
                        }),

                    new TestCaseDescriptor("Verify", "CancellationHonored", "A cancelled token aborts the request",
                        executeAsync: async _ =>
                        {
                            using (MockNeverBounceServer server = MockNeverBounceServer.Constant(200, ValidJson, delayMs: 2000))
                            using (NeverBounceClient client = BuildClient(server))
                            using (CancellationTokenSource cts = new CancellationTokenSource())
                            {
                                cts.Cancel();
                                try
                                {
                                    await client.VerifyAsync("cancel@example.com", token: cts.Token).ConfigureAwait(false);
                                    throw new Exception("Expected OperationCanceledException");
                                }
                                catch (OperationCanceledException) { }
                            }
                        })
                });
        }

        #endregion

        #region ResultMapping-Suite

        /// <summary>
        /// Pure unit cases for EmailValidationResult.FromNeverBounceResult and NeverBounceResult.
        /// </summary>
        public static TestSuiteDescriptor ResultMappingSuite()
        {
            return new TestSuiteDescriptor(
                suiteId: "ResultMapping",
                displayName: "Result Mapping",
                cases: new List<TestCaseDescriptor>
                {
                    new TestCaseDescriptor("ResultMapping", "FromNull", "Mapping a null result is invalid",
                        executeAsync: _ =>
                        {
                            EmailValidationResult r = EmailValidationResult.FromNeverBounceResult(null!);
                            Assert(!r.Valid, "Expected invalid for null");
                            return Task.CompletedTask;
                        }),

                    new TestCaseDescriptor("ResultMapping", "FromValid", "Mapping success/valid is valid",
                        executeAsync: _ =>
                        {
                            NeverBounceResult nbr = new NeverBounceResult { Status = "success", Result = "valid" };
                            EmailValidationResult r = EmailValidationResult.FromNeverBounceResult(nbr);
                            Assert(r.Valid, "Expected valid");
                            return Task.CompletedTask;
                        }),

                    new TestCaseDescriptor("ResultMapping", "FromCatchall", "Mapping success/catchall is valid",
                        executeAsync: _ =>
                        {
                            NeverBounceResult nbr = new NeverBounceResult { Status = "success", Result = "catchall" };
                            EmailValidationResult r = EmailValidationResult.FromNeverBounceResult(nbr);
                            Assert(r.Valid, "Expected valid");
                            return Task.CompletedTask;
                        }),

                    new TestCaseDescriptor("ResultMapping", "FromInvalid", "Mapping success/invalid is invalid",
                        executeAsync: _ =>
                        {
                            NeverBounceResult nbr = new NeverBounceResult { Status = "success", Result = "invalid" };
                            EmailValidationResult r = EmailValidationResult.FromNeverBounceResult(nbr);
                            Assert(!r.Valid, "Expected invalid");
                            return Task.CompletedTask;
                        }),

                    new TestCaseDescriptor("ResultMapping", "FromEmptyStatus", "Mapping an empty status is invalid",
                        executeAsync: _ =>
                        {
                            NeverBounceResult nbr = new NeverBounceResult { Status = string.Empty, Result = "valid" };
                            EmailValidationResult r = EmailValidationResult.FromNeverBounceResult(nbr);
                            Assert(!r.Valid, "Expected invalid when status is empty");
                            return Task.CompletedTask;
                        }),

                    new TestCaseDescriptor("ResultMapping", "ProfanityOverridesValid", "Profanity flag overrides a valid result",
                        executeAsync: _ =>
                        {
                            NeverBounceResult nbr = new NeverBounceResult
                            {
                                Status = "success",
                                Result = "valid",
                                Flags = new List<string> { "profanity" }
                            };
                            EmailValidationResult r = EmailValidationResult.FromNeverBounceResult(nbr);
                            Assert(!r.Valid, "Expected profanity to force invalid");
                            Assert(r.Flags.ContainsProfanity == true, "Expected ContainsProfanity");
                            return Task.CompletedTask;
                        }),

                    new TestCaseDescriptor("ResultMapping", "DisposableOverridesValid", "Disposable flag overrides a valid result",
                        executeAsync: _ =>
                        {
                            NeverBounceResult nbr = new NeverBounceResult
                            {
                                Status = "success",
                                Result = "valid",
                                Flags = new List<string> { "disposable_email" }
                            };
                            EmailValidationResult r = EmailValidationResult.FromNeverBounceResult(nbr);
                            Assert(!r.Valid, "Expected disposable to force invalid");
                            Assert(r.Flags.IsDisposableAddress == true, "Expected IsDisposableAddress");
                            return Task.CompletedTask;
                        }),

                    new TestCaseDescriptor("ResultMapping", "UnknownFlagPreserved", "Unknown flags are preserved in AllFlags",
                        executeAsync: _ =>
                        {
                            NeverBounceResult nbr = new NeverBounceResult
                            {
                                Status = "success",
                                Result = "valid",
                                Flags = new List<string> { "some_new_flag" }
                            };
                            EmailValidationResult r = EmailValidationResult.FromNeverBounceResult(nbr);
                            Assert(r.Flags.AllFlags.Contains("some_new_flag"), "Expected unknown flag preserved");
                            return Task.CompletedTask;
                        }),

                    new TestCaseDescriptor("ResultMapping", "UnknownWithoutSmtpInvalid", "success/unknown without smtp_connectable is invalid",
                        executeAsync: _ =>
                        {
                            NeverBounceResult nbr = new NeverBounceResult { Status = "success", Result = "unknown" };
                            EmailValidationResult r = EmailValidationResult.FromNeverBounceResult(nbr);
                            Assert(!r.Valid, "Expected invalid for unknown without smtp_connectable");
                            return Task.CompletedTask;
                        }),

                    new TestCaseDescriptor("ResultMapping", "NonSuccessStatusValidResultInvalid", "A non-success status is invalid even when result is valid",
                        executeAsync: _ =>
                        {
                            NeverBounceResult nbr = new NeverBounceResult { Status = "general_failure", Result = "valid" };
                            EmailValidationResult r = EmailValidationResult.FromNeverBounceResult(nbr);
                            Assert(!r.Valid, "Expected invalid when status is not success");
                            return Task.CompletedTask;
                        }),

                    new TestCaseDescriptor("ResultMapping", "SmtpConnectableInvalidStaysInvalid", "smtp_connectable does not rescue an invalid result",
                        executeAsync: _ =>
                        {
                            NeverBounceResult nbr = new NeverBounceResult
                            {
                                Status = "success",
                                Result = "invalid",
                                Flags = new List<string> { "smtp_connectable" }
                            };
                            EmailValidationResult r = EmailValidationResult.FromNeverBounceResult(nbr);
                            Assert(!r.Valid, "Expected invalid; smtp_connectable only applies to unknown/catchall");
                            Assert(r.Flags.SmtpConnectable == true, "Expected SmtpConnectable flag");
                            return Task.CompletedTask;
                        }),

                    new TestCaseDescriptor("ResultMapping", "SmtpConnectableUnknownValid", "smtp_connectable rescues an unknown result",
                        executeAsync: _ =>
                        {
                            NeverBounceResult nbr = new NeverBounceResult
                            {
                                Status = "success",
                                Result = "unknown",
                                Flags = new List<string> { "smtp_connectable" }
                            };
                            EmailValidationResult r = EmailValidationResult.FromNeverBounceResult(nbr);
                            Assert(r.Valid, "Expected valid when smtp_connectable and result unknown");
                            return Task.CompletedTask;
                        }),

                    new TestCaseDescriptor("ResultMapping", "DisposableOverridesSmtpValid", "Disposable flag overrides an smtp-derived valid",
                        executeAsync: _ =>
                        {
                            NeverBounceResult nbr = new NeverBounceResult
                            {
                                Status = "success",
                                Result = "catchall",
                                Flags = new List<string> { "smtp_connectable", "disposable_email" }
                            };
                            EmailValidationResult r = EmailValidationResult.FromNeverBounceResult(nbr);
                            Assert(!r.Valid, "Expected disposable to override an smtp/catchall valid");
                            Assert(r.Flags.IsDisposableAddress == true, "Expected IsDisposableAddress flag");
                            return Task.CompletedTask;
                        }),

                    new TestCaseDescriptor("ResultMapping", "FreeEmailHostMapsToIsFreeService", "free_email_host maps to IsFreeService",
                        executeAsync: _ =>
                        {
                            NeverBounceResult nbr = new NeverBounceResult
                            {
                                Status = "success",
                                Result = "valid",
                                Flags = new List<string> { "free_email_host" }
                            };
                            EmailValidationResult r = EmailValidationResult.FromNeverBounceResult(nbr);
                            Assert(r.Valid, "Expected valid");
                            Assert(r.Flags.IsFreeService == true, "Expected IsFreeService flag");
                            return Task.CompletedTask;
                        }),

                    new TestCaseDescriptor("ResultMapping", "ContainsAliasMapsFlag", "contains_alias maps to ContainsAlias",
                        executeAsync: _ =>
                        {
                            NeverBounceResult nbr = new NeverBounceResult
                            {
                                Status = "success",
                                Result = "valid",
                                Flags = new List<string> { "contains_alias" }
                            };
                            EmailValidationResult r = EmailValidationResult.FromNeverBounceResult(nbr);
                            Assert(r.Flags.ContainsAlias == true, "Expected ContainsAlias flag");
                            return Task.CompletedTask;
                        }),

                    new TestCaseDescriptor("ResultMapping", "EmptyResultInvalid", "An empty result string is invalid",
                        executeAsync: _ =>
                        {
                            NeverBounceResult nbr = new NeverBounceResult { Status = "success", Result = string.Empty };
                            EmailValidationResult r = EmailValidationResult.FromNeverBounceResult(nbr);
                            Assert(!r.Valid, "Expected invalid when result is empty");
                            return Task.CompletedTask;
                        }),

                    new TestCaseDescriptor("ResultMapping", "NbrFlagsNullBecomesEmpty", "NeverBounceResult.Flags never returns null",
                        executeAsync: _ =>
                        {
                            NeverBounceResult nbr = new NeverBounceResult { Flags = null! };
                            List<string> flags = nbr.Flags;
                            Assert(flags != null, "Flags should not be null");
                            Assert(flags!.Count == 0, "Flags should be empty");
                            return Task.CompletedTask;
                        }),

                    new TestCaseDescriptor("ResultMapping", "NbrFlagsPreserved", "NeverBounceResult.Flags retains an assigned list",
                        executeAsync: _ =>
                        {
                            NeverBounceResult nbr = new NeverBounceResult { Flags = new List<string> { "a", "b" } };
                            Assert(nbr.Flags.Count == 2, "Flags should retain assigned values");
                            return Task.CompletedTask;
                        }),

                    new TestCaseDescriptor("ResultMapping", "DefaultResultInvalid", "A default EmailValidationResult is invalid",
                        executeAsync: _ =>
                        {
                            EmailValidationResult r = new EmailValidationResult();
                            Assert(!r.Valid, "Expected default to be invalid");
                            Assert(r.Time != null, "Expected Time to be initialized");
                            Assert(r.Flags != null, "Expected Flags to be initialized");
                            return Task.CompletedTask;
                        })
                });
        }

        #endregion

        #region Serialization-Suite

        /// <summary>
        /// Cases covering the internal SerializationHelper (JSON and XML paths).
        /// </summary>
        public static TestSuiteDescriptor SerializationSuite()
        {
            return new TestSuiteDescriptor(
                suiteId: "Serialization",
                displayName: "Serialization Helper",
                cases: new List<TestCaseDescriptor>
                {
                    new TestCaseDescriptor("Serialization", "SerializeJsonNull", "Serializing null returns null",
                        executeAsync: _ =>
                        {
                            SerializationHelper helper = new SerializationHelper();
                            string? json = helper.SerializeJson(null!, false);
                            Assert(json == null, "Expected null");
                            return Task.CompletedTask;
                        }),

                    new TestCaseDescriptor("Serialization", "SerializeJsonPretty", "Pretty JSON is indented",
                        executeAsync: _ =>
                        {
                            SerializationHelper helper = new SerializationHelper();
                            string json = helper.SerializeJson(new NeverBounceResult { Status = "success", Result = "valid" }, true);
                            Assert(json.Contains("\n"), "Expected multi-line pretty output");
                            return Task.CompletedTask;
                        }),

                    new TestCaseDescriptor("Serialization", "SerializeJsonCompact", "Compact JSON is single-line",
                        executeAsync: _ =>
                        {
                            SerializationHelper helper = new SerializationHelper();
                            string json = helper.SerializeJson(new NeverBounceResult { Status = "success", Result = "valid" }, false);
                            Assert(!json.Contains("\n"), "Expected single-line compact output");
                            return Task.CompletedTask;
                        }),

                    new TestCaseDescriptor("Serialization", "DeserializeJsonRoundtrip", "JSON round-trips into a typed object",
                        executeAsync: _ =>
                        {
                            SerializationHelper helper = new SerializationHelper();
                            NeverBounceResult nbr = helper.DeserializeJson<NeverBounceResult>(
                                "{\"status\":\"success\",\"result\":\"valid\",\"flags\":[\"has_dns\"]}");
                            Assert(nbr.Status == "success", "Status mismatch");
                            Assert(nbr.Result == "valid", "Result mismatch");
                            Assert(nbr.Flags.Count == 1, "Flags mismatch");
                            return Task.CompletedTask;
                        }),

                    new TestCaseDescriptor("Serialization", "DeserializeJsonTrailingComma", "Trailing commas and comments are tolerated",
                        executeAsync: _ =>
                        {
                            SerializationHelper helper = new SerializationHelper();
                            NeverBounceResult nbr = helper.DeserializeJson<NeverBounceResult>(
                                "{\"status\":\"success\", /* comment */ \"result\":\"valid\",}");
                            Assert(nbr.Result == "valid", "Result mismatch");
                            return Task.CompletedTask;
                        }),

                    new TestCaseDescriptor("Serialization", "SerializeExceptionConverter", "Objects carrying an Exception serialize",
                        executeAsync: _ =>
                        {
                            SerializationHelper helper = new SerializationHelper();
                            EmailValidationResult r = new EmailValidationResult { Exception = new InvalidOperationException("boom") };
                            string json = helper.SerializeJson(r, false);
                            Assert(json.Contains("boom"), "Expected exception message in output");
                            return Task.CompletedTask;
                        }),

                    new TestCaseDescriptor("Serialization", "SerializeNameValueCollection", "NameValueCollection serializes via the custom converter",
                        executeAsync: _ =>
                        {
                            SerializationHelper helper = new SerializationHelper();
                            NvcSample sample = new NvcSample();
                            sample.Values.Add("k", "v");
                            string json = helper.SerializeJson(sample, false);
                            Assert(json.Contains("\"k\""), "Expected key in output");
                            Assert(json.Contains("\"v\""), "Expected value in output");
                            return Task.CompletedTask;
                        }),

                    new TestCaseDescriptor("Serialization", "XmlRoundtrip", "XML round-trips into a typed object",
                        executeAsync: _ =>
                        {
                            SerializationHelper helper = new SerializationHelper();
                            XmlSample original = new XmlSample { Name = "Widget", Count = 7 };
                            string xml = helper.SerializeXml(original, true);
                            XmlSample restored = helper.DeserializeXml<XmlSample>(xml);
                            Assert(restored.Name == "Widget", "Name mismatch");
                            Assert(restored.Count == 7, "Count mismatch");
                            return Task.CompletedTask;
                        }),

                    new TestCaseDescriptor("Serialization", "XmlFromBytes", "XML can be deserialized from bytes",
                        executeAsync: _ =>
                        {
                            SerializationHelper helper = new SerializationHelper();
                            XmlSample original = new XmlSample { Name = "Gadget", Count = 3 };
                            string xml = helper.SerializeXml(original, false);
                            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(xml);
                            XmlSample restored = helper.DeserializeXml<XmlSample>(bytes);
                            Assert(restored.Name == "Gadget", "Name mismatch");
                            return Task.CompletedTask;
                        }),

                    new TestCaseDescriptor("Serialization", "XmlWithBom", "A UTF-8 BOM preamble is stripped before parsing",
                        executeAsync: _ =>
                        {
                            SerializationHelper helper = new SerializationHelper();
                            XmlSample original = new XmlSample { Name = "Bommed", Count = 1 };
                            string xml = helper.SerializeXml(original, false);
                            string bom = System.Text.Encoding.UTF8.GetString(System.Text.Encoding.UTF8.GetPreamble());
                            XmlSample restored = helper.DeserializeXml<XmlSample>(bom + xml);
                            Assert(restored.Name == "Bommed", "Name mismatch after BOM strip");
                            return Task.CompletedTask;
                        }),

                    new TestCaseDescriptor("Serialization", "DeserializeExceptionNotSupported", "Deserializing an Exception is rejected",
                        executeAsync: _ =>
                        {
                            SerializationHelper helper = new SerializationHelper();
                            try
                            {
                                helper.DeserializeJson<EmailValidationResult>("{\"Exception\":{\"Message\":\"x\"}}");
                                throw new Exception("Expected NotSupportedException");
                            }
                            catch (NotSupportedException) { }
                            return Task.CompletedTask;
                        }),

                    new TestCaseDescriptor("Serialization", "DeserializeNameValueCollectionNotImplemented", "Deserializing a NameValueCollection is rejected",
                        executeAsync: _ =>
                        {
                            SerializationHelper helper = new SerializationHelper();
                            try
                            {
                                helper.DeserializeJson<NvcSample>("{\"Values\":{\"k\":\"v\"}}");
                                throw new Exception("Expected NotImplementedException");
                            }
                            catch (NotImplementedException) { }
                            return Task.CompletedTask;
                        }),

                    new TestCaseDescriptor("Serialization", "DeserializeXmlNullBytesThrows", "Deserializing null bytes throws",
                        executeAsync: _ =>
                        {
                            SerializationHelper helper = new SerializationHelper();
                            try { helper.DeserializeXml<XmlSample>((byte[])null!); throw new Exception("Expected ArgumentNullException"); }
                            catch (ArgumentNullException) { }
                            return Task.CompletedTask;
                        }),

                    new TestCaseDescriptor("Serialization", "DeserializeXmlNullStringThrows", "Deserializing a null string throws",
                        executeAsync: _ =>
                        {
                            SerializationHelper helper = new SerializationHelper();
                            try { helper.DeserializeXml<XmlSample>((string)null!); throw new Exception("Expected ArgumentNullException"); }
                            catch (ArgumentNullException) { }
                            return Task.CompletedTask;
                        }),

                    new TestCaseDescriptor("Serialization", "SerializeXmlNullThrows", "Serializing a null object to XML throws",
                        executeAsync: _ =>
                        {
                            SerializationHelper helper = new SerializationHelper();
                            try { helper.SerializeXml(null!, false); throw new Exception("Expected ArgumentNullException"); }
                            catch (ArgumentNullException) { }
                            return Task.CompletedTask;
                        })
                });
        }

        #endregion

        #region Sample-Types

        /// <summary>Public POCO used to exercise XML serialization round-trips.</summary>
        public class XmlSample
        {
            /// <summary>Name.</summary>
            public string Name { get; set; } = string.Empty;

            /// <summary>Count.</summary>
            public int Count { get; set; } = 0;
        }

        /// <summary>Public POCO used to exercise the NameValueCollection JSON converter.</summary>
        public class NvcSample
        {
            /// <summary>Values.</summary>
            public NameValueCollection Values { get; set; } = new NameValueCollection();
        }

        #endregion
    }
}
