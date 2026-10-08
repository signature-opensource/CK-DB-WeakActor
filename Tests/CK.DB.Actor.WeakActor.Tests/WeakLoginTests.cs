using CK.Core;
using CK.DB.Auth;
using CK.IO.Authentication.Weak;
using CK.SqlServer;
using CK.Testing;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Shouldly;
using System;
using System.Threading.Tasks;
using static CK.Testing.MonitorTestHelper;

namespace CK.DB.Actor.WeakActor.Tests;

/// <summary>
/// The weak actors are the actors of the "Weak" authentication provider.
/// </summary>
[TestFixture]
public class WeakLoginTests
{
    static WeakActorTable Table => SharedEngine.Map.StObjs.Obtain<WeakActorTable>().ShouldNotBeNull();

    static IWeakLoginCommand Command( string name )
    {
        return SharedEngine.AutomaticServices.GetRequiredService<PocoDirectory>().Create<IWeakLoginCommand>( c => c.WeakActorName = name );
    }

    static async Task<(int Id, string Name)> CreateAsync( bool enabled )
    {
        using var ctx = new SqlStandardCallContext( TestHelper.Monitor );
        var name = "Weak-" + Guid.NewGuid().ToString( "N" );
        var id = await Table.CreateAsync( ctx, 1, name );
        if( enabled ) await Table.EnableAsync( ctx, 1, id );
        return (id, name);
    }

    [Test]
    public async Task an_enabled_weak_actor_logs_in_at_the_Unsafe_level_Async()
    {
        var p = SharedEngine.AutomaticServices.GetRequiredService<SqlLoginProvider>();
        p.ProviderNames.ShouldContain( "Weak" );
        var (id, name) = await CreateAsync( enabled: true );

        (await p.LoginAsync( TestHelper.Monitor, "Weak", Command( name ), actualLogin: false )).IsSuccess.ShouldBeTrue();
        var r = await p.LoginAsync( TestHelper.Monitor, "Weak", Command( name ) );
        r.IsSuccess.ShouldBeTrue();
        r.Actor!.ActorId.ShouldBe( id );
        r.Actor.ActorName.ShouldBe( name, "The DisplayName of CK.vWeakActor." );
        r.Actor.IsWeak.ShouldBeTrue();
        r.Actor.Level.ShouldBe( AuthLevel.Unsafe );
        r.Actor.Schemes.ShouldHaveSingleItem().Name.ShouldBe( "Weak" );
        r.Actor.Schemes[0].LastUsed.ShouldBeGreaterThan( DateTime.UtcNow.AddMinutes( -1 ) );
    }

    [Test]
    public async Task unknown_or_disabled_weak_actors_are_InvalidUserKey_never_UnregisteredUser_Async()
    {
        var p = SharedEngine.AutomaticServices.GetRequiredService<SqlLoginProvider>();
        var (_, disabled) = await CreateAsync( enabled: false );

        foreach( var name in new[] { disabled, "Unknown-" + Guid.NewGuid().ToString( "N" ), "" } )
        {
            var r = await p.LoginAsync( TestHelper.Monitor, "Weak", Command( name ) );
            r.IsSuccess.ShouldBeFalse();
            r.IsUnregisteredUser.ShouldBeFalse();
            r.FailureCode.ShouldBe( (int)KnownLoginFailureCode.InvalidUserKey );
        }
    }

    [Test]
    public async Task a_weak_actor_is_read_at_the_Unsafe_level_even_before_its_first_login_Async()
    {
        var users = SharedEngine.AutomaticServices.GetRequiredService<SqlActorInfoProvider>();
        var (id, name) = await CreateAsync( enabled: true );

        var u = (await users.GetActorInfoAsync( TestHelper.Monitor, id )).ShouldNotBeNull();
        u.ActorName.ShouldBe( name );
        u.IsWeak.ShouldBeTrue();
        u.Level.ShouldBe( AuthLevel.Unsafe );
    }

    [Test]
    public async Task a_disabled_weak_actor_cannot_be_read_Async()
    {
        var users = SharedEngine.AutomaticServices.GetRequiredService<SqlActorInfoProvider>();
        var (id, _) = await CreateAsync( enabled: true );
        using( var ctx = new SqlStandardCallContext( TestHelper.Monitor ) )
        {
            await Table.DisableAsync( ctx, 1, id );
        }
        (await users.GetActorInfoAsync( TestHelper.Monitor, id )).ShouldBeNull( "The restoration of a command refuses it." );
    }

    [Test]
    public async Task a_weak_actor_is_not_an_account_to_bind_Async()
    {
        IAuthenticationDatabaseService db = SharedEngine.Map.StObjs.Obtain<Auth.Package>().ShouldNotBeNull();
        var provider = db.FindProvider( "Weak" ).ShouldNotBeNull();
        provider.CanCreatePayload.ShouldBeFalse();
        using var ctx = new SqlStandardCallContext( TestHelper.Monitor );
        await Should.ThrowAsync<NotSupportedException>( () => provider.CreateOrUpdateUserAsync( ctx, 1, 3712, Command( "X" ) ) );
    }
}
