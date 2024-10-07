using CK.Core;
using CK.DB.Zone;
using CK.SqlServer;
using CK.Testing;
using Dapper;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using NUnit.Framework;
using System;
using static CK.Testing.MonitorTestHelper;

namespace CK.DB.HZone.WeakActor.Tests;

public class HZoneWeakActorTests
{
    Package Package => SharedEngine.Map.StObjs.Obtain<Package>();

    WeakActorTable WeakActorTable => SharedEngine.Map.StObjs.Obtain<WeakActorTable>();

    ZoneTable ZoneTable => SharedEngine.Map.StObjs.Obtain<ZoneTable>();

    GroupTable GroupTable => SharedEngine.Map.StObjs.Obtain<GroupTable>();

    [Test]
    public void can_be_added_to_every_group_inside_zone()
    {
        using( var context = new SqlStandardCallContext( TestHelper.Monitor ) )
        {
            var zoneId = ZoneTable.CreateZone( context, 1 );
            var groupId1 = GroupTable.CreateGroup( context, 1, zoneId );
            var groupId2 = GroupTable.CreateGroup( context, 1, zoneId );
            var groupId3 = GroupTable.CreateGroup( context, 1, zoneId );
            var weakActorId = WeakActorTable.Create( context, 1, Guid.NewGuid().ToString(), zoneId );

            WeakActorTable.AddIntoGroup( context, 1, groupId1, weakActorId );
            WeakActorTable.AddIntoGroup( context, 1, groupId2, weakActorId );
            WeakActorTable.AddIntoGroup( context, 1, groupId3, weakActorId );
        }
    }

    [Test]
    public void can_add_weak_actor_to_a_group_in_hierarchy()
    {
        using( var context = new SqlStandardCallContext( TestHelper.Monitor ) )
        {
            var rootZone = ZoneTable.CreateZone( context, 1 );
            var zone = ZoneTable.CreateZone( context, 1, rootZone );
            var innerZone = ZoneTable.CreateZone( context, 1, zone );
            var groupInInnerZone = GroupTable.CreateGroup( context, 1, innerZone );

            var weakActor = WeakActorTable.Create( context, 1, Guid.NewGuid().ToString(), zone );
            WeakActorTable.AddIntoGroup( context, 1, groupInInnerZone, weakActor );
        }
    }

    [Test]
    public void move_a_simple_zone_containing_a_weak_actor_with_option_0_none_should_throw()
    {
        using( var context = new SqlStandardCallContext( TestHelper.Monitor ) )
        {
            var rootZoneOrigin = ZoneTable.CreateZone( context, 1 );
            var rootZoneTarget = ZoneTable.CreateZone( context, 1 );
            var zoneUnderTest = ZoneTable.CreateZone( context, 1, rootZoneOrigin );

            WeakActorTable.Create( context, 1, Guid.NewGuid().ToString(), zoneUnderTest );

            ZoneTable.Invoking
                     (
                         sut => sut.MoveZone( context, 1, zoneUnderTest, rootZoneTarget, GroupMoveOption.None )
                     )
                     .Should()
                     .Throw<SqlDetailedException>()
                     .WithInnerException<SqlException>()
                     .WithMessage( "*Group.UserNotInZone*" );

            ZoneTable.MoveZone( context, 1, zoneUnderTest, rootZoneOrigin, GroupMoveOption.None );
        }
    }

    [Test]
    public void move_a_simple_zone_not_containing_any_weak_actor_with_option_1_none_should_not_throw()
    {
        using( var context = new SqlStandardCallContext( TestHelper.Monitor ) )
        {
            var rootZoneOrigin = ZoneTable.CreateZone( context, 1 );
            var rootZoneTarget = ZoneTable.CreateZone( context, 1 );
            var zoneUnderTest = ZoneTable.CreateZone( context, 1, rootZoneOrigin );

            ZoneTable.MoveZone( context, 1, zoneUnderTest, rootZoneTarget, GroupMoveOption.Intersect );
            ZoneTable.MoveZone( context, 1, zoneUnderTest, rootZoneOrigin, GroupMoveOption.Intersect );
        }
    }

