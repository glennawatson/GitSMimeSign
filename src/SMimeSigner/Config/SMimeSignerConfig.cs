// Copyright (c) 2019 Glenn Watson. All rights reserved.
// Glenn Watson licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using Salaros.Configuration;

namespace SMimeSigner.Config
{
    /// <summary>
    /// Represents a configuration file and it's contents.
    /// It contains common configuration options that aren't passed in by GIT.
    /// </summary>
    internal class SMimeSignerConfig
    {
        private const string FileName = ".smimesignerconfig";

        /// <summary>
        /// Initializes a new instance of the <see cref="SMimeSignerConfig"/> class.
        /// </summary>
        /// <param name="timeAuthorityUri">The URI to the RFC3161 time stamping authority URI.</param>
        public SMimeSignerConfig(Uri timeAuthorityUri)
        {
            TimeAuthorityUrl = timeAuthorityUri;
        }

        /// <summary>
        /// Gets the RFC3161 time stamping authority URI.
        /// </summary>
        public Uri TimeAuthorityUrl { get; }

        /// <summary>
        /// Loads the configuration from the configuration file in the user profile, if it exists.
        /// </summary>
        /// <returns>The configuration.</returns>
        public static SMimeSignerConfig LoadUserProfileConfig()
        {
            var userProfilePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var configFilePath = Path.Combine(userProfilePath, FileName);

            if (!File.Exists(configFilePath))
            {
                return null;
            }

            var iniFileParser = new ConfigParser(configFilePath);

            var timeAuthorityUriString = iniFileParser.GetValue("Certificate", "TimeAuthorityUrl");

            Uri authorityUri = null;
            if (!string.IsNullOrWhiteSpace(timeAuthorityUriString) && !Uri.TryCreate(timeAuthorityUriString, UriKind.Absolute, out authorityUri))
            {
                throw new Exception("The timestamp authority is not a valid URL inside configuration file: " + configFilePath);
            }

            return new SMimeSignerConfig(authorityUri);
        }
    }
}
