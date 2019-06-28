// Copyright (c) 2019 Glenn Watson. All rights reserved.
// Glenn Watson licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace GitSMimeSigner.Actions
{
    internal static class ListKeysAction
    {
        public static Task<int> Do()
        {
            using (X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
            {
                try
                {
                    store.Open(OpenFlags.OpenExistingOnly | OpenFlags.ReadOnly);

                    for (int i = 0; i < store.Certificates.Count; ++i)
                    {
                        var certificate = store.Certificates[i];

                        if (i != 0)
                        {
                            Console.WriteLine();
                        }

                        Console.WriteLine("ID: {0}", certificate.Thumbprint);
                        Console.WriteLine("S/N: {0}", certificate.SerialNumber);
                        Console.WriteLine("Signature Algorithm: {0}", certificate.SignatureAlgorithm.FriendlyName);
                        Console.WriteLine($"Validity: {certificate.NotBefore.ToString("o", CultureInfo.InvariantCulture)} - {certificate.NotAfter.ToString("o", CultureInfo.InvariantCulture)}");
                        Console.WriteLine("Issuer: {0}", certificate.Issuer);
                        Console.WriteLine("Subject: {0}", certificate.Subject);
                        Console.WriteLine("Emails: {0}", certificate.GetNameInfo(X509NameType.EmailName, false));
                    }

                    return Task.FromResult(0);
                }
                catch (CryptographicException)
                {
                    Console.WriteLine("Could not open key store.");
                    return Task.FromResult(1);
                }
            }
        }
    }
}
