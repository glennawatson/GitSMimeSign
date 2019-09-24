// Copyright (c) 2019 Glenn Watson. All rights reserved.
// Glenn Watson licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

using GitSMimeSign.Actions;
using GitSMimeSign.Helpers;

using Shouldly;

using Xunit;

namespace GitSMimeSigner.Tests
{
    /// <summary>
    /// Tests associated with the Sign procedures.
    /// </summary>
    public class SignVerifyTests
    {
        /// <summary>
        /// Perform signing/verification with detached signatures and PEM encoding.
        /// This is the common scenario for GIT.
        /// </summary>
        /// <returns>A task to monitor.</returns>
        [Fact]
        public async Task TestSignAndVerifyPemDetached()
        {
            var (outputStream, gpgStream) = GetOutputStreams();
            var certificate = Generate();
            var bytes = Encoding.UTF8.GetBytes("Hello World");
            var result = await SignAction.PerformSign(certificate, bytes, null, true, true, X509IncludeOption.WholeChain).ConfigureAwait(false);

            Assert.Equal(0, result);

            var output = GetStreamContents(outputStream).Replace("\r\n", string.Empty).Replace("\n", string.Empty).Replace("\r", string.Empty);
            var gpg = GetStreamContents(gpgStream);
            output.ShouldBe("[GitSMimeSign:] Finished signing");
            gpg.ShouldNotBeEmpty();
        }

        private static string GetStreamContents(Stream stream)
        {
            stream.Flush();
            stream.Position = 0;
            using (var sr = new StreamReader(stream))
            {
                return sr.ReadToEnd();
            }
        }

        private static (MemoryStream output, MemoryStream gpg) GetOutputStreams()
        {
            var gpgMemoryStream = new MemoryStream();
            var outputMemoryStream = new MemoryStream();

            GpgOutputHelper.OutputStream = new Lazy<Stream>(gpgMemoryStream);
            GpgOutputHelper.TextWriter = new StreamWriter(gpgMemoryStream);
            InfoOutputHelper.TextWriter = new StreamWriter(outputMemoryStream);

            return (outputMemoryStream, gpgMemoryStream);
        }

        private static X509Certificate2 Generate()
        {
            // Taken from https://github.com/dotnet/corefx/blob/5012dfe0813bf9f3eaf7a6460671e07ea048fd52/src/System.Security.Cryptography.X509Certificates/tests/CertificateCreation/CertificateRequestUsageTests.cs#L190
            using (RSA rsa = RSA.Create())
            {
                CertificateRequest request = new CertificateRequest(
                    "CN=localhost, OU=.NET Framework (CoreFX), O=Microsoft Corporation, L=Redmond, S=Washington, C=US",
                    rsa,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);

                var caFakeIssuer = GenerateCA("CN=Fake Name CA, OU=Fake, O=Fake Org, L=FakeVille, S=Fake State, C=US");

                byte[] serialNumber = new byte[8];
                RandomNumberGenerator.Fill(serialNumber);
                DateTimeOffset now = DateTimeOffset.UtcNow;
                var cert = request.Create(caFakeIssuer, now, now.AddDays(90), serialNumber);

                var temp = cert.CopyWithPrivateKey(rsa);

                // Work around described in https://github.com/dotnet/corefx/issues/35120
                return new X509Certificate2(temp.Export(X509ContentType.Pfx));
            }
        }

        private static X509Certificate2 GenerateCA(string subject)
        {
            using (RSA rsa = RSA.Create())
            {
                CertificateRequest request = new CertificateRequest(
                    subject,
                    rsa,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);

                request.CertificateExtensions.Add(
                    new X509BasicConstraintsExtension(true, false, 0, true));

                DateTimeOffset now = DateTimeOffset.UtcNow;
                return request.CreateSelfSigned(now, now.AddDays(90));
            }
        }
    }
}
