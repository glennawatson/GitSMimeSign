// Copyright (c) 2019 Glenn Watson. All rights reserved.
// Glenn Watson licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

using CommandLine;
using CommandLine.Text;

using GitSMimeSign.Actions;
using GitSMimeSign.Helpers;
using GitSMimeSign.Properties;

using Microsoft.ApplicationInsights.DataContracts;

namespace GitSMimeSign
{
    /// <summary>
    /// The main entry class to the application.
    /// </summary>
    [SuppressMessage("Design", "CA1031: Do not catch generic exceptions", Justification = "Catch all deliberate.")]
    internal static class Program
    {
        /// <summary>
        /// The main entry location for the application.
        /// </summary>
        /// <param name="args">Command line arguments that have been passed in.</param>
        public static async Task<int> Main(string[] args)
        {
            // Command Line Parser doesn't handle loner '-' well, so convert to empty brackets.
            for (int i = 0; i < args.Length; ++i)
            {
                var arg = args[i];
                if (arg == "-")
                {
                    args[i] = string.Empty;
                }
            }

            TelemetryHelper.Client?.TrackTrace(Resources.InitializingApplication);

            var parserResult = Parser.Default.ParseArguments<Options>(args);
            try
            {
                return await parserResult.MapResult(OnExecuteAsync, _ => Task.FromResult(1)).ConfigureAwait(false);
            }
            catch (SignClientException ex)
            {
                // Only send through SignClientException since these contain no personal information.
                TelemetryHelper.Client?.TrackException(ex);
                Console.WriteLine(ex.ToString());
                Console.WriteLine();
                Console.WriteLine(HelpText.AutoBuild(parserResult, null, null));
                return 1;
            }
            catch (Exception ex)
            {
                // Due to the fact we want to be careful with personal information just track the fact that an Exception occurred.
                TelemetryHelper.Client?.TrackTrace(ex.GetType().FullName, SeverityLevel.Critical);
                Console.WriteLine(ex.ToString());
                Console.WriteLine();
                Console.WriteLine(HelpText.AutoBuild(parserResult, null, null));
                return 1;
            }
            finally
            {
                TelemetryHelper.DeInit();
            }
        }

        private static async Task<int> OnExecuteAsync(Options options)
        {
            if (!options.ListKeys && !options.Sign && !options.VerifySignature)
            {
                throw new Exception(Resources.ValidCommandAction);
            }

            GpgOutputHelper.FileDescriptor = options.StatusFileDescriptor;

            try
            {
                int result = 1;
                if (options.ListKeys)
                {
                    result = await ListKeysAction.Do().ConfigureAwait(false);
                }

                if (options.Sign)
                {
                    result = await SignAction.Do(options.FileNames.FirstOrDefault(), options.LocalUser, options.GetTimestampAuthorityUri(), options.DetachedSign, options.Armor, options.IncludeOption).ConfigureAwait(false);
                }

                if (options.VerifySignature)
                {
                    result = VerifyAction.Do(options.FileNames.ToArray());
                }

                if (result == 1)
                {
                    throw new Exception(Resources.ValidCommandAction);
                }

                return result;
            }
            catch (SignClientException ex)
            {
                InfoOutputHelper.WriteLine(ex.ToString());
                TelemetryHelper.Client?.TrackException(ex);
            }
            catch (Exception ex)
            {
                InfoOutputHelper.WriteLine(ex.ToString());

                // Don't pass the exception in the generic case, due to it might contain personal information.
                TelemetryHelper.Client?.TrackTrace(ex.GetType().FullName, SeverityLevel.Critical);
            }

            return 1;
        }
    }
}
