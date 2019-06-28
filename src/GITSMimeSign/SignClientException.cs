// Copyright (c) 2019 Glenn Watson. All rights reserved.
// Glenn Watson licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;

namespace GitSMimeSign
{
    /// <summary>
    /// An exception that happens as part of the sign client operation.
    /// </summary>
    public class SignClientException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SignClientException"/> class.
        /// </summary>
        public SignClientException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SignClientException"/> class.
        /// </summary>
        /// <param name="message">The message about the issue.</param>
        public SignClientException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SignClientException"/> class.
        /// </summary>
        /// <param name="message">The message about the issue.</param>
        /// <param name="innerException">The inner exception.</param>
        public SignClientException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
