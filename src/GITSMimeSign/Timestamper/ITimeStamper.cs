// Copyright (c) 2019 Glenn Watson. All rights reserved.
// Glenn Watson licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Security.Cryptography.Pkcs;
using System.Threading.Tasks;

namespace GitSMimeSign.Timestamper
{
    /// <summary>
    /// Performs RFC3161 time stamping.
    /// </summary>
    internal interface ITimeStamper
    {
        /// <summary>
        /// Gets a timestamp.
        /// </summary>
        /// <param name="signed">A CMS to time stamp.</param>
        /// <param name="timeStampAuthorityUri">The URI to the timestamp authority.</param>
        /// <returns>The RFC3161 timestamp token.</returns>
        Task<Rfc3161TimestampToken> GetAndSetRfc3161Timestamp(SignedCms signed, Uri timeStampAuthorityUri);

        /// <summary>
        /// Checks the signer for any RFC3161 signing information and makes sure it's valid.
        /// </summary>
        /// <param name="signerInfo">The signer information to check.</param>
        /// <param name="notBefore">The date time the signing information must be at or after.</param>
        /// <param name="notAfter">The date time the signing information must be at or before.</param>
        /// <returns>If the RFC3161 is valid.</returns>
        bool? CheckRFC3161Timestamp(SignerInfo signerInfo, DateTimeOffset? notBefore, DateTimeOffset? notAfter);
    }
}
