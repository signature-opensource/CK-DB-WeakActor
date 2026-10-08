using CK.Core;
using CK.DB.Auth;
using CK.IO.Authentication.Weak;
using CK.IO.Authentication.WeakZone;
using CK.SqlServer;
using CK.Testing;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Shouldly;
using System;
using System.Threading.Tasks;
using static CK.Testing.MonitorTestHelper;

namespace CK.DB.Zone.WeakActor.Tests;

/// <summary>
/// With zones, the name of a weak actor is unique in its zone only: the login command carries the zone.
/// </summary>
[TestFixture]
public class ZoneWeakLoginTests
{
    static WeakActorTable Table => SharedEngine.Map.StObjs.Obtain<WeakActorTable>().ShouldNotBeNull();

    static IWeakZoneLoginCommand Command( string name, int zoneId )
    {
        var c = SharedEngine.AutomaticServices.GetRequiredService<PocoDirectory>().Create<IWeakLoginCommand>( c => c.WeakActorName = name );
        // CK.IO.Authentication.WeakZone extends the command.
        var z = c.ShouldBeAssignableTo<IWeakZoneLoginCommand>();
        z.ZoneId = zoneId;
        return z;
    }

    [Test]
    public async Task the_zone_selects_the_weak_actor_Async()
    {
        var zones = SharedEngine.Map.StObjs.Obtain<ZoneTable>().ShouldNotBeNull();
        var p = SharedEngine.AutomaticServices.GetRequiredService<SqlLoginProvider>();
        var name = "Weak-" + Guid.NewGuid().ToString( "N" );
        int zone1, zone2, zone3, id1, id2;
        using( var ctx = new SqlStandardCallContext( TestHelper.Monitor ) )
        {
            zone1 = await zones.CreateZoneAsync( ctx, 1 );
            zone2 = await zones.CreateZoneAsync( ctx, 1 );
            zone3 = await zones.CreateZoneAsync( ctx, 1 );
            id1 = await Table.CreateAsync( ctx, 1, name, zone1 );
            id2 = await Table.CreateAsync( ctx, 1, name, zone2 );
            await Table.EnableAsync( ctx, 1, id1 );
            await Table.EnableAsync( ctx, 1, id2 );
        }

        var r1 = await p.LoginAsync( TestHelper.Monitor, "Weak", Command( name, zone1 ) );
        r1.User.ShouldNotBeNull().UserId.ShouldBe( id1 );
        r1.User.UserName.ShouldBe( $"{name} (#Zone-{zone1})", "The DisplayName of CK.vWeakActor, transformed by CK.DB.Zone.WeakActor." );
        r1.User.Level.ShouldBe( AuthLevel.Unsafe );

        var r2 = await p.LoginAsync( TestHelper.Monitor, "Weak", Command( name, zone2 ) );
        r2.User.ShouldNotBeNull().UserId.ShouldBe( id2 );

        var r3 = await p.LoginAsync( TestHelper.Monitor, "Weak", Command( name, zone3 ) );
        r3.IsSuccess.ShouldBeFalse();
        r3.FailureCode.ShouldBe( (int)KnownLoginFailureCode.InvalidUserKey );
    }
}
