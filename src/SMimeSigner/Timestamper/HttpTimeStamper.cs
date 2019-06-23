// Copyright (c) 2019 Glenn Watson. All rights reserved.
// Glenn Watson licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Text;
using System.Threading.Tasks;

using SMimeSigner.Helpers;

namespace SMimeSigner.Timestamper
{
    /// <summary>
    /// A time stamper which checks against the HTTP client authority.
    /// </summary>
    internal class HttpTimeStamper : ITimeStamper
    {
        private readonly Func<HttpClient> _httpClientFunc;

        public HttpTimeStamper(Func<HttpClient> clientFunc = null)
        {
            HttpClient Backup() => new HttpClient();
            _httpClientFunc = clientFunc ?? Backup;
        }

        /// <summary>
        /// Gets a default instance to avoid extra memory allocs.
        /// </summary>
        public static HttpTimeStamper Default { get; } = new HttpTimeStamper();

        /// <inheritdoc />
        public async Task<Rfc3161TimestampToken> GetAndSetRfc3161Timestamp(SignedCms signedData, Uri timeStampAuthorityUri)
        {
            if (timeStampAuthorityUri == null)
            {
                throw new ArgumentNullException(nameof(timeStampAuthorityUri));
            }

            // This example figures out which signer is new by it being "the only signer"
            if (signedData.SignerInfos.Count > 1)
            {
                throw new ArgumentException("We must have only one signer", nameof(signedData));
            }

            SignerInfo newSignerInfo = signedData.SignerInfos[0];

            byte[] nonce = new byte[8];

            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(nonce);
            }

            var request = Rfc3161TimestampRequest.CreateFromSignerInfo(
                newSignerInfo,
                HashAlgorithmName.SHA384,
                requestSignerCertificates: true,
                nonce: nonce);

            var client = _httpClientFunc.Invoke();
            var content = new ReadOnlyMemoryContent(request.Encode());
            content.Headers.ContentType = new MediaTypeHeaderValue("application/timestamp-query");
            var httpResponse = await client.PostAsync(timeStampAuthorityUri, content).ConfigureAwait(false);
            if (!httpResponse.IsSuccessStatusCode)
            {
                throw new CryptographicException(
                    $"There was a error from the timestamp authority. It responded with {httpResponse.StatusCode} {(int)httpResponse.StatusCode}: {httpResponse.Content}");
            }

            if (httpResponse.Content.Headers.ContentType.MediaType != "application/timestamp-reply")
            {
                throw new CryptographicException("The reply from the time stamp server was in a invalid format.");
            }

            var data = await httpResponse.Content.ReadAsByteArrayAsync().ConfigureAwait(false);

            var timestampToken = request.ProcessResponse(data, out _);

            newSignerInfo.UnsignedAttributes.Add(new AsnEncodedData(CertificateHelper.SignatureTimeStampOin, timestampToken.AsSignedCms().Encode()));

            return timestampToken;
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool? CheckRFC3161Timestamp(SignerInfo signerInfo, DateTimeOffset? notBefore, DateTimeOffset? notAfter)
        {
            return CheckRFC3161TimestampInternal(signerInfo, notBefore, notAfter);
        }

        internal static bool? CheckRFC3161TimestampInternal(SignerInfo signerInfo, DateTimeOffset? notBefore, DateTimeOffset? notAfter)
        {
            bool found = false;
            byte[] signatureBytes = null;

            foreach (CryptographicAttributeObject attr in signerInfo.UnsignedAttributes)
            {
                if (attr.Oid.Value == CertificateHelper.SignatureTimeStampOin.Value)
                {
                    foreach (AsnEncodedData attrInst in attr.Values)
                    {
                        byte[] attrData = attrInst.RawData;

                        // New API starts here:
                        if (!Rfc3161TimestampToken.TryDecode(attrData, out var token, out var bytesRead))
                        {
                            return false;
                        }

                        if (bytesRead != attrData.Length)
                        {
                            return false;
                        }

                        signatureBytes = signatureBytes ?? signerInfo.GetSignature();

                        // Check that the token was issued based on the SignerInfo's signature value
                        if (!token.VerifySignatureForSignerInfo(signerInfo, out _))
                        {
                            return false;
                        }

                        var timestamp = token.TokenInfo.Timestamp;

                        // Check that the signed timestamp is within the provided policy range
                        // (which may be (signerInfo.Certificate.NotBefore, signerInfo.Certificate.NotAfter);
                        // or some other policy decision)
                        if (timestamp < notBefore.GetValueOrDefault(timestamp) ||
                            timestamp > notAfter.GetValueOrDefault(timestamp))
                        {
                            return false;
                        }

                        var tokenSignerCert = token.AsSignedCms().SignerInfos[0].Certificate;

                        // Implicit policy decision: Tokens required embedded certificates (since this method has
                        // no resolver)
                        if (tokenSignerCert == null)
                        {
                            return false;
                        }

                        found = true;
                    }
                }
            }

            // If we found any attributes and none of them returned an early false, then the SignerInfo is
            // conformant to policy.
            if (found)
            {
                return true;
            }

            // Inconclusive, as no signed timestamps were found
            return null;
        }
    }
}
