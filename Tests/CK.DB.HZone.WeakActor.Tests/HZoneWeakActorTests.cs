using CK.Core;
using CK.DB.Zone;
using CK.DB.Zone.WeakActor;
using CK.SqlServer;
using CK.Testing;
using Dapper;
using Microsoft.Data.SqlClient;
using NUnit.Framework;
using Shouldly;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static CK.Testing.MonitorTestHelper;

namespace CK.DB.HZone.WeakActor.Tests;

public class HZoneWeakActorTests
{
    Package Package => SharedEngine.Map.StObjs.Obtain<Package>().ShouldNotBeNull();
    WeakActorTable WeakActorTable => SharedEngine.Map.StObjs.Obtain<WeakActorTable>().ShouldNotBeNull();
    ZoneTable ZoneTable => SharedEngine.Map.StObjs.Obtain<ZoneTable>().ShouldNotBeNull();
    GroupTable GroupTable => SharedEngine.Map.StObjs.Obtain<GroupTable>().ShouldNotBeNull();

    [Test]
    public async Task adding_a_weak_actor_to_a_group_in_hierarchy_Async()
    {
        using( var context = new SqlStandardCallContext( TestHelper.Monitor ) )
        {
            var rootZoneId = await ZoneTable.CreateZoneAsync( context, 1 );
            var zoneId = await ZoneTable.CreateZoneAsync( context, 1, rootZoneId );
            var innerZoneId = await ZoneTable.CreateZoneAsync( context, 1, zoneId );
            var groupInInnerZoneId = await GroupTable.CreateGroupAsync( context, 1, innerZoneId );

            var weakActorId = await WeakActorTable.CreateAsync( context, 1, Guid.NewGuid().ToString(), zoneId );

            context[ZoneTable].QuerySingle<int>( $"select count(*) from CK.tActorProfile where GroupId = {zoneId} and ActorId = {weakActorId}" )
                          .ShouldBe( 1, "The WeakActor is in its defining Zone." );

            context[ZoneTable].QuerySingle<int>( $"select count(*) from CK.tActorProfile where GroupId = {rootZoneId} and ActorId = {weakActorId}" )
                          .ShouldBe( 0, "The WeakActor is NOT in the parent Zone!" );

            // The autoAddMemberInZone is false: the WeakActor is not in the innerZoneId Zone, this fails.
            await Should.ThrowAsync<SqlDetailedException>( () => GroupTable.AddMemberAsync( context, 1, groupInInnerZoneId, weakActorId, autoAddMemberInZone: false ) );

            // Add it to the Group in the the innerZoneId Zone.
            await GroupTable.AddMemberAsync( context, 1, groupInInnerZoneId, weakActorId, autoAddMemberInZone: true );

            context[ZoneTable].QuerySingle<int>( $"select count(*) from CK.tActorProfile where GroupId = {groupInInnerZoneId} and ActorId = {weakActorId}" )
                          .ShouldBe( 1, "The WeakActor is in the target Group." );

            context[ZoneTable].QuerySingle<int>( $"select count(*) from CK.tActorProfile where GroupId = {innerZoneId} and ActorId = {weakActorId}" )
                          .ShouldBe( 1, "And has been added to the innerZoneId Zone." );

            context[ZoneTable].QuerySingle<int>( $"select count(*) from CK.tActorProfile where GroupId = {rootZoneId} and ActorId = {weakActorId}" )
                          .ShouldBe( 0, "But he is NOT in the parent Zone!" );
        }
    }

