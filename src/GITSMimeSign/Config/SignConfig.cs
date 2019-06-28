// Copyright (c) 2019 Glenn Watson. All rights reserved.
// Glenn Watson licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;

using GitSMimeSign.Properties;

using Salaros.Configuration;

namespace GitSMimeSign.Config
{
    /// <summary>
    /// Represents a configuration file and it's contents.
    /// It contains common configuration options that aren't passed in by GIT.
    /// </summary>
    internal class SignConfig
    {
        private const string FileName = ".gitsmimesignconfig";

        private static readonly Lazy<SignConfig> SignConfigDefault = new Lazy<SignConfig>(LoadUserProfileConfigInternal, LazyThreadSafetyMode.None);

        /// <summary>
        /// Initializes a new instance of the <see cref="SignConfig"/> class.
        /// </summary>
        /// <param name="timeAuthorityUri">The URI to the RFC3161 time stamping authority URI.</param>
        /// <param name="disableTelemetry">A value indicating if we should disable telemetry.</param>
        public SignConfig(Uri timeAuthorityUri, bool disableTelemetry)
        {
            TimeAuthorityUrl = timeAuthorityUri;
            DisableTelemetry = disableTelemetry;
        }

        /// <summary>
        /// Gets the RFC3161 time stamping authority URI.
        /// </summary>
        public Uri TimeAuthorityUrl { get; }

        /// <summary>
        /// Gets a value indicating whether we should disable telemetry.
        /// </summary>
        public bool DisableTelemetry { get; }

        /// <summary>
        /// Loads the configuration from the configuration file in the user profile, if it exists.
        /// </summary>
        /// <returns>The configuration.</returns>
        public static SignConfig LoadUserProfileConfig()
        {
            return SignConfigDefault.Value;
        }

        [SuppressMessage("Design", "CA1031: Do not catch generic exceptions", Justification = "Catch all deliberate.")]
        private static SignConfig LoadUserProfileConfigInternal()
        {
            var userProfilePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var configFilePath = Path.Combine(userProfilePath, FileName);

            if (!File.Exists(configFilePath))
            {
                return null;
            }

            try
            {
                var iniFileParser = new ConfigParser(configFilePath);

                var timeAuthorityUriString = iniFileParser.GetValue("Certificate", "TimeAuthorityUrl");

                Uri authorityUri = null;

                if (!string.IsNullOrWhiteSpace(timeAuthorityUriString) && !Uri.TryCreate(timeAuthorityUriString, UriKind.Absolute, out authorityUri))
                {
                    throw new SignClientException(Resources.InvalidTimestampUriConfig + configFilePath);
                }

                var disableTelemetryString = iniFileParser.GetValue("Telemetry", "Disable");

                bool disableTelemetry = false;
                if (!string.IsNullOrWhiteSpace(disableTelemetryString) && !bool.TryParse(disableTelemetryString, out disableTelemetry))
                {
                    throw new SignClientException(Resources.InvalidTelemtryValue);
                }

                return new SignConfig(authorityUri, disableTelemetry);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
