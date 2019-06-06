// Copyright (c) 2019 Glenn Watson. All rights reserved.
// Glenn Watson licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SMimeSigner.Helpers
{
    internal static class InfoOutputHelper
    {
        /// <summary>
        /// Use the GNUPG prefix since git is expecting this for output.
        /// </summary>
        private const string Prefix = "[smimesigner:] ";

        private static readonly TextWriter _textWriter = Console.Error;

        public static void WriteLine(string output, params object[] args)
        {
            // Use unix style output since this seems to be what GIT likes.
            _textWriter?.Write(Prefix + output + '\n', args);
        }
    }
}