    [Test]
    public async Task create_a_weak_actor_should_throw_when_weak_actor_name_candidate_is_in_hierarchy_already_Async()
    {
        using( var context = new SqlStandardCallContext( TestHelper.Monitor ) )
        {
            var rootZone = await ZoneTable.CreateZoneAsync( context, 1 );

            var childZoneA = await ZoneTable.CreateZoneAsync( context, 1, rootZone );
            var childZoneA1 = await ZoneTable.CreateZoneAsync( context, 1, childZoneA );
            var childZoneA2 = await ZoneTable.CreateZoneAsync( context, 1, childZoneA );

            var childZoneB = await ZoneTable.CreateZoneAsync( context, 1, rootZone );
            var childZoneB1 = await ZoneTable.CreateZoneAsync( context, 1, childZoneB );
            var childZoneB2 = await ZoneTable.CreateZoneAsync( context, 1, childZoneB );

            var bottomName = Guid.NewGuid().ToString();
            await WeakActorTable.CreateAsync( context, 1, bottomName, childZoneB2 );

            // Just to be sure that the any name can be added everywhere.
            await WeakActorTable.CreateAsync( context, 1, Guid.NewGuid().ToString() );
            await WeakActorTable.CreateAsync( context, 1, Guid.NewGuid().ToString(), rootZone );
            await WeakActorTable.CreateAsync( context, 1, Guid.NewGuid().ToString(), childZoneA );
            await WeakActorTable.CreateAsync( context, 1, Guid.NewGuid().ToString(), childZoneA1 );
            await WeakActorTable.CreateAsync( context, 1, Guid.NewGuid().ToString(), childZoneA2 );
            await WeakActorTable.CreateAsync( context, 1, Guid.NewGuid().ToString(), childZoneB );
            await WeakActorTable.CreateAsync( context, 1, Guid.NewGuid().ToString(), childZoneB1 );
            await WeakActorTable.CreateAsync( context, 1, Guid.NewGuid().ToString(), childZoneB2 );

            // Same (B2).
            (await Should.ThrowAsync<SqlDetailedException>( () => WeakActorTable.CreateAsync( context, 1, bottomName, childZoneB2 ) ))
                          .InnerException.ShouldBeOfType<SqlException>()
                          .Message.ShouldMatch( @".*WeakActor\.WeakActorNameShouldBeUniqueInHZone.*" );
            // Direct parent (B).
            (await Should.ThrowAsync<SqlDetailedException>( () => WeakActorTable.CreateAsync( context, 1, bottomName, childZoneB ) ))
                          .InnerException.ShouldBeOfType<SqlException>()
                          .Message.ShouldMatch( @".*WeakActor\.WeakActorNameShouldBeUniqueInHZone.*" );
            // Grand parent.
            (await Should.ThrowAsync<SqlDetailedException>( () => WeakActorTable.CreateAsync( context, 1, bottomName, rootZone ) ))
                          .InnerException.ShouldBeOfType<SqlException>()
                          .Message.ShouldMatch( @".*WeakActor\.WeakActorNameShouldBeUniqueInHZone.*" );
            // Root 0 zone.
            (await Should.ThrowAsync<SqlDetailedException>( () => WeakActorTable.CreateAsync( context, 1, bottomName, zoneId: 0 ) ))
                          .InnerException.ShouldBeOfType<SqlException>()
                          .Message.ShouldMatch( @".*WeakActor\.WeakActorNameShouldBeUniqueInHZone.*" );

            // The sibling B1 and all A branch are fine (but we must destroy them immediately).
            await Should.NotThrowAsync( async () =>
            {
                int id = await WeakActorTable.CreateAsync( context, 1, bottomName, childZoneB1 );
                await WeakActorTable.DestroyAsync( context, 1, id );
                id = await WeakActorTable.CreateAsync( context, 1, bottomName, childZoneA );
                await WeakActorTable.DestroyAsync( context, 1, id );
                id = await WeakActorTable.CreateAsync( context, 1, bottomName, childZoneA1 );
                await WeakActorTable.DestroyAsync( context, 1, id );
                id = await WeakActorTable.CreateAsync( context, 1, bottomName, childZoneA2 );
                await WeakActorTable.DestroyAsync( context, 1, id );
            } );

            // Registering a WeakActor in 0 prevents this name to ever exist anywhere else.
            var veryTopName = Guid.NewGuid().ToString();
            await WeakActorTable.CreateAsync( context, 1, veryTopName, zoneId: 0 );

            (await Should.ThrowAsync<SqlDetailedException>( () => WeakActorTable.CreateAsync( context, 1, veryTopName, zoneId: 0 ) ))
                          .InnerException.ShouldBeOfType<SqlException>()
                          .Message.ShouldMatch( @".*WeakActor\.WeakActorNameShouldBeUniqueInHZone.*" );
            (await Should.ThrowAsync<SqlDetailedException>( () => WeakActorTable.CreateAsync( context, 1, veryTopName, childZoneA2 ) ))
                          .InnerException.ShouldBeOfType<SqlException>()
                          .Message.ShouldMatch( @".*WeakActor\.WeakActorNameShouldBeUniqueInHZone.*" );
            (await Should.ThrowAsync<SqlDetailedException>( () => WeakActorTable.CreateAsync( context, 1, veryTopName, childZoneB ) ))
                          .InnerException.ShouldBeOfType<SqlException>()
                          .Message.ShouldMatch( @".*WeakActor\.WeakActorNameShouldBeUniqueInHZone.*" );
            (await Should.ThrowAsync<SqlDetailedException>( () => WeakActorTable.CreateAsync( context, 1, veryTopName, childZoneB1 ) ))
                          .InnerException.ShouldBeOfType<SqlException>()
                          .Message.ShouldMatch( @".*WeakActor\.WeakActorNameShouldBeUniqueInHZone.*" );
            (await Should.ThrowAsync<SqlDetailedException>( () => WeakActorTable.CreateAsync( context, 1, veryTopName, rootZone ) ))
                          .InnerException.ShouldBeOfType<SqlException>()
                          .Message.ShouldMatch( @".*WeakActor\.WeakActorNameShouldBeUniqueInHZone.*" );
            (await Should.ThrowAsync<SqlDetailedException>( () => WeakActorTable.CreateAsync( context, 1, veryTopName, childZoneB2 ) ))
                          .InnerException.ShouldBeOfType<SqlException>()
                          .Message.ShouldMatch( @".*WeakActor\.WeakActorNameShouldBeUniqueInHZone.*" );


            // In B.
            var topName = Guid.NewGuid().ToString();
            await WeakActorTable.CreateAsync( context, 1, topName, childZoneB );

            (await Should.ThrowAsync<SqlDetailedException>( () => WeakActorTable.CreateAsync( context, 1, veryTopName, childZoneB ) ))
                          .InnerException.ShouldBeOfType<SqlException>()
                          .Message.ShouldMatch( @".*WeakActor\.WeakActorNameShouldBeUniqueInHZone.*" );
            (await Should.ThrowAsync<SqlDetailedException>( () => WeakActorTable.CreateAsync( context, 1, veryTopName, childZoneB2 ) ))
                          .InnerException.ShouldBeOfType<SqlException>()
                          .Message.ShouldMatch( @".*WeakActor\.WeakActorNameShouldBeUniqueInHZone.*" );
            (await Should.ThrowAsync<SqlDetailedException>( () => WeakActorTable.CreateAsync( context, 1, veryTopName, childZoneB1 ) ))
                          .InnerException.ShouldBeOfType<SqlException>()
                          .Message.ShouldMatch( @".*WeakActor\.WeakActorNameShouldBeUniqueInHZone.*" );
            (await Should.ThrowAsync<SqlDetailedException>( () => WeakActorTable.CreateAsync( context, 1, veryTopName, rootZone ) ))
                          .InnerException.ShouldBeOfType<SqlException>()
                          .Message.ShouldMatch( @".*WeakActor\.WeakActorNameShouldBeUniqueInHZone.*" );
            (await Should.ThrowAsync<SqlDetailedException>( () => WeakActorTable.CreateAsync( context, 1, veryTopName, 0 ) ))
                          .InnerException.ShouldBeOfType<SqlException>()
                          .Message.ShouldMatch( @".*WeakActor\.WeakActorNameShouldBeUniqueInHZone.*" );
            // in A branch it's ok.
            await Should.NotThrowAsync( async () =>
            {
                int id = await WeakActorTable.CreateAsync( context, 1, bottomName, childZoneA );
                await WeakActorTable.DestroyAsync( context, 1, id );
                id = await WeakActorTable.CreateAsync( context, 1, bottomName, childZoneA1 );
                await WeakActorTable.DestroyAsync( context, 1, id );
                id = await WeakActorTable.CreateAsync( context, 1, bottomName, childZoneA2 );
                await WeakActorTable.DestroyAsync( context, 1, id );
            } );
        }
    }

}