    [Test]
    public void move_a_simple_zone_containing_a_weak_actor_with_option_1_none_should_throw()
    {
        // sZoneMove => sGroupMove => sGroupUserRemove => sZoneUserRemove
        //TODO: Here we want either :
        // - throw because the last call (sZoneUserRemove) throws by design.
        // - Leave the weak actor in the zone, so check :
        //       * Unique name in the new HZone
        //       * Groups that are in the old hzone, not in the new hzone are not compatible
        // but the second option may be more about option 2 auto registration in a specific way for WA.
        using( var context = new SqlStandardCallContext() )
        {
            var rootZoneOrigin = ZoneTable.CreateZone( context, 1 );
            var rootZoneTarget = ZoneTable.CreateZone( context, 1 );
            var zoneUnderTest = ZoneTable.CreateZone( context, 1, rootZoneOrigin );

            WeakActorTable.Create( context, 1, Guid.NewGuid().ToString(), zoneUnderTest );

            ZoneTable.Invoking( sut => sut.MoveZone( context, 1, zoneUnderTest, rootZoneTarget, GroupMoveOption.Intersect ) )
                     .Should()
                     .Throw<SqlDetailedException>()
                     .WithInnerException<SqlException>()
                     .WithMessage( "*Zone.CannotRemoveWeakActor*" );
        }
    }

    [Test]
    public void move_a_simple_zone_containing_a_weak_actor_with_option_2_none_should_throw()
    {
        using( var context = new SqlStandardCallContext( TestHelper.Monitor ) )
        {
            var rootZoneOrigin = ZoneTable.CreateZone( context, 1 );
            var rootZoneTarget = ZoneTable.CreateZone( context, 1 );
            var zoneUnderTest = ZoneTable.CreateZone( context, 1, rootZoneOrigin );

            WeakActorTable.Create( context, 1, Guid.NewGuid().ToString(), zoneUnderTest );

            ZoneTable.Invoking( sut => sut.MoveZone( context,
                                                     1,
                                                     zoneUnderTest,
                                                     rootZoneTarget,
                                                     GroupMoveOption.AutoUserRegistration ) )
                     .Should()
                     .Throw<SqlDetailedException>()
                     .WithInnerException<SqlException>()
                     .WithMessage( "*Group.WeakActorCannotUseAutoUserRegistration*" );
        }
    }

    [Test]
    public void should_find_a_weak_actor_name_in_hierarchy()
    {
        using( var context = new SqlStandardCallContext( TestHelper.Monitor ) )
        {
            var weakActorName = Guid.NewGuid().ToString();
            var rootZone = ZoneTable.CreateZone( context, 1 );
            var childZone = ZoneTable.CreateZone( context, 1, rootZone );
            var otherChildZone = ZoneTable.CreateZone( context, 1, rootZone );
            var childChildZone = ZoneTable.CreateZone( context, 1, childZone );

            WeakActorTable.IsWeakActorNameInHierarchy( context, 0, weakActorName ).Should().BeFalse();
            WeakActorTable.IsWeakActorNameInHierarchy( context, rootZone, weakActorName ).Should().BeFalse();
            WeakActorTable.IsWeakActorNameInHierarchy( context, childZone, weakActorName ).Should().BeFalse();
            WeakActorTable.IsWeakActorNameInHierarchy( context, otherChildZone, weakActorName ).Should().BeFalse();
            WeakActorTable.IsWeakActorNameInHierarchy( context, childChildZone, weakActorName ).Should().BeFalse();

            WeakActorTable.Create( context, 1, weakActorName, childZone );

            WeakActorTable.IsWeakActorNameInHierarchy( context, 0, weakActorName ).Should().BeTrue();
            WeakActorTable.IsWeakActorNameInHierarchy( context, rootZone, weakActorName ).Should().BeTrue();
            WeakActorTable.IsWeakActorNameInHierarchy( context, childZone, weakActorName ).Should().BeTrue();
            WeakActorTable.IsWeakActorNameInHierarchy( context, otherChildZone, weakActorName ).Should().BeTrue();
            WeakActorTable.IsWeakActorNameInHierarchy( context, childChildZone, weakActorName ).Should().BeTrue();
        }
    }

