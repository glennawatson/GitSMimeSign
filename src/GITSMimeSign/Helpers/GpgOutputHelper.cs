// Copyright (c) 2019 Glenn Watson. All rights reserved.
// Glenn Watson licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Threading;

namespace GitSMimeSign.Helpers
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

        /// <summary>
        /// Gets or sets the file descriptor. This will be unix style file descriptors, or a file for debugging.
        /// It can be NULL for no output.
        /// </summary>
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
                        TextWriter = Console.Out;
                        OutputStream = new Lazy<Stream>(Console.OpenStandardOutput, LazyThreadSafetyMode.PublicationOnly);
                        break;
                    case "2":
                        TextWriter = Console.Error;
                        OutputStream = new Lazy<Stream>(Console.OpenStandardError, LazyThreadSafetyMode.PublicationOnly);
                        break;
                    default:
                        TextWriter = new StreamWriter(value);
                        OutputStream = new Lazy<Stream>(() => new FileStream(value, FileMode.OpenOrCreate, FileAccess.Write), true);
                        break;
                }
            }
        }

        /// <summary>
        /// Gets or sets the text writer.
        /// </summary>
        internal static TextWriter TextWriter { get; set; }

        /// <summary>
        /// Gets or sets a stream to the output.
        /// </summary>
        internal static Lazy<Stream> OutputStream { get; set; }

        /// <summary>
        /// Writes the line to the gpg output. Adding the prefix.
        /// </summary>
        /// <param name="output">The string to format.</param>
        /// <param name="args">The arguments if any for the formatting.</param>
        public static void WriteLine(string output, params object[] args)
        {
            TextWriter?.WriteLine(Prefix + output, args);
        }

        /// <summary>
        /// Writes the line to the gpg output without the prefix.
        /// </summary>
        /// <param name="output">The string to format.</param>
        /// <param name="args">The arguments if any for the formatting.</param>
        public static void NoPrefixWriteLine(string output, params object[] args)
        {
            TextWriter?.WriteLine(output, args);
        }

        /// <summary>
        /// Flushes the output.
        /// </summary>
        public static void Flush()
        {
            TextWriter?.Flush();
        }
    }
}
