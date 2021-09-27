// Copyright (c) 2019 Glenn Watson. All rights reserved.
// Glenn Watson licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using GitSMimeSign.Helpers;
using GitSMimeSign.Properties;

namespace GitSMimeSign.Actions
{
    internal static class ListKeysAction
    {
        public static Task<int> Do()
        {
            using (var store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
            {
                try
                {
                    store.Open(OpenFlags.OpenExistingOnly | OpenFlags.ReadOnly);

                    var firstEntry = true;

                    for (var i = 0; i < store.Certificates.Count; ++i)
                    {
                        var certificate = store.Certificates[i];

                        if (!certificate.IsValidSigningCertificate())
                        {
                            continue;
                        }

                        if (!firstEntry)
                        {
                            Console.WriteLine();
                        }
                        else
                        {
                            firstEntry = false;
                        }

                        Console.WriteLine(Resources.IdHeader, certificate.Thumbprint);
                        Console.WriteLine(Resources.SerialNumberHeader, certificate.SerialNumber);
                        Console.WriteLine(Resources.SignatureAlgorithmHeader, certificate.SignatureAlgorithm.FriendlyName);
                        Console.WriteLine(Resources.ValidityHeader, certificate.NotBefore.ToString("o", CultureInfo.InvariantCulture), certificate.NotAfter.ToString("o", CultureInfo.InvariantCulture));
                        Console.WriteLine(Resources.IssuerHeader, certificate.Issuer);
                        Console.WriteLine(Resources.SubjectHeader, certificate.Subject);
                        Console.WriteLine(Resources.EmailHeader, certificate.GetNameInfo(X509NameType.EmailName, false));
                    }

                    return Task.FromResult(0);
                }
                catch (CryptographicException)
                {
                    return Task.FromException<int>(new SignClientException(Resources.X509StoreIssue));
                }
            }
        }
    }
}
