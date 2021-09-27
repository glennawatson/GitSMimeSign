﻿// Copyright (c) 2019 Glenn Watson. All rights reserved.
// Glenn Watson licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

using GitSMimeSign.Properties;

namespace GitSMimeSign.Helpers
{
    /// <summary>
    /// Handles dealing with certificates to and from the PEM format.
    /// </summary>
    internal static class PemHelper
    {
        /// <summary>
        /// Gets the pem encoded string.
        /// </summary>
        /// <param name="elementType">The type of value to store in the PEM.</param>
        /// <param name="bytes">The bytes to encode.</param>
        /// <returns>A PEM encoded value.</returns>
        public static string EncodeString(string elementType, byte[] bytes)
        {
            var builder = new StringBuilder();

            builder.Append("-----BEGIN ").Append(elementType).AppendLine("-----");

            var base64 = Convert.ToBase64String(bytes);

            var offset = 0;
            const int LineLength = 64;

            while (offset < base64.Length)
            {
                var lineEnd = Math.Min(offset + LineLength, base64.Length);
                builder.Append(base64, offset, lineEnd - offset).AppendLine();
                offset = lineEnd;
            }

            builder.Append("-----END ").Append(elementType).AppendLine("-----");
            return builder.ToString();
        }

        /// <summary>
        /// Attempts to decode files in the PEM format.
        /// </summary>
        /// <param name="encodedBytes">The encoded bytes.</param>
        /// <param name="outputBody">The output body to set. If the encoding is invalid we will set the to the encodedBytes parameter.</param>
        /// <returns>If the conversion was successful or not.</returns>
        public static bool TryDecode(byte[] encodedBytes, out byte[] outputBody)
        {
            try
            {
                var encodedString = Encoding.UTF8.GetString(encodedBytes);
                outputBody = DecodeString(encodedString);
                return true;
            }
            catch (InvalidOperationException)
            {
                outputBody = encodedBytes;
                return false;
            }
        }

        private static byte[] DecodeString(string encodedString)
        {
            var (header, body, footer) = ExtractPemParts(encodedString);

            var headerFormat = ExtractFormat(header, isFooter: false);
            var footerFormat = ExtractFormat(footer, isFooter: true);

            if (!headerFormat.Equals(footerFormat, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new SignClientException(string.Format(CultureInfo.InvariantCulture, Resources.InvalidPemHeaderFooter, headerFormat, footerFormat));
            }

            return body;
        }

        private static (string header, byte[] body, string footer) ExtractPemParts(string pem)
        {
            var match = Regex.Match(pem, @"^(?<header>\-+\s?BEGIN[^-]+\-+)\s*(?<body>[^-]+)\s*(?<footer>\-+\s?END[^-]+\-+)\s*$");
            if (!match.Success)
            {
                throw new InvalidOperationException(Resources.InvalidPemEncoding);
            }

            var header = match.Groups["header"].Value;
            var bodyText = match.Groups["body"].Value.RemoveWhitespace();
            var footer = match.Groups["footer"].Value;

            var body = Convert.FromBase64String(bodyText);

            return (header, body, footer);
        }

        private static string RemoveWhitespace(this string input)
        {
            return Regex.Replace(input, @"\s+", string.Empty);
        }

        private static string ExtractFormat(string headerOrFooter, bool isFooter)
        {
            var beginOrEnd = isFooter ? "END" : "BEGIN";
            var match = Regex.Match(headerOrFooter, $@"({beginOrEnd})\s+(?<format>[^-]+)", RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                throw new SignClientException(Resources.UnknownPemFormat);
            }

            return match.Groups["format"].Value.Trim();
        }
    }
}
