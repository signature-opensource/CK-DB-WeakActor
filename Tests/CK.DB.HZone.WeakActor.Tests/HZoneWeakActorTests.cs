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
    Actor.UserTable UserTable => SharedEngine.Map.StObjs.Obtain<Actor.UserTable>().ShouldNotBeNull();

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

    [Test]
    public async Task a_WeakActor_defined_in_the_root_Zone_0_can_be_in_any_Zone_and_Group_Async()
    {
        using( var context = new SqlStandardCallContext( TestHelper.Monitor ) )
        {
            var zoneA = await ZoneTable.CreateZoneAsync( context, 1 );
            var zoneA1 = await ZoneTable.CreateZoneAsync( context, 1, zoneA );
            var zoneA11 = await ZoneTable.CreateZoneAsync( context, 1, zoneA1 );
            var zoneB = await ZoneTable.CreateZoneAsync( context, 1 );
            var unzonedGroup = await GroupTable.CreateGroupAsync( context, 1, 0 );
            var groupA11 = await GroupTable.CreateGroupAsync( context, 1, zoneA11 );
            var groupB = await GroupTable.CreateGroupAsync( context, 1, zoneB );

            var weakActorId = await WeakActorTable.CreateAsync( context, 1, Guid.NewGuid().ToString(), zoneId: 0 );

            // Like any member, it is implicitly in the Zone 0.
            await GroupTable.AddMemberAsync( context, 1, unzonedGroup, weakActorId );
            IsMember( context, unzonedGroup, weakActorId ).ShouldBeTrue();

            // Like any member, it must be in the parent Zones...
            (await Should.ThrowAsync<SqlDetailedException>( () => ZoneTable.AddMemberAsync( context, 1, zoneA1, weakActorId ) ))
                          .InnerException.ShouldBeOfType<SqlException>()
                          .Message.ShouldMatch( @".*Zone\.MemberNotInParentZone.*" );
            // ...or be automatically added to them.
            await GroupTable.AddMemberAsync( context, 1, groupA11, weakActorId, autoAddMemberInZone: true );
            IsMember( context, groupA11, weakActorId ).ShouldBeTrue();
            IsMember( context, zoneA11, weakActorId ).ShouldBeTrue();
            IsMember( context, zoneA1, weakActorId ).ShouldBeTrue();
            IsMember( context, zoneA, weakActorId ).ShouldBeTrue();

            // A top level Zone.
            await ZoneTable.AddMemberAsync( context, 1, zoneB, weakActorId );
            await GroupTable.AddMemberAsync( context, 1, groupB, weakActorId );
            IsMember( context, groupB, weakActorId ).ShouldBeTrue();

            // It can be removed from any Zone (the Zone 0 is its defining Zone).
            await ZoneTable.RemoveMemberAsync( context, 1, zoneA, weakActorId );
            IsMember( context, zoneA, weakActorId ).ShouldBeFalse();
            IsMember( context, zoneA11, weakActorId ).ShouldBeFalse();
            IsMember( context, groupA11, weakActorId ).ShouldBeFalse();

            // It is always reachable: moving a Group keeps it (like a User).
            await ZoneTable.AddMemberAsync( context, 1, zoneA, weakActorId );
            await GroupTable.MoveGroupAsync( context, 1, groupB, zoneA );
            IsMember( context, groupB, weakActorId ).ShouldBeTrue();
        }
    }

    [TestCase( GroupMoveOption.None )]
    [TestCase( GroupMoveOption.Intersect )]
    [TestCase( GroupMoveOption.AutoUserRegistration )]
    public async Task moving_a_Group_removes_the_WeakActors_that_are_no_more_reachable_Async( GroupMoveOption option )
    {
        using( var context = new SqlStandardCallContext( TestHelper.Monitor ) )
        {
            var rootZone = await ZoneTable.CreateZoneAsync( context, 1 );
            var zoneA1 = await ZoneTable.CreateZoneAsync( context, 1, rootZone );
            var zoneA2 = await ZoneTable.CreateZoneAsync( context, 1, rootZone );

            // A User in both zones.
            var userId = await UserTable.CreateUserAsync( context, 1, Guid.NewGuid().ToString() );
            await ZoneTable.AddMemberAsync( context, 1, zoneA1, userId, autoAddMemberInParentZone: true );
            await ZoneTable.AddMemberAsync( context, 1, zoneA2, userId, autoAddMemberInParentZone: true );
            // A WeakActor defined in the root zone, registered in both zones: it is reachable from A1 and A2.
            var rootWeakActorId = await WeakActorTable.CreateAsync( context, 1, Guid.NewGuid().ToString(), rootZone );
            await ZoneTable.AddMemberAsync( context, 1, zoneA1, rootWeakActorId, autoAddMemberInParentZone: true );
            await ZoneTable.AddMemberAsync( context, 1, zoneA2, rootWeakActorId, autoAddMemberInParentZone: true );
            // A WeakActor defined in A1: it is not reachable from A2.
            var a1WeakActorId = await WeakActorTable.CreateAsync( context, 1, Guid.NewGuid().ToString(), zoneA1 );

            var groupId = await GroupTable.CreateGroupAsync( context, 1, zoneA1 );
            await GroupTable.AddMemberAsync( context, 1, groupId, userId );
            await GroupTable.AddMemberAsync( context, 1, groupId, rootWeakActorId );
            await GroupTable.AddMemberAsync( context, 1, groupId, a1WeakActorId );

            await GroupTable.MoveGroupAsync( context, 1, groupId, zoneA2, option );

            IsMember( context, groupId, a1WeakActorId ).ShouldBeFalse( "The A1 WeakActor is no more reachable." );
            IsMember( context, groupId, rootWeakActorId ).ShouldBeTrue( "The root WeakActor is still reachable." );
            IsMember( context, groupId, userId ).ShouldBeTrue();
            IsMember( context, zoneA1, a1WeakActorId ).ShouldBeTrue( "The A1 WeakActor is still in its defining Zone." );
        }
    }

    [TestCase( GroupMoveOption.None )]
    [TestCase( GroupMoveOption.Intersect )]
    [TestCase( GroupMoveOption.AutoUserRegistration )]
    public async Task moving_a_Group_below_the_defining_Zone_handles_the_WeakActors_like_Users_Async( GroupMoveOption option )
    {
        using( var context = new SqlStandardCallContext( TestHelper.Monitor ) )
        {
            var zoneA = await ZoneTable.CreateZoneAsync( context, 1 );
            var zoneA1 = await ZoneTable.CreateZoneAsync( context, 1, zoneA );

            // The User and the WeakActor are in A but not in A1.
            var userId = await UserTable.CreateUserAsync( context, 1, Guid.NewGuid().ToString() );
            await ZoneTable.AddMemberAsync( context, 1, zoneA, userId );
            var weakActorId = await WeakActorTable.CreateAsync( context, 1, Guid.NewGuid().ToString(), zoneA );

            var groupId = await GroupTable.CreateGroupAsync( context, 1, zoneA );
            await GroupTable.AddMemberAsync( context, 1, groupId, userId );
            await GroupTable.AddMemberAsync( context, 1, groupId, weakActorId );

            if( option == GroupMoveOption.None )
            {
                (await Should.ThrowAsync<SqlDetailedException>( () => GroupTable.MoveGroupAsync( context, 1, groupId, zoneA1, option ) ))
                              .InnerException.ShouldBeOfType<SqlException>()
                              .Message.ShouldMatch( @".*Group\.MemberNotInZone.*" );
                IsMember( context, groupId, weakActorId ).ShouldBeTrue();
                return;
            }
            await GroupTable.MoveGroupAsync( context, 1, groupId, zoneA1, option );
            if( option == GroupMoveOption.Intersect )
            {
                IsMember( context, groupId, userId ).ShouldBeFalse();
                IsMember( context, groupId, weakActorId ).ShouldBeFalse();
                IsMember( context, zoneA1, weakActorId ).ShouldBeFalse();
            }
            else
            {
                IsMember( context, groupId, userId ).ShouldBeTrue();
                IsMember( context, zoneA1, userId ).ShouldBeTrue( "The User has been registered in A1..." );
                IsMember( context, groupId, weakActorId ).ShouldBeTrue();
                IsMember( context, zoneA1, weakActorId ).ShouldBeTrue( "...and so has the WeakActor." );
            }
            IsMember( context, zoneA, weakActorId ).ShouldBeTrue( "The WeakActor is still in its defining Zone." );
        }
    }

    [Test]
    public async Task moving_a_Group_with_only_reachable_WeakActors_with_None_option_throws_Async()
    {
        using( var context = new SqlStandardCallContext( TestHelper.Monitor ) )
        {
            var zoneA = await ZoneTable.CreateZoneAsync( context, 1 );
            var zoneA1 = await ZoneTable.CreateZoneAsync( context, 1, zoneA );
            var weakActorId = await WeakActorTable.CreateAsync( context, 1, Guid.NewGuid().ToString(), zoneA );
            var groupId = await GroupTable.CreateGroupAsync( context, 1, zoneA );
            await GroupTable.AddMemberAsync( context, 1, groupId, weakActorId );

            // The WeakActor is not in A1: like a User, this fails (the Group is not silently cleared).
            (await Should.ThrowAsync<SqlDetailedException>( () => GroupTable.MoveGroupAsync( context, 1, groupId, zoneA1, GroupMoveOption.None ) ))
                          .InnerException.ShouldBeOfType<SqlException>()
                          .Message.ShouldMatch( @".*Group\.MemberNotInZone.*" );
            IsMember( context, groupId, weakActorId ).ShouldBeTrue();
        }
    }

    [TestCase( false )]
    [TestCase( true )]
    public async Task moving_a_Zone_checks_the_WeakActorName_uniqueness_Async( bool useGroupMove )
    {
        using( var context = new SqlStandardCallContext( TestHelper.Monitor ) )
        {
            var name = Guid.NewGuid().ToString();
            var zoneA = await ZoneTable.CreateZoneAsync( context, 1 );
            var zoneA1 = await ZoneTable.CreateZoneAsync( context, 1, zoneA );
            var zoneB = await ZoneTable.CreateZoneAsync( context, 1 );
            var zoneB1 = await ZoneTable.CreateZoneAsync( context, 1, zoneB );
            var zoneC = await ZoneTable.CreateZoneAsync( context, 1 );
            await WeakActorTable.CreateAsync( context, 1, name, zoneA );
            await WeakActorTable.CreateAsync( context, 1, name, zoneB1 );

            Task Move( int zoneId, int newParentZoneId ) => useGroupMove
                                                            ? GroupTable.MoveGroupAsync( context, 1, zoneId, newParentZoneId )
                                                            : ZoneTable.MoveZoneAsync( context, 1, zoneId, newParentZoneId );

            // B1 (defines the name) below A1 (A defines the name).
            (await Should.ThrowAsync<SqlDetailedException>( () => Move( zoneB1, zoneA1 ) ))
                          .InnerException.ShouldBeOfType<SqlException>()
                          .Message.ShouldMatch( @".*WeakActor\.WeakActorNameShouldBeUniqueInHZone.*" );
            // B (its child B1 defines the name) below A.
            (await Should.ThrowAsync<SqlDetailedException>( () => Move( zoneB, zoneA ) ))
                          .InnerException.ShouldBeOfType<SqlException>()
                          .Message.ShouldMatch( @".*WeakActor\.WeakActorNameShouldBeUniqueInHZone.*" );
            context[ZoneTable].QuerySingle<int>( $"select ZoneId from CK.tGroup where GroupId = {zoneB}" ).ShouldBe( 0 );

            // A (defines the name) below C: no clash.
            await Move( zoneA, zoneC );
            // A1 (its parent A defines the name) below B: A1 doesn't define the name, no clash.
            await Move( zoneA1, zoneB );
            context[ZoneTable].QuerySingle<int>( $"select ZoneId from CK.tGroup where GroupId = {zoneA1}" ).ShouldBe( zoneB );
        }
    }

    [TestCase( false )]
    [TestCase( true )]
    public async Task moving_a_Zone_removes_the_WeakActors_that_are_no_more_reachable_Async( bool useGroupMove )
    {
        using( var context = new SqlStandardCallContext( TestHelper.Monitor ) )
        {
            var rootZone = await ZoneTable.CreateZoneAsync( context, 1 );
            var zoneA = await ZoneTable.CreateZoneAsync( context, 1, rootZone );
            var zoneA1 = await ZoneTable.CreateZoneAsync( context, 1, zoneA );
            var zoneA11 = await ZoneTable.CreateZoneAsync( context, 1, zoneA1 );
            var groupA11 = await GroupTable.CreateGroupAsync( context, 1, zoneA11 );
            var zoneB = await ZoneTable.CreateZoneAsync( context, 1, rootZone );

            // A WeakActor defined in A: it is registered in A1 and A11 and in the Group of A11.
            var aWeakActorId = await WeakActorTable.CreateAsync( context, 1, Guid.NewGuid().ToString(), zoneA );
            await GroupTable.AddMemberAsync( context, 1, groupA11, aWeakActorId, autoAddMemberInZone: true );
            IsMember( context, zoneA1, aWeakActorId ).ShouldBeTrue();
            IsMember( context, zoneA11, aWeakActorId ).ShouldBeTrue();

            // A WeakActor defined in A1: it moves with its Zone.
            var a1WeakActorId = await WeakActorTable.CreateAsync( context, 1, Guid.NewGuid().ToString(), zoneA1 );
            await GroupTable.AddMemberAsync( context, 1, groupA11, a1WeakActorId, autoAddMemberInZone: true );
            IsMember( context, zoneA11, a1WeakActorId ).ShouldBeTrue();

            // Moves A1 from A to B.
            if( useGroupMove )
            {
                await GroupTable.MoveGroupAsync( context, 1, zoneA1, zoneB );
            }
            else
            {
                await ZoneTable.MoveZoneAsync( context, 1, zoneA1, zoneB );
            }
            context[ZoneTable].QuerySingle<int>( $"select ZoneId from CK.tGroup where GroupId = {zoneA1}" ).ShouldBe( zoneB );

            IsMember( context, zoneA1, aWeakActorId ).ShouldBeFalse( "The A WeakActor is no more reachable..." );
            IsMember( context, zoneA11, aWeakActorId ).ShouldBeFalse( "...from any child zone..." );
            IsMember( context, groupA11, aWeakActorId ).ShouldBeFalse( "...nor from their groups." );

            IsMember( context, zoneA1, a1WeakActorId ).ShouldBeTrue( "The A1 WeakActor follows its defining Zone..." );
            IsMember( context, zoneA11, a1WeakActorId ).ShouldBeTrue( "...its child zones..." );
            IsMember( context, groupA11, a1WeakActorId ).ShouldBeTrue( "...and their groups." );
        }
    }

    bool IsMember( SqlStandardCallContext context, int groupId, int actorId )
    {
        return context[ZoneTable].QuerySingle<int>( "select count(*) from CK.tActorProfile where GroupId = @groupId and ActorId = @actorId", new { groupId, actorId } ) == 1;
    }

}
