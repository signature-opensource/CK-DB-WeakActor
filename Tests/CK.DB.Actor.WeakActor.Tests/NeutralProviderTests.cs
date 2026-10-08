using NUnit.Framework;
using Shouldly;
using System;
using System.IO;

namespace CK.DB.Actor.WeakActor.Tests;

/// <summary>
/// CK.DB.Actor.WeakActor is used by both authentication stacks: it must not bring CK.Cris.Authentication (even
/// transitively), that defines the same CK.Auth.CrisAuthenticationService as the old CK.Cris.Auth (the generated code of an
/// old-stack application would not compile). The IWeakLoginCommand handler of the new stack is in the AuthCenter package
/// until the old stack is retired.
/// </summary>
[TestFixture]
public class NeutralProviderTests
{
    [Test]
    public void the_provider_does_not_bring_the_new_authentication_stack()
    {
        // Every transitive dependency of the test project (that references the provider) is copied here.
        File.Exists( Path.Combine( AppContext.BaseDirectory, "CK.Cris.Authentication.dll" ) ).ShouldBeFalse();
    }
}
