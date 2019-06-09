// Copyright (c) 2019 Glenn Watson. All rights reserved.
// Glenn Watson licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.IO;

namespace SMimeSigner.Helpers
{
    /// <summary>
    /// Helpers which assist with common file stream operations.
    /// </summary>
    internal static class FileSystemStreamHelper
    {
        /// <summary>
        /// Reads all the contents from the specified file name stream.
        /// It can be a file name or can be null just to read from stdin.
        /// </summary>
        /// <param name="fileName">The file name or descriptor id.</param>
        /// <returns>The bytes contents from the specified handle.</returns>
        public static byte[] ReadFileStreamFully(string fileName)
        {
            using (var stream = OpenFileOrDescriptor(fileName))
            {
                return ReadFully(stream);
            }
        }

        private static Stream OpenFileOrDescriptor(string fileName)
        {
            Stream stream;
            if (!string.IsNullOrWhiteSpace(fileName) && fileName == "-")
            {
                stream = new FileStream(fileName, FileMode.Open, FileAccess.Read);
            }
            else if (!string.IsNullOrWhiteSpace(fileName))
            {
                stream = new FileStream(fileName, FileMode.Open, FileAccess.Read);
            }
            else
            {
                if (!Console.IsInputRedirected)
                {
                    throw new Exception("StdIn has not been redirected.");
                }

                stream = Console.OpenStandardInput();
            }

            return stream;
        }

        private static byte[] ReadFully(Stream stream)
        {
            var ms = new MemoryStream();
            stream.CopyTo(ms);
            return ms.ToArray();
        }
    }
}
