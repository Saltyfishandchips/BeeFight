using System;
using System.Collections.Generic;
using UnityEngine;

public class ResourceManager : MonoBehaviour
{
    [SerializeField] private Mesh resourceMesh;
    [SerializeField] private Material resourceMaterial;
    [SerializeField] private Vector2 initPosition;
    [SerializeField] private int startResourceNum; 

    private List<Resource> resourceList;
    private List<Matrix4x4> resourceMatirx;
    public static ResourceManager Instance;

    private void Awake() {
        if (Instance != null) {
            throw new Exception("Resource Manager already has Instance!");
        }
        Instance = this;
    }

    private void Start() {
        resourceList = new List<Resource>();
        resourceMatirx = new List<Matrix4x4>();

        for (int i = 0; i < startResourceNum; ++i) {
            SpwanResource();
        }

    }

    private void Update() {
        float resourceSize = 1f;
        Vector3 scale = new Vector3(resourceSize, resourceSize * 0.5f, resourceSize);

        for (int i = 0; i < resourceList.Count; ++i) {
            Resource resource = resourceList[i];
            if (resource.GetHolder() != null) {
                if (resource.GetHolder().IsDead()) {
                // 携带者已经死亡
                resource.SetHolder(null);
                }
                else {
                    Vector3 targetPos = resource.GetHolder().position - Vector3.up * resourceSize;
                    resource.postion = Vector3.Lerp(resource.postion, targetPos, Time.deltaTime);
                    resource.veclocity = resource.GetHolder().velocity;
                }
            }
            else if (resource.stacked == false) {

            }
            
        }

        for (int i = 0; i < resourceList.Count; ++i) {
            resourceMatirx[i] = Matrix4x4.TRS(resourceList[i].postion, Quaternion.identity, scale);
        }

        Graphics.DrawMeshInstanced(resourceMesh, 0, resourceMaterial, resourceMatirx);
    }

    public static Resource TryGetRandomResource() {
        if (Instance.resourceList.Count == 0) {
            return null;
        }
        else {
            Resource resource = Instance.resourceList[UnityEngine.Random.Range(0, Instance.resourceList.Count)];
            if (resource.GetHolder() == null) {
                return resource;
            }
            else {
                return null;
            }
        }
    }

    private void SpwanResource() {
        float gridScale = 0.1f;
        Vector3 position = new Vector3(initPosition.x + gridScale * UnityEngine.Random.Range(-1f, 1f) * Field.size.x, 0, initPosition.y + gridScale * UnityEngine.Random.Range(-1f, 1f) * Field.size.z);
        Resource resource = new Resource(position);

        resourceList.Add(resource);
        resourceMatirx.Add(Matrix4x4.identity);
    }

    private void DeleteResource(Resource resource) {
        resource.dead = true;
        resourceList.Remove(resource);
        resourceMatirx.RemoveAt(resourceMatirx.Count - 1);
    }

    
}
