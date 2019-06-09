// Copyright (c) 2019 Glenn Watson. All rights reserved.
// Glenn Watson licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.IO;

namespace SMimeSigner.Helpers
{
    /// <summary>
    /// This will output more informational pieces which typically get shown to the end user.
    /// </summary>
    internal static class InfoOutputHelper
    {
        /// <summary>
        /// Use the GNUPG prefix since git is expecting this for output.
        /// </summary>
        private const string Prefix = "[smimesigner:] ";

        /// <summary>
        /// Gets or sets the text writer where to send the output.
        /// </summary>
        internal static TextWriter TextWriter { get; set; } = Console.Error;

        /// <summary>
        /// Writes the line to the users output. Adding the prefix.
        /// </summary>
        /// <param name="output">The string to format.</param>
        /// <param name="args">The arguments if any for the formatting.</param>
        public static void WriteLine(string output, params object[] args)
        {
            // Use unix style output since this seems to be what GIT likes.
            TextWriter?.Write(Prefix + output + '\n', args);
        }

        /// <summary>
        /// Outputs a line to the output buffer.
        /// </summary>
        internal static void WriteLine()
        {
            TextWriter?.Write('\n');
        }
    }
}
