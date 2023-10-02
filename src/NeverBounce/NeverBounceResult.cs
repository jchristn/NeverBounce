using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace NeverBounce
{
    /// <summary>
    /// Result from NeverBounce.
    /// </summary>
    public class NeverBounceResult
    {
        #region Public-Members

        /// <summary>
        /// Status.
        /// </summary>
        [JsonPropertyName("status")]
        public string Status { get; set; } = null;

        /// <summary>
        /// Result.
        /// </summary>
        [JsonPropertyName("result")]
        public string Result { get; set; } = null;

        /// <summary>
        /// Flags.
        /// </summary>
        [JsonPropertyName("flags")]
        public List<string> Flags
        {
            get
            {
                return _Flags;
            }
            set
            {
                if (value == null) _Flags = new List<string>();
                else _Flags = value;
            }
        }

        #endregion

        #region Private-Members

        private List<string> _Flags { get; set; } = new List<string>();

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public NeverBounceResult()
        {

        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
