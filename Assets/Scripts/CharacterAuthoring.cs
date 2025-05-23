using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public struct CharacterMoveDirection : IComponentData
{
    public float2 Value;
}

public struct CharacterMoveSpeed : IComponentData
{
    public float Value;
}

public class CharacterAuthoring : MonoBehaviour
{
    public float MoveSpeed = 5;
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
        }
    }
}

public partial struct CharacterMoveSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach(var (velocity, direction, speed) in SystemAPI.Query<RefRW<PhysicsVelocity>, CharacterMoveDirection, CharacterMoveSpeed>())
        {
            float2 moveStep = direction.Value * speed.Value;
            float3 currentVelocity = velocity.ValueRO.Linear;

            velocity.ValueRW.Linear = new float3(moveStep.x, currentVelocity.y, moveStep.y);
        }
    }
}