    [Test]
    public void create_a_weak_actor_should_throw_when_weak_actor_name_candidate_is_in_hierarchy_already()
    {
        using( var context = new SqlStandardCallContext( TestHelper.Monitor ) )
        {
            var weakActorName = Guid.NewGuid().ToString();
            var rootZone = ZoneTable.CreateZone( context, 1 );

            var childZone10 = ZoneTable.CreateZone( context, 1, rootZone );
            var childZone11 = ZoneTable.CreateZone( context, 1, childZone10 );
            var childZone12 = ZoneTable.CreateZone( context, 1, childZone10 );

            var childZone20 = ZoneTable.CreateZone( context, 1, rootZone );
            var childZone21 = ZoneTable.CreateZone( context, 1, childZone20 );
            var childZone22 = ZoneTable.CreateZone( context, 1, childZone20 );

            WeakActorTable.Create( context, 1, weakActorName, childZone22 );

            WeakActorTable.Create( context, 1, Guid.NewGuid().ToString() );
            WeakActorTable.Create( context, 1, Guid.NewGuid().ToString(), rootZone );
            WeakActorTable.Create( context, 1, Guid.NewGuid().ToString(), childZone10 );
            WeakActorTable.Create( context, 1, Guid.NewGuid().ToString(), childZone11 );
            WeakActorTable.Create( context, 1, Guid.NewGuid().ToString(), childZone12 );
            WeakActorTable.Create( context, 1, Guid.NewGuid().ToString(), childZone20 );
            WeakActorTable.Create( context, 1, Guid.NewGuid().ToString(), childZone21 );
            WeakActorTable.Create( context, 1, Guid.NewGuid().ToString(), childZone22 );

            // Why the inner message is not the same as the test i've written in CK.DB.Actor ?
            WeakActorTable.Invoking( sut => sut.Create( context, 1, weakActorName ) )
                          .Should()
                          .Throw<SqlDetailedException>()
                          .WithInnerException<SqlException>()
                          .WithMessage( "*WeakActor.WeakActorNameShouldBeUniqueInHZone*" );
            WeakActorTable.Invoking( sut => sut.Create( context, 1, weakActorName, rootZone ) )
                          .Should()
                          .Throw<SqlDetailedException>()
                          .WithInnerException<SqlException>()
                          .WithMessage( "*WeakActor.WeakActorNameShouldBeUniqueInHZone*" );
            WeakActorTable.Invoking( sut => sut.Create( context, 1, weakActorName, childZone10 ) )
                          .Should()
                          .Throw<SqlDetailedException>()
                          .WithInnerException<SqlException>()
                          .WithMessage( "*WeakActor.WeakActorNameShouldBeUniqueInHZone*" );
            WeakActorTable.Invoking( sut => sut.Create( context, 1, weakActorName, childZone11 ) )
                          .Should()
                          .Throw<SqlDetailedException>()
                          .WithInnerException<SqlException>()
                          .WithMessage( "*WeakActor.WeakActorNameShouldBeUniqueInHZone*" );
            WeakActorTable.Invoking( sut => sut.Create( context, 1, weakActorName, childZone12 ) )
                          .Should()
                          .Throw<SqlDetailedException>()
                          .WithInnerException<SqlException>()
                          .WithMessage( "*WeakActor.WeakActorNameShouldBeUniqueInHZone*" );
            WeakActorTable.Invoking( sut => sut.Create( context, 1, weakActorName, childZone20 ) )
                          .Should()
                          .Throw<SqlDetailedException>()
                          .WithInnerException<SqlException>()
                          .WithMessage( "*WeakActor.WeakActorNameShouldBeUniqueInHZone*" );
            WeakActorTable.Invoking( sut => sut.Create( context, 1, weakActorName, childZone21 ) )
                          .Should()
                          .Throw<SqlDetailedException>()
                          .WithInnerException<SqlException>()
                          .WithMessage( "*WeakActor.WeakActorNameShouldBeUniqueInHZone*" );
            WeakActorTable.Invoking( sut => sut.Create( context, 1, weakActorName, childZone22 ) )
                          .Should()
                          .Throw<SqlDetailedException>()
                          .WithInnerException<SqlException>()
                          .WithMessage( "*WeakActor.WeakActorNameShouldBeUniqueInHZone*" );
        }
    }

    #region sWeakActorZoneMove

    [Test]
    public void should_move_weak_actor_to_a_new_zone_in_a_perfect_world()
    {
        using( var context = new SqlStandardCallContext( TestHelper.Monitor ) )
        {
            var zoneId = ZoneTable.CreateZone( context, 1 );
            var weakActorName = Guid.NewGuid().ToString();
            var weakActorId = WeakActorTable.Create( context, 1, weakActorName, zoneId );

            var zoneIdTarget = ZoneTable.CreateZone( context, 1 );

            WeakActorTable.MoveZone( context, 1, weakActorId, zoneIdTarget );

            var zoneIdResult = context[WeakActorTable].QuerySingle<int>
            (
                "select ZoneId from CK.vWeakActor where WeakActorId=@weakActorId",
                new { weakActorId }
            );
            zoneIdResult.Should().Be( zoneIdTarget );

            var oldProfileResult = context[WeakActorTable].QuerySingle<int>
            (
                "select 1 from CK.tActorProfile where ActorId=@weakActorId and GroupId=@zoneIdTarget",
                new { weakActorId, zoneIdTarget }
            );
            oldProfileResult.Should().Be( 1 );

            var newProfileResult = context[WeakActorTable].Query<int>
            (
                "select 1 from CK.tActorProfile where ActorId=@weakActorId and GroupId=@zoneId",
                new { weakActorId, zoneId }
            );
            newProfileResult.Should().BeEmpty();

            WeakActorTable.MoveZone( context, 1, weakActorId, zoneId );
        }
    }

