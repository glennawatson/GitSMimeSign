﻿// Copyright (c) 2019 Glenn Watson. All rights reserved.
// Glenn Watson licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Security.Cryptography.X509Certificates;

using CommandLine;

using GitSMimeSign.Config;
using GitSMimeSign.Properties;

namespace GitSMimeSign
{
    /// <summary>
    /// The command line options available for the application.
    /// Will be populated with chosen parameters from the user.
    /// </summary>
    [SuppressMessage("Design", "CA1812: Options is an internal class that is apparently never instantiated.", Justification = "Generated by CommandLineParser.")]
    internal class Options
    {
        /// <summary>
        /// Gets or sets the local user.
        /// </summary>
        [Option('u', "local-user", HelpText = "Use USER-ID to sign")]
        public string LocalUser { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to make a detached signing.
        /// </summary>
        [Option('b', "detached-sign", HelpText = "Make a detached signature")]
        public bool DetachedSign { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to create ascii armored output.
        /// </summary>
        [Option('a', "armor", HelpText = "Create ASCII armored output")]
        public bool Armor { get; set; }

        /// <summary>
        /// Gets or sets a optional file descriptor where to output special status strings.
        /// </summary>
        [Option("status-fd", HelpText = "Write special status strings to the specified file descriptor")]
        public string StatusFileDescriptor { get; set; }

        /// <summary>
        /// Gets or sets the RFC3161 timestamp authority for signing.
        /// </summary>
        [Option('t', "timestamp-authority", Default = "http://timestamp.digicert.com", HelpText = "A URL to the RFC 3161 time stamp authority.")]
        public string TimestampAuthority { get; set; } = "http://timestamp.digicert.com";

        /// <summary>
        /// Gets or sets if we should include certificates.
        /// </summary>
        [Option("include-certs", Default = -2, HelpText = "This option overrides the command line option --include-certs. A value of -2 includes all certificates except for the root certificate, -1 includes all certificates, 0 does not include any certificates, 1 includes only the signers certificate and all other positive values include up to value certificates starting with the signer cert. Default is -2.")]
        public int IncludeCertificates { get; set; } = -2;

        public X509IncludeOption IncludeOption
        {
            get
            {
                switch (IncludeCertificates)
                {
                    case -2:
                        return X509IncludeOption.ExcludeRoot;
                    case -1:
                        return X509IncludeOption.WholeChain;
                    case 0:
                        return X509IncludeOption.None;
                    case 1:
                        return X509IncludeOption.EndCertOnly;
                    default:
                        throw new Exception(Resources.InvalidCertificateMode);
                }
            }
        }

        /// <summary>
        /// Gets or sets the key id format.
        /// </summary>
        [Option("keyid-format", Default = "long", HelpText = "Select how to display key IDs. 'none' does not show the key ID at all but shows the fingerprint in a separate line. 'short' is the traditional 8-character key ID. 'long' is the more accurate (but less convenient) 16-character key ID. Add an '0x' to either to include an '0x' at the beginning of the key ID, as in 0x99242560.")]
        public string KeyIdFormat { get; set; } = "long";

        /// <summary>
        /// Gets or sets a value indicating whether we should list the keys.
        /// </summary>
        [Option("list-keys", HelpText = "List all the keys available on the system.")]
        public bool ListKeys { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether we should verify a object.
        /// </summary>
        [Option("verify", HelpText = "Verifies a signature.")]
        public bool VerifySignature { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether we should sign a object.
        /// </summary>
        [Option('s', "sign", HelpText = "Sign a object and make a signature.")]
        public bool Sign { get; set; }

        [Value(0, MetaName = "fileName", HelpText = "The file name.")]
        public IEnumerable<string> FileNames { get; set; }

        public Uri GetTimestampAuthorityUri()
        {
            Uri uri = null;

            if (!string.IsNullOrWhiteSpace(TimestampAuthority) && !Uri.TryCreate(TimestampAuthority, UriKind.Absolute, out uri))
            {
                throw new Exception(string.Format(CultureInfo.InvariantCulture, Resources.InvalidTimeAuthority, uri));
            }

            if (string.IsNullOrWhiteSpace(TimestampAuthority))
            {
                uri = SignConfig.LoadUserProfileConfig()?.TimeAuthorityUrl;
            }

            if (uri is null)
            {
                return null;
            }

            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            {
                throw new Exception(Resources.InvalidTimeAuthorityScheme + uri);
            }

            return uri;
        }
    }
}
