using System.ComponentModel;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using Unity.Collections;
using UnityEngine;
using Unity.Physics.Systems;

public struct EnemyTag : IComponentData { }

public struct EnemyAttackData : IComponentData
{
    public float HitPoints;
    public float CooldownTime;
}

public struct EnemyCooldownExpirationTimestamp : IComponentData, IEnableableComponent
{
    public double Value;
}

[RequireComponent(typeof(CharacterAuthoring))]
public class EnemyAuthoring : MonoBehaviour
{
    public float AttackDamage = 1;
    public float CoolDownTime = 0.2f;
    private class Baker : Baker<EnemyAuthoring>
    {
        public override void Bake(EnemyAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent<EnemyTag>(entity);
            AddComponent(entity, new EnemyAttackData
            {
                HitPoints = authoring.AttackDamage,
                CooldownTime = authoring.CoolDownTime
            });
            AddComponent<EnemyCooldownExpirationTimestamp>(entity);
            SetComponentEnabled<EnemyCooldownExpirationTimestamp>(entity, false);
        }
    }
}

[BurstCompile]
public partial struct EnemyMoveToPayerSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<PlayerTag>();
    }

    public void OnUpdate (ref SystemState state)
    {
        var playerEntity = SystemAPI.GetSingletonEntity<PlayerTag>();
        var playerPosition = SystemAPI.GetComponent<LocalTransform>(playerEntity).Position;

        var moveToPlayerJob = new EnemyMoveToPlayerJob
        {
            PlayerPosition = playerPosition
        };

        state.Dependency = moveToPlayerJob.ScheduleParallel(state.Dependency);
    }
}

[BurstCompile]
[WithAll(typeof(EnemyTag))]
public partial struct EnemyMoveToPlayerJob : IJobEntity
{
    public float3 PlayerPosition;

    private void Execute (ref CharacterMoveDirection dir, in LocalTransform transform)
    {
        var vectorToPlayer = PlayerPosition - transform.Position;
        dir.Value = math.normalize(new float2(vectorToPlayer.x, vectorToPlayer.z));
    }
}

[UpdateInGroup(typeof(PhysicsSystemGroup))]
[UpdateAfter(typeof(PhysicsSimulationGroup))]
[UpdateBefore(typeof(AfterPhysicsSystemGroup))]
public partial struct EnemyAttackSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<SimulationSingleton>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var epalsedTime = SystemAPI.Time.ElapsedTime;

        foreach (var (expirationTimestamp, cooldownEnabled) in SystemAPI.Query<EnemyCooldownExpirationTimestamp, EnabledRefRW<EnemyCooldownExpirationTimestamp>>())
        {
            if (expirationTimestamp.Value >  epalsedTime)
                continue;

            cooldownEnabled.ValueRW = false;
        }

        var attackJob = new EnemyAttackJob
        {
            PlayerLookup = SystemAPI.GetComponentLookup<PlayerTag>(true),
            AttackDataLookup = SystemAPI.GetComponentLookup<EnemyAttackData>(true),
            CooldownLookup = SystemAPI.GetComponentLookup<EnemyCooldownExpirationTimestamp>(),
            DamageBufferLookup = SystemAPI.GetBufferLookup<DamageThisFrame>(),
            ElapsedTime = epalsedTime,
        };

        var simulationSingleton = SystemAPI.GetSingleton<SimulationSingleton>();
        state.Dependency = attackJob.Schedule(simulationSingleton, state.Dependency);
    }
}

[BurstCompile]
public struct EnemyAttackJob : ICollisionEventsJob
{
    [Unity.Collections.ReadOnly] public ComponentLookup<PlayerTag> PlayerLookup;
    [Unity.Collections.ReadOnly] public ComponentLookup<EnemyAttackData> AttackDataLookup;

    public ComponentLookup<EnemyCooldownExpirationTimestamp> CooldownLookup;

    public BufferLookup<DamageThisFrame> DamageBufferLookup;

    public double ElapsedTime;

    public void Execute(CollisionEvent collisionEvent)
    {
        Entity playerEntity;
        Entity enemyEntity;

        if (PlayerLookup.HasComponent(collisionEvent.EntityA) && AttackDataLookup.HasComponent(collisionEvent.EntityB))
        {
            playerEntity = collisionEvent.EntityA;
            enemyEntity = collisionEvent.EntityB;
        }
        else if (PlayerLookup.HasComponent(collisionEvent.EntityB) && AttackDataLookup.HasComponent(collisionEvent.EntityA))
        {
            playerEntity = collisionEvent.EntityB;
            enemyEntity = collisionEvent.EntityA;
        }
        else
        {
            return;
        }

        if(CooldownLookup.IsComponentEnabled(enemyEntity))
            return;

        var attackData = AttackDataLookup[enemyEntity];
        CooldownLookup[enemyEntity] = new EnemyCooldownExpirationTimestamp { Value = ElapsedTime + attackData.CooldownTime };
        CooldownLookup.SetComponentEnabled(enemyEntity, true);

        var playerDamageBuffer = DamageBufferLookup[playerEntity];
        playerDamageBuffer.Add(new DamageThisFrame { Value = attackData.HitPoints });
    }
}