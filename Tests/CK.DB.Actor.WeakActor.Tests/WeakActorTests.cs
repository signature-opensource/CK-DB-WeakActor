using CK.Core;
using CK.SqlServer;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.Threading.Tasks;
using static CK.Testing.DBSetupTestHelper;

namespace CK.DB.Actor.WeakActor.Tests
{
    public class WeakActorTests
    {
        WeakActorTable Table => TestHelper.StObjMap.StObjs.Obtain<WeakActorTable>();

        [Test]
        public async Task anonymous_cannot_create_weak_actors_Async()
        {
            using( var context = new SqlStandardCallContext() )
            {
                await Table.Invoking( sut => sut.CreateAsync( context, 0, Guid.NewGuid().ToString() ) )
                    .Should().ThrowAsync<SqlDetailedException>();
            }
        }

        [Test]
        public async Task can_create_weak_actor_Async()
        {
            using( var context = new SqlStandardCallContext() )
            {
                var weakActorId = await Table.CreateAsync( context, 1, Guid.NewGuid().ToString() );
                weakActorId.Should().BeGreaterThan( 0 );
            }
        }

        [Test]
        public async Task can_destroy_weak_actor_Async()
        {
            using( var context = new SqlStandardCallContext() )
            {
                var weakActorId = await Table.CreateAsync( context, 1, Guid.NewGuid().ToString() );

                await Table.DestroyAsync( context, 1, weakActorId );
            }
        }

        [Test]
        public async Task can_add_a_weak_actor_into_a_group_Async()
        {
            var groupTable = TestHelper.StObjMap.StObjs.Obtain<GroupTable>();

            using( var context = new SqlStandardCallContext() )
            {
                var groupId = await groupTable.CreateGroupAsync( context, 1 );
                var weakActorId = await Table.CreateAsync( context, 1, Guid.NewGuid().ToString() );

                await Table.Invoking( sut => sut.AddWeakActorIntoGroupAsync( context, 1, groupId, 3712 ) )
                    .Should().ThrowAsync<SqlDetailedException>();

                await Table.AddWeakActorIntoGroupAsync( context, 1, groupId, weakActorId );

                Table.Database.ExecuteReader( "select * from CK.tActorProfile where GroupId = @0 and ActorId = @1", groupId, weakActorId )
                    .Rows.Should().HaveCount( 1 );
            }
        }
    }
}
