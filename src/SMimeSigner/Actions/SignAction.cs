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
using SMimeSigner.Timestamper;

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

        /// <summary>
        /// Gets or sets the time stamper which will perform time stamping operations.
        /// </summary>
        internal static ITimeStamper TimeStamper { get; set; } = HttpTimeStamper.Default;

        /// <summary>
        /// Performs the sign action.
        /// </summary>
        /// <param name="fileName">The file name to sign. If null will be stdin.</param>
        /// <param name="localUser">The email address or certificate id of the certificate to sign against.</param>
        /// <param name="timeStampAuthority">A uri to the timestamp authority.</param>
        /// <param name="isDetached">If the signing operation should be detached and produce separate signature.</param>
        /// <param name="useArmor">If we should produce data in the PEM encoded format.</param>
        /// <param name="includeOption">The certificate include options, if we should just include end certificates, or intermediate/root certificates as well.</param>
        /// <returns>0 if the operation was successful, 1 otherwise.</returns>
        public static Task<int> Do(string fileName, string localUser, Uri timeStampAuthority, bool isDetached, bool useArmor, X509IncludeOption includeOption)
        {
            if (localUser == null)
            {
                throw new ArgumentNullException(nameof(localUser), "You must specify the ID for signing. Either a email address or the certificate ID.");
            }

            var certificate = CertificateHelper.FindUserCertificate(localUser);

            if (certificate == null)
            {
                throw new ArgumentException("Failed to get identity certificate with identity: " + localUser, nameof(localUser));
            }

            var bytes = FileSystemStreamHelper.ReadFileStreamFully(fileName);

            return PerformSign(certificate, bytes, timeStampAuthority, isDetached, useArmor, includeOption);
        }

        /// <summary>
        /// Performs a signing operation.
        /// </summary>
        /// <param name="certificate">The certificate to use for signing.</param>
        /// <param name="bytes">The bytes to sign.</param>
        /// <param name="timeStampAuthority">A optional RFC3161 timestamp authority to sign against.</param>
        /// <param name="isDetached">If we should be producing detached results.</param>
        /// <param name="useArmor">If we should encode in PEM format.</param>
        /// <param name="includeOption">The certificate include options, if we should just include end certificates, or intermediate/root certificates as well.</param>
        /// <returns>0 if the operation was successful, 1 otherwise.</returns>
        internal static async Task<int> PerformSign(X509Certificate2 certificate, byte[] bytes, Uri timeStampAuthority, bool isDetached, bool useArmor, X509IncludeOption includeOption)
        {
            if (certificate == null)
            {
                throw new ArgumentNullException(nameof(certificate), "Must have a valid certificate");
            }

            if (!certificate.HasPrivateKey)
            {
                throw new ArgumentException($"The certificate {certificate.Thumbprint} has a invalid signing key.", nameof(certificate));
            }

            if (bytes == null)
            {
                throw new ArgumentNullException(nameof(bytes), "Must have valid data to encrypt.");
            }

            // Git is looking for "\n[GNUPG:] SIG_CREATED ", meaning we need to print a
            // line before SIG_CREATED. BEGIN_SIGNING seems appropriate. GPG emits this,
            // though GPGSM does not.
            GpgOutputHelper.WriteLine(BeginSigning);

            var contentInfo = new ContentInfo(bytes);
            var cms = new SignedCms(contentInfo, isDetached);
            var signer = new CmsSigner(certificate) { IncludeOption = includeOption };
            signer.SignedAttributes.Add(new Pkcs9SigningTime());

            cms.ComputeSignature(signer, false);

            // If we are provided with a authority, add the timestamp certificate into our unsigned attributes.
            if (timeStampAuthority != null)
            {
                InfoOutputHelper.WriteLine("Stamping with RFC 3161 Time Stamp Authority: " + timeStampAuthority);
                await TimeStamper.GetAndSetRfc3161Timestamp(cms, timeStampAuthority).ConfigureAwait(false);
            }

            var encoding = cms.Encode();

            // Write out the signature in GPG expected format.
            WriteGpgFormatSignature(certificate, isDetached);

            KeyOutputHelper.Write(encoding, useArmor);

            InfoOutputHelper.WriteLine("Finished signing");

            GpgOutputHelper.Flush();
            InfoOutputHelper.Flush();

            return 0;
        }

        private static void WriteGpgFormatSignature(X509Certificate2 certificate, bool isDetached)
        {
            const int signatureCode = 0; // GPGSM uses 0 as well.
            var signatureType = isDetached ? "D" : "S";
            var (algorithmCode, hashCode) = CertificateHelper.ToPgpPublicKeyAlgorithmCode(certificate);
            GpgOutputHelper.WriteLine($"{SignatureCreated} {signatureType} {algorithmCode} {hashCode} {signatureCode} {DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)} {certificate.Thumbprint}");
        }
    }
}
