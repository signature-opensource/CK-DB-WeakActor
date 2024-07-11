using CK.Core;
using CK.SqlServer;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using static CK.Testing.MonitorTestHelper;
using Dapper;
using CK.Testing;

namespace CK.DB.Actor.WeakActor.Tests
{
    public class WeakActorTests
    {
        WeakActorTable Table => SharedEngine.Map.StObjs.Obtain<WeakActorTable>();

        [Test]
        public async Task anonymous_cannot_create_weak_actors_Async()
        {
            using( var context = new SqlStandardCallContext( TestHelper.Monitor ) )
            {
                await Table.Invoking( sut => sut.CreateAsync( context, 0, Guid.NewGuid().ToString() ) )
                           .Should()
                           .ThrowAsync<SqlDetailedException>();
            }
        }

        [Test]
        public async Task can_create_weak_actor_Async()
        {
            using( var context = new SqlStandardCallContext( TestHelper.Monitor ) )
            {
                var weakActorId = await Table.CreateAsync( context, 1, Guid.NewGuid().ToString() );
                weakActorId.Should().BeGreaterThan( 0 );
            }
        }

        [Test]
        public async Task can_destroy_weak_actor_Async()
        {
            using( var context = new SqlStandardCallContext( TestHelper.Monitor ) )
            {
                var weakActorId = await Table.CreateAsync( context, 1, Guid.NewGuid().ToString() );

                await Table.DestroyAsync( context, 1, weakActorId );
            }
        }

        [Test]
        public async Task can_add_a_weak_actor_into_a_group_Async()
        {
            var groupTable = SharedEngine.Map.StObjs.Obtain<GroupTable>();
            Debug.Assert( groupTable != null, nameof( groupTable ) + " != null" );
            var userTable = SharedEngine.Map.StObjs.Obtain<UserTable>();
            Debug.Assert( userTable != null, nameof( userTable ) + " != null" );

            using( var context = new SqlStandardCallContext( TestHelper.Monitor ) )
            {
                var groupId = await groupTable.CreateGroupAsync( context, 1 );
                var weakActorId = await Table.CreateAsync( context, 1, Guid.NewGuid().ToString() );

                var userActorId = await userTable.CreateUserAsync( context, 1, Guid.NewGuid().ToString() );
                await Table.Invoking( sut => sut.AddIntoGroupAsync( context, 1, groupId, userActorId ) )
                           .Should()
                           .ThrowAsync<SqlDetailedException>();

                await Table.AddIntoGroupAsync( context, 1, groupId, weakActorId );

                var sql = "select count(*) from CK.tActorProfile where GroupId = @groupId and ActorId = @weakActorId";
                context[Table].QuerySingle<int>( sql, new { groupId, weakActorId } )
                              .Should()
                              .Be( 1 );
            }
        }

        [Test]
        public void create_twice_the_same_weak_actor_name_should_throw()
        {
            using( var context = new SqlStandardCallContext( TestHelper.Monitor ) )
            {
                var name = Guid.NewGuid().ToString();
                Table.Create( context, 1, name );
                Table.Invoking( sut => sut.Create( context, 1, name ) )
                     .Should()
                     .Throw<SqlDetailedException>();
            }
        }
    }
}
