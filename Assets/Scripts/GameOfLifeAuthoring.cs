using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace GameOfLife
{
    public class GameOfLifeAuthoring : MonoBehaviour
    {
        [SerializeField] float livingScale = 0.9f;
        [SerializeField] int StartLivingPercent = 10;
        [SerializeField] int worldLength = 2;
        [SerializeField] float updateInterval = 0.2f;
        [SerializeField] Mesh mesh;
        [SerializeField] Material material;
        [SerializeField] Slider slider;
        private NativeList<Entity> entities;
        private NativeList<bool> lives;
        private NativeList<int3> positions;
        private EntityManager entityManager;
        private float elapsedTime = 0;
        private void Awake()
        {
            Debug.Log("GameOfLifeAuthoring Create");
            entities = new NativeList<Entity>(Allocator.Persistent);
            lives = new NativeList<bool>(Allocator.Persistent);
            positions = new NativeList<int3>(Allocator.Persistent);
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            EntityArchetype entityArchetype = entityManager.CreateArchetype(
                typeof(Scale),
                typeof(Translation),
                typeof(RenderMesh),
                typeof(LocalToWorld),
                typeof(RenderBounds),
                typeof(WorldRenderBounds),
                typeof(ChunkWorldRenderBounds)
                );
            for (int x = 0; x < worldLength; x++)
            {
                for (int y = 0; y < worldLength; y++)
                {
                    for (int z = 0; z < worldLength; z++)
                    {
                        var entity = entityManager.CreateEntity(entityArchetype);
                        entityManager.SetName(entity, "living thing");
                        entityManager.SetSharedComponentData(entity, new RenderMesh() { mesh = mesh, material = material, castShadows = ShadowCastingMode.On, receiveShadows = true });
                        entityManager.SetComponentData(entity, new Translation { Value = new float3(x, y, z) });
                        bool live = UnityEngine.Random.Range(0, 100) < StartLivingPercent;
                        entityManager.SetComponentData(entity, new Scale { Value = live ? livingScale : 0 });
                        lives.Add(live);
                        positions.Add(new int3(x, y, z));
                        entities.Add(entity);
                    }
                }
            }
        }

        private void OnDestroy()
        {
            entities.Dispose();
            lives.Dispose();
        }

        private void Update()
        {
            elapsedTime += Time.deltaTime;
            if (elapsedTime > updateInterval)
            {
                elapsedTime = 0;
                UpdateLives();
            }
        }

        private void UpdateLives()
        {
            NativeArray<bool> newLives = new NativeArray<bool>(lives.Length, Allocator.Temp);
            for (int i = 0; i < lives.Length; i++)
            {
                int3 pos = To3D(i);
                int liveNeighbors = 0;

                for (int x = pos.x - 1; x <= pos.x + 1; x++)
                {
                    for (int y = pos.y - 1; y <= pos.y + 1; y++)
                    {
                        for (int z = pos.z - 1; z <= pos.z + 1; z++)
                        {
                            if (x != pos.x && y != pos.y && z != pos.z)
                            {
                                if (PosLives(x, y, z)) liveNeighbors++;
                            }
                        }
                    }
                }

                if (lives[i] == true && liveNeighbors == 2 || liveNeighbors == 3) newLives[i] = true;
                else if (liveNeighbors == 3 && lives[i] == false) newLives[i] = true;
                else newLives[i] = false;
            }

            for (int i = 0; i < entities.Length; i++)
            {
                Entity entity = entities[i];
                entityManager.SetComponentData(entity, new Scale() { Value = newLives[i] ? livingScale : 0 });
                lives[i] = newLives[i];
            }

            newLives.Dispose();
        }

        private bool PosLives(int x, int y, int z)
        {
            int index = To1D(x, y, z);
            if (index >= 0 && index < lives.Length)
            {
                return lives[index];
            }
            return false;
        }

        public int To1D(int x, int y, int z)
        {
            return (z * worldLength * worldLength) + (y * worldLength) + x;
        }

        public int3 To3D(int idx)
        {
            int z = idx / (worldLength * worldLength);
            idx -= z * worldLength * worldLength;
            int y = idx / worldLength;
            int x = idx % worldLength;
            return new int3(x, y, z);
        }

        public void ResetWorld()
        {
            for (int i = 0; i < lives.Length; i++)
            {
                bool live = UnityEngine.Random.Range(0, 100) < StartLivingPercent;
                entityManager.SetComponentData(entities[i], new Scale { Value = live ? livingScale : 0 });
                lives[i] = live;
            }
        }

        public void SetStartLivePercent()
        {
            StartLivingPercent = (int)slider.value;
        }
    }
}