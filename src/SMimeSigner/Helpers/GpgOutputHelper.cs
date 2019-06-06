// Copyright (c) 2019 Glenn Watson. All rights reserved.
// Glenn Watson licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.IO;

namespace SMimeSigner.Helpers
{
    /// <summary>
    /// Handles the output.
    /// </summary>
    internal static class GpgOutputHelper
    {
        /// <summary>
        /// Use the GNUPG prefix since git is expecting this for output.
        /// </summary>
        private const string Prefix = "[GNUPG:] ";

        private static string _fileDescriptor;

        private static TextWriter _textWriter;

        public static string FileDescriptor
        {
            get => _fileDescriptor;
            set
            {
                _fileDescriptor = value;

                if (string.IsNullOrWhiteSpace(value))
                {
                    return;
                }

                switch (value)
                {
                    case "1":
                        _textWriter = Console.Out;
                        break;
                    case "2":
                        _textWriter = Console.Error;
                        break;
                    default:
                        _textWriter = new StreamWriter(value);
                        break;
                }
            }
        }

        public static void WriteLine(string output, params object[] args)
        {
            _textWriter?.WriteLine(Prefix + output, args);
        }
    }
}
