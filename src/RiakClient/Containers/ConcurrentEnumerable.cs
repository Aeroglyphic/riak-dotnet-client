// <copyright file="ConcurrentEnumerable.cs" company="Basho Technologies, Inc.">
// Copyright 2011 - OJ Reeves & Jeremiah Peschka
// Copyright 2014 - Basho Technologies, Inc.
//
// This file is provided to you under the Apache License,
// Version 2.0 (the "License"); you may not use this file
// except in compliance with the License.  You may obtain
// a copy of the License at
//
//   http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing,
// software distributed under the License is distributed on an
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
// KIND, either express or implied.  See the License for the
// specific language governing permissions and limitations
// under the License.
// </copyright>

namespace RiakClient.Containers
{
    using System.Collections.Generic;

    internal class ConcurrentEnumerable<T> : IConcurrentEnumerable<T>
    {
        private readonly IEnumerable<T> wrapped;

        public ConcurrentEnumerable(IEnumerable<T> wrapped)
        {
            this.wrapped = wrapped;
        }

        public IConcurrentEnumerator<T> GetEnumerator()
        {
            return new ConcurrentEnumerator<T>(wrapped.GetEnumerator());
        }
    }
}
