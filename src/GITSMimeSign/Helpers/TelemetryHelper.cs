// Copyright (c) 2019 Glenn Watson. All rights reserved.
// Glenn Watson licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;

using GitSMimeSign.Config;

using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Extensibility.Implementation;

namespace GitSMimeSign.Helpers
{
    /// <summary>
    /// Helpers with telemetry based information.
    /// </summary>
    internal static class TelemetryHelper
    {
        static TelemetryHelper()
        {
            if (SignConfig.LoadUserProfileConfig()?.DisableTelemetry ?? false)
            {
                return;
            }

            TelemetryConfiguration.Active.InstrumentationKey = "a2d03416-ac19-4444-b75e-b9e2eaf2d2f1";
            TelemetryConfiguration.Active.TelemetryChannel = new InMemoryChannel();

            Client = new TelemetryClient();

            // Set session data:
            Client.Context.User.Id = Environment.UserName;
            Client.Context.Session.Id = Guid.NewGuid().ToString();
            Client.Context.Device.OperatingSystem = Environment.OSVersion.ToString();

            Client.Context.Component.Version = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductVersion;
        }

        /// <summary>
        /// Gets the client.
        /// </summary>
        public static TelemetryClient Client { get; }

        public static void DeInit()
        {
            Client?.Flush();
        }
    }
}