    [Test]
    public void can_move_a_weak_actor_to_a_new_zone_in_the_same_hierarchy()
    {
        using( var context = new SqlStandardCallContext( TestHelper.Monitor ) )
        {
            var baseZoneId = ZoneTable.CreateZone( context, 1 );
            var zoneId = ZoneTable.CreateZone( context, 1, baseZoneId );
            var weakActorName = Guid.NewGuid().ToString();
            var weakActorId = WeakActorTable.Create( context, 1, weakActorName, zoneId );

            var zoneIdTarget = ZoneTable.CreateZone( context, 1, baseZoneId );

            WeakActorTable.MoveZone( context, 1, weakActorId, zoneIdTarget );

            var zoneIdResult = context[WeakActorTable].QuerySingle<int>
            (
                "select ZoneId from CK.vWeakActor where WeakActorId=@weakActorId",
                new { weakActorId }
            );
            zoneIdResult.Should().Be( zoneIdTarget );

            var newProfileResult = context[WeakActorTable].QuerySingle<int>
            (
                "select 1 from CK.tActorProfile where ActorId=@weakActorId and GroupId=@zoneIdTarget",
                new { weakActorId, zoneIdTarget }
            );
            newProfileResult.Should().Be( 1 );

            var oldProfileResult = context[WeakActorTable].Query<int>
            (
                "select 1 from CK.tActorProfile where ActorId=@weakActorId and GroupId=@zoneId",
                new { weakActorId, zoneId }
            );
            oldProfileResult.Should().BeEmpty();

            WeakActorTable.MoveZone( context, 1, weakActorId, zoneId );
        }
    }

    [Test]
    public void should_throw_when_moving_a_weak_actor_to_a_new_hierarchy_where_the_name_clash()
    {
        using( var context = new SqlStandardCallContext( TestHelper.Monitor ) )
        {
            var baseZoneId = ZoneTable.CreateZone( context, 1 );
            var targetBaseZoneId = ZoneTable.CreateZone( context, 1 );
            var weakActorName = Guid.NewGuid().ToString();
            WeakActorTable.Create( context, 1, weakActorName, targetBaseZoneId );

            var zoneId = ZoneTable.CreateZone( context, 1, baseZoneId );
            var weakActorId = WeakActorTable.Create( context, 1, weakActorName, zoneId );

            var zoneIdTarget = ZoneTable.CreateZone( context, 1, targetBaseZoneId );

            WeakActorTable.Invoking( sut => sut.MoveZone( context, 1, weakActorId, zoneIdTarget ) )
                          .Should().Throw<SqlDetailedException>()
                          .WithInnerException<SqlException>()
                          .WithMessage( "*WeakActor.WeakActorNameShouldBeUniqueInHZone*" );
        }
    }

    [Test]
    public void should_not_throw_when_moving_a_weak_actor_with_a_new_unique_name()
    {
        using( var context = new SqlStandardCallContext( TestHelper.Monitor ) )
        {
            var zoneId = ZoneTable.CreateZone( context, 1 );
            var weakActorName = Guid.NewGuid().ToString();
            var weakActorId = WeakActorTable.Create( context, 1, weakActorName, zoneId );

            var zoneIdTarget = ZoneTable.CreateZone( context, 1 );
            WeakActorTable.Create( context, 1, weakActorName, zoneIdTarget );


            var displayName = context[WeakActorTable].QuerySingle<string>
            (
                "select DisplayName from CK.vWeakActor where WeakActorId=@weakActorId",
                new { weakActorId }
            );
            WeakActorTable.MoveZone( context, 1, weakActorId, zoneIdTarget, newWeakActorName: displayName );
        }
    }

