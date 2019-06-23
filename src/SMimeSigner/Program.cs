// Copyright (c) 2019 Glenn Watson. All rights reserved.
// Glenn Watson licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using CommandLine;
using CommandLine.Text;

using SMimeSigner.Actions;
using SMimeSigner.Helpers;

namespace SMimeSigner
{
    /// <summary>
    /// The main entry class to the application.
    /// </summary>
    internal static class Program
    {
        /// <summary>
        /// The main entry location for the application.
        /// </summary>
        /// <param name="args">Command line arguments that have been passed in.</param>
        public static async Task<int> Main(string[] args)
        {
            #if DEBUG
            // Debugger.Launch();
            #endif

            // Command Line Parser doesn't handle loner '-' well, so convert to empty brackets.
            for (int i = 0; i < args.Length; ++i)
            {
                var arg = args[i];
                if (arg == "-")
                {
                    args[i] = string.Empty;
                }
            }

            var parserResult = Parser.Default.ParseArguments<Options>(args);
            try
            {
                return await parserResult.MapResult(OnExecuteAsync, _ => Task.FromResult(1)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Console.WriteLine();
                Console.WriteLine(HelpText.AutoBuild(parserResult, null, null));
                return 1;
            }
        }

        private static async Task<int> OnExecuteAsync(Options options)
        {
            if (!options.ListKeys && !options.Sign && !options.VerifySignature)
            {
                throw new Exception("Must select a action. Pick from --list--keys, --sign or --verify.");
            }

            GpgOutputHelper.FileDescriptor = options.StatusFileDescriptor;

            try
            {
                if (options.ListKeys)
                {
                    return await ListKeysAction.Do().ConfigureAwait(false);
                }

                if (options.Sign)
                {
                    return await SignAction.Do(options.FileNames.FirstOrDefault(), options.LocalUser, options.GetTimestampAuthorityUri(), options.DetachedSign, options.Armor, options.IncludeOption).ConfigureAwait(false);
                }

                if (options.VerifySignature)
                {
                    return VerifyAction.Do(options.FileNames.ToArray());
                }
            }
            catch (Exception ex)
            {
                InfoOutputHelper.WriteLine(ex.ToString());
            }

            return 1;
        }
    }
}
