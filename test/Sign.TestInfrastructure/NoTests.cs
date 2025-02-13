// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE.txt file in the project root for more information.

using Xunit;

namespace Sign.TestInfrastructure;

public sealed class NoTests
{
    [Fact]
    public void Pass()
    {
        Assert.True(true);
    }
}