    [Test]
    public void should_throw_when_moving_a_weak_actor_to_a_new_zone_while_in_a_group_out_of_new_hierarchy()
    {
        using( var context = new SqlStandardCallContext( TestHelper.Monitor ) )
        {
            var baseZoneId = ZoneTable.CreateZone( context, 1 );
            var zoneId = ZoneTable.CreateZone( context, 1, baseZoneId );
            var groupId = GroupTable.CreateGroup( context, 1, baseZoneId );

            var weakActorName = Guid.NewGuid().ToString();
            var weakActorId = WeakActorTable.Create( context, 1, weakActorName, zoneId );
            WeakActorTable.AddIntoGroup( context, 1, groupId, weakActorId );

            var baseZoneIdTarget = ZoneTable.CreateZone( context, 1 );
            var zoneIdTarget = ZoneTable.CreateZone( context, 1, baseZoneIdTarget );

            WeakActorTable.Invoking( sut => sut.MoveZone( context, 1, weakActorId, zoneIdTarget ) )
                          .Should().Throw<SqlDetailedException>()
                          .WithInnerException<SqlException>()
                          .WithMessage( "*WeakActor.IsInAGroup*" );
        }
    }

    [Test]
    public void can_move_a_weak_actor_to_a_new_zone_while_only_being_in_a_group_inside_new_hierarchy()
    {
        using( var context = new SqlStandardCallContext( TestHelper.Monitor ) )
        {
            var baseZoneId = ZoneTable.CreateZone( context, 1 );
            var zoneId = ZoneTable.CreateZone( context, 1, baseZoneId );
            var groupId = GroupTable.CreateGroup( context, 1, baseZoneId );
            var zoneIdTarget = ZoneTable.CreateZone( context, 1, baseZoneId );

            var weakActorName = Guid.NewGuid().ToString();
            var weakActorId = WeakActorTable.Create( context, 1, weakActorName, zoneId );
            WeakActorTable.AddIntoGroup( context, 1, groupId, weakActorId );

            WeakActorTable.MoveZone( context, 1, weakActorId, zoneIdTarget );

            var zoneIdResult = context[WeakActorTable].QuerySingle<int>
            (
                "select ZoneId from CK.vWeakActor where WeakActorId=@weakActorId",
                new { weakActorId }
            );
            zoneIdResult.Should().Be( zoneIdTarget );

            var newProfileResult = context[WeakActorTable].QuerySingle<int>
            (
                "select 1 from CK.tActorProfile where ActorId=@weakActorId and GroupId=@zoneIdTarget",
                new { weakActorId, zoneIdTarget }
            );
            newProfileResult.Should().Be( 1 );

            var oldProfileResult = context[WeakActorTable].Query<int>
            (
                "select 1 from CK.tActorProfile where ActorId=@weakActorId and GroupId=@zoneId",
                new { weakActorId, zoneId }
            );
            oldProfileResult.Should().BeEmpty();

            WeakActorTable.MoveZone( context, 1, weakActorId, zoneId );
        }

    }

    [Test]
    public void should_not_throw_when_moving_a_weak_actor_to_a_new_zone_while_in_a_group_with_option_1_intersect()
    {
        using( var context = new SqlStandardCallContext( TestHelper.Monitor ) )
        {
            var zoneId = ZoneTable.CreateZone( context, 1 );
            var weakActorName = Guid.NewGuid().ToString();
            var weakActorId = WeakActorTable.Create( context, 1, weakActorName, zoneId );

            var groupId = GroupTable.CreateGroup( context, 1, zoneId );
            WeakActorTable.AddIntoGroup( context, 1, groupId, weakActorId );

            var zoneIdTarget = ZoneTable.CreateZone( context, 1 );

            WeakActorTable.MoveZone( context, 1, weakActorId, zoneIdTarget, option: Zone.WeakActor.WeakActorZoneMoveOption.Intersect );

            var zoneIdResult = context[WeakActorTable].QuerySingle<int>
            (
                "select ZoneId from CK.vWeakActor where WeakActorId=@weakActorId",
                new { weakActorId }
            );
            zoneIdResult.Should().Be( zoneIdTarget );

            var oldProfileResult = context[WeakActorTable].QuerySingle<int>
            (
                "select 1 from CK.tActorProfile where ActorId=@weakActorId and GroupId=@zoneIdTarget",
                new { weakActorId, zoneIdTarget }
            );
            oldProfileResult.Should().Be( 1 );

            var newProfileResult = context[WeakActorTable].Query<int>
            (
                "select 1 from CK.tActorProfile where ActorId=@weakActorId and GroupId=@zoneId",
                new { weakActorId, zoneId }
            );
            newProfileResult.Should().BeEmpty();

            WeakActorTable.MoveZone( context, 1, weakActorId, zoneId );

            context[WeakActorTable].Query( "select * from CK.tActorProfile where ActorId=@weakActorId and GroupId=@groupId", new { weakActorId, groupId } )
                                   .Should().BeEmpty();
        }
    }

    #endregion
}
