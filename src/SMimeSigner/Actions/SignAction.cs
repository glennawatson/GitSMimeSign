// Copyright (c) 2019 Glenn Watson. All rights reserved.
// Glenn Watson licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

using SMimeSigner.Helpers;

namespace SMimeSigner.Actions
{
    internal static class SignAction
    {
        // BEGIN_SIGNING
        //   Mark the start of the actual signing process. This may be used as an
        //   indication that all requested secret keys are ready for use.
        private const string BeginSigning = "BEGIN_SIGNING";

        // SIG_CREATED <type> <pk_algo> <hash_algo> <class> <timestamp> <keyfpr>
        //   A signature has been created using these parameters.
        //   Values for type <type> are:
        //     - D :: detached
        //     - C :: cleartext
        //     - S :: standard
        //   (only the first character should be checked)
        //
        //   <class> are 2 hex digits with the OpenPGP signature class.
        //
        //   Note, that TIMESTAMP may either be a number of seconds since Epoch
        //   or an ISO 8601 string which can be detected by the presence of the
        //   letter 'T'.
        private const string SignatureCreated = "SIG_CREATED";

        public static async Task<int> Do(string fileName, string localUser, Uri timeStampAuthority, bool isDetached, bool useArmor, X509IncludeOption includeOption)
        {
            if (localUser == null)
            {
                throw new ArgumentNullException(nameof(localUser), "You must specify the ID for signing. Either a email address or the certificate ID.");
            }

            var certificate = CertificateHelper.FindUserCertificate(localUser);

            if (certificate == null)
            {
                throw new Exception("Failed to get identity certificate with identity: " + localUser);
            }

            if (!certificate.HasPrivateKey)
            {
                throw new Exception($"The certificate {certificate.Thumbprint} has a invalid signing key.");
            }

            // Git is looking for "\n[GNUPG:] SIG_CREATED ", meaning we need to print a
            // line before SIG_CREATED. BEGIN_SIGNING seems appropriate. GPG emits this,
            // though GPGSM does not.
            GpgOutputHelper.WriteLine(BeginSigning);

            var bytes = FileSystemStreamHelper.ReadFileStreamFully(fileName);

            var contentInfo = new ContentInfo(bytes);
            var cms = new SignedCms(contentInfo, isDetached);
            var signer = new CmsSigner(certificate) { IncludeOption = includeOption };

            if (timeStampAuthority != null)
            {
                var timestampToken = await GetTimestamp(cms, signer, timeStampAuthority).ConfigureAwait(false);

                signer.UnsignedAttributes.Add(new AsnEncodedData(CertificateHelper.SignatureTimeStampOin, timestampToken.AsSignedCms().Encode()));
            }
            else
            {
                signer.SignedAttributes.Add(new Pkcs9SigningTime());
            }

            cms.ComputeSignature(signer);

            WriteSignature(certificate, isDetached);

            var encoding = cms.Encode();
            if (useArmor)
            {
                Console.WriteLine(PemHelper.EncodeString("SIGNED MESSAGE", encoding));
            }
            else
            {
                using (Stream myOutStream = Console.OpenStandardOutput())
                {
                    myOutStream.Write(encoding, 0, encoding.Length);
                }
            }

            return 0;
        }

        private static async Task<Rfc3161TimestampToken> GetTimestamp(SignedCms toSign, CmsSigner newSigner, Uri timeStampAuthorityUri)
        {
            if (timeStampAuthorityUri == null)
            {
                throw new ArgumentNullException(nameof(timeStampAuthorityUri));
            }

            // This example figures out which signer is new by it being "the only signer"
            if (toSign.SignerInfos.Count > 0)
            {
                throw new ArgumentException("We must have only one signer", nameof(toSign));
            }

            toSign.ComputeSignature(newSigner);

            SignerInfo newSignerInfo = toSign.SignerInfos[0];

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

            var client = new HttpClient();
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

            return request.ProcessResponse(data, out _);
        }

        private static void WriteSignature(X509Certificate2 certificate, bool isDetached)
        {
            const int signatureCode = 0; // GPGSM uses 0 as well.
            var signatureType = isDetached ? "D" : "S";
            var (algorithmCode, hashCode) = CertificateHelper.ToPgpPublicKeyAlgorithmCode(certificate);
            GpgOutputHelper.WriteLine($"{SignatureCreated} {signatureType} {algorithmCode} {hashCode} {signatureCode} {DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)} {certificate.Thumbprint}");
        }
    }
}
