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
    internal static class KeyOutputHelper
    {
        /// <summary>
        /// Writes the line to the users output. Adding the prefix.
        /// </summary>
        /// <param name="data">The data to format.</param>
        /// <param name="usePemEncoding">The value to output.</param>
        public static void Write(byte[] data, bool usePemEncoding)
        {
            if (usePemEncoding)
            {
                Console.WriteLine(PemHelper.EncodeString("SIGNED MESSAGE", data));
            }
            else
            {
                using (Stream myOutStream = Console.OpenStandardOutput())
                {
                    myOutStream.Write(data, 0, data.Length);
                }
            }

            Console.Out.Flush();
        }
    }
}
