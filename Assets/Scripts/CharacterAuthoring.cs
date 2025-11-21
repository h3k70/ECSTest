using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

public struct CharacterMoveDirection : IComponentData
{
    public float2 Value;
}

public struct CharacterMoveSpeed : IComponentData
{
    public float Value;
}

public struct CharacterMaxHitPoints : IComponentData
{
    public float Value;
}

public struct CharacterCurrentHitPoints : IComponentData
{
    public float Value;
}

public struct DamageThisFrame : IBufferElementData
{
    public float Value; 
}

public struct Stun : IComponentData
{
    public float Time;
}

public struct CurrentTarget : IComponentData
{
    public float3 Value;
}

public class CharacterAuthoring : MonoBehaviour
{
    public float MoveSpeed = 5;
    public float MaxHP = 100;

    private class Baker : Baker<CharacterAuthoring>
    {
        public override void Bake(CharacterAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent<CharacterMoveDirection>(entity);
            AddComponent(entity, new CharacterMoveSpeed
            {
                Value = authoring.MoveSpeed
            });
            AddComponent(entity, new CharacterMaxHitPoints
            {
                Value = authoring.MaxHP
            });
            AddComponent(entity, new CharacterCurrentHitPoints
            {
                Value = authoring.MaxHP
            });
            AddBuffer<DamageThisFrame>(entity);
        }
    }
}

public partial struct CharacterMoveSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        
        foreach(var (velocity, direction, speed) in SystemAPI.Query<RefRW<PhysicsVelocity>, CharacterMoveDirection, CharacterMoveSpeed>().WithNone<Stun>())
        {
            float2 moveStep = direction.Value * speed.Value;
            float3 currentVelocity = velocity.ValueRO.Linear;

            velocity.ValueRW.Linear = new float3(moveStep.x, currentVelocity.y, moveStep.y);
        }
        /*
        var rotateJob = new CharacterMoveJob();
        state.Dependency = rotateJob.ScheduleParallel(state.Dependency);
        */
    }
}

[BurstCompile]
public partial struct CharacterMoveJob : IJobEntity
{
    private void Execute(ref PhysicsVelocity velocity, in CharacterMoveDirection direction, in CharacterMoveSpeed speed)
    {
        float2 moveStep = direction.Value * speed.Value;
        float3 currentVelocity = velocity.Linear;

        velocity.Linear = new float3(moveStep.x, currentVelocity.y, moveStep.y);
    }
}

[BurstCompile]
public partial struct CharacterRotateSysyem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var rotateJob = new CharacterRotateJob();
        state.Dependency = rotateJob.ScheduleParallel(state.Dependency);
    }
}

[BurstCompile]
public partial struct CharacterRotateJob : IJobEntity
{
    private void Execute(ref LocalTransform transform, in CharacterMoveDirection dir)
    {
        float3 newDir = new float3(dir.Value.x, 0f, dir.Value.y);

        if (math.lengthsq(newDir) > 0.0001f)
        {
            quaternion targetRotation = quaternion.LookRotation(newDir, math.up());
            transform.Rotation = targetRotation;
        }
    }
}


public partial struct ProcessDamageThisFrameSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (hp, damageThisFrame) in SystemAPI.Query<RefRW<CharacterCurrentHitPoints>, DynamicBuffer<DamageThisFrame>>())
        {
            if (damageThisFrame.IsEmpty)
                continue;

            foreach (var damage in damageThisFrame)
            {
                hp.ValueRW.Value -= damage.Value;
            }
            damageThisFrame.Clear();
        }
    }
}
