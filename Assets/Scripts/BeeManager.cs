using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class BeeManager : MonoBehaviour
{
    [SerializeField] private Mesh beeMesh;
    [SerializeField] private Material beeMaterial;
    [SerializeField] private Color[] teamColor;
    [SerializeField] private int maxSpawnDist;
    [SerializeField] private int numOfTeams;
    [SerializeField] private int initBeeSpawnNum;

    private int maxBeeSpwanNum = 50000;
    List<Bee> beelist;
    List<Bee> beePool;
    List<Bee>[] teamOfBees;
    public static BeeManager Instance;

    // 渲染合批的最大上限
    private const int beeMaxBatch = 1023;
    private List<List<Matrix4x4>> beeMatrix;
    private List<List<Vector4>> beeColor;
    private int batchIdx = 0;
    private MaterialPropertyBlock matProps;
    private List<Tuple<int, int>> dir;

    // 攻击欲望，值越高代表蜜蜂更容易攻击其他蜜蜂而非搬运资源
    [Range(0f, 1f)]
    [SerializeField] private float aggression;
    // 攻击距离
    [SerializeField] private float attackDistance;
    // 追击时速度系数
    [SerializeField] private float chaseScale;
    // 攻击时速度系数
    [SerializeField] private float attackScale;
    // 命中范围
    [SerializeField] private float hitDistance;
    [SerializeField] private float grabDistance;
    

    private void Awake() {
        if (Instance != null) {
            throw new Exception("BeeManager Instance has already existed!");
        }    
        Instance = this;

        dir = new List<Tuple<int, int>>();
        var dir0 = Tuple.Create(0, 1);
        dir.Add(dir0);
        var dir1 = Tuple.Create(1, 0);
        dir.Add(dir1);
        var dir2 = Tuple.Create(0 , -1);
        dir.Add(dir2);
        var dir3 = Tuple.Create(-1 , 0);
        dir.Add(dir3);
    }

    private void Start() {
        beelist = new List<Bee>(maxBeeSpwanNum);
		teamOfBees = new List<Bee>[numOfTeams];
		beePool = new List<Bee>(maxBeeSpwanNum);

        beeMatrix = new List<List<Matrix4x4>>();
		beeMatrix.Add(new List<Matrix4x4>());
		beeColor = new List<List<Vector4>>();
		beeColor.Add(new List<Vector4>());

        matProps = new MaterialPropertyBlock();
        matProps.SetVectorArray("_Color",new Vector4[beeMaxBatch]);

        for (int i = 0; i < numOfTeams; ++i) {
            teamOfBees[i] = new List<Bee>(maxSpawnDist / numOfTeams);
        }

        for (int i = 0; i < initBeeSpawnNum; ++i) {
            int team = i % numOfTeams;
            float spawnX = 40;
            float spawnY = 40;
            Vector3 pos = new Vector3(spawnX * dir[team].Item1, 0, spawnY * dir[team].Item2) + UnityEngine.Random.insideUnitSphere * maxSpawnDist;
            _SpawnBee(pos ,team);
        }
    }

    private void Update() {
        Quaternion rotation = Quaternion.identity;
        float size = 0.2f;
        Vector3 scale = new Vector3(size, size, size); 
        for (int i = 0; i < beelist.Count; ++i) {
            Color color= teamColor[beelist[i].GetTeam()];
            beeMatrix[i / beeMaxBatch][i % beeMaxBatch] = Matrix4x4.TRS(beelist[i].position, rotation, scale);
            beeColor[i / beeMaxBatch][i % beeMaxBatch] = color;
        }

        for (int i = 0; i <= batchIdx; i++) {
			if (beeMatrix[i].Count > 0) {
				matProps.SetVectorArray("_Color",beeColor[i]);
                Graphics.DrawMeshInstanced(beeMesh, 0, beeMaterial, beeMatrix[i], matProps);
			}
		}
    }

    private void FixedUpdate() {
        for (int i = 0; i < beelist.Count; ++i) {
            Bee bee = beelist[i];
            bee.isAttacking = false;
            bee.isHoldingResource = false;

            if (!bee.IsDead()) {
                bee.velocity = UnityEngine.Random.insideUnitSphere * maxSpawnDist;

                List<Bee> allies = teamOfBees[bee.GetTeam()];

                Bee attractiveFriend  = allies[UnityEngine.Random.Range(0, allies.Count)];
                Vector3 delta = attractiveFriend.position - bee.position;
                float dist = Mathf.Sqrt(delta.x * delta.x + delta.y * delta.y + delta.z * delta.z);
                if (dist > 0f) {
					bee.velocity += delta * (Time.fixedDeltaTime / dist);
				}

                Bee repellentFriend  = allies[UnityEngine.Random.Range(0, allies.Count)];
                delta = repellentFriend.position - bee.position;
                dist = Mathf.Sqrt(delta.x * delta.x + delta.y * delta.y + delta.z * delta.z);
                if (dist > 0f) {
					bee.velocity -= delta * (Time.fixedDeltaTime / dist);
				}

                if (bee.isHoldingResource == false && bee.targetBee == null) {
                    if (UnityEngine.Random.value < aggression) {
                        int emenyTeam = UnityEngine.Random.Range(0, numOfTeams);
                        while(emenyTeam == bee.GetTeam()) {
                            emenyTeam = UnityEngine.Random.Range(0, numOfTeams);
                        }

                        List<Bee> emeny = teamOfBees[emenyTeam];
                        if (emeny.Count > 0) {
							bee.targetBee = emeny[UnityEngine.Random.Range(0,emeny.Count)];
						}
                        else {
                            bee.carryResource = ResourceManager.TryGetRandomResource();
                        }

                    }
                }
                else if (bee.targetBee != null) {
                    if (bee.targetBee.IsDead()) {
                        bee.targetBee = null;
                    }
                    else {
                        delta = bee.targetBee.position - bee.position;
                        dist = Mathf.Sqrt(delta.x * delta.x + delta.y * delta.y + delta.z * delta.z);

                        if (dist > attackDistance) {
                            bee.velocity += delta * (chaseScale * Time.fixedDeltaTime / dist);
                        }
                        else {
                            bee.isAttacking = true;
                            bee.velocity += delta * (attackScale * Time.fixedDeltaTime / dist);
                            if (dist < hitDistance) {
                                //ParticleManager.SpawnParticle(bee.enemyTarget.position,ParticleType.Blood,bee.velocity * .35f,2f,6);
								bee.targetBee.SetDead(true);
								bee.targetBee.velocity *= .5f;
								bee.targetBee = null;
                            }
                        }
                    }
                }
                else if (bee.carryResource != null) {
                    Resource resource = bee.carryResource;
                    if (resource.GetHolder() == null) {
                        if (resource.dead == true) {
                            bee.carryResource = null;
                        }
                        else if (resource.stacked){
                            bee.carryResource = null;
                        }
                        else {
                            delta = resource.postion - bee.position;
                            dist = Mathf.Sqrt(delta.x * delta.x + delta.y * delta.y + delta.z * delta.z);
                            if (dist > grabDistance) {
                                bee.velocity += delta * (Time.fixedDeltaTime / dist);
                            }
                            else if (resource.stacked) {

                            }
                        }
                    }
                    else if (resource.GetHolder() == bee) { // 资源所有者就是蜜蜂

                    }
                    else if (resource.GetHolder().GetTeam() != bee.GetTeam()) {
                        bee.targetBee = resource.GetHolder();
                    }
                    else if (resource.GetHolder().GetTeam() == bee.GetTeam()) {
                        bee.carryResource = null;
                    }
                }
            }
            else {
                bee.velocity.y += Field.gravity * Time.fixedDeltaTime;
                bee.beeLifeTimer -= Time.fixedDeltaTime / 10f;
                if (bee.beeLifeTimer < 0f) {
                    DeleteBee(bee);
                }
            }

            bee.position += Time.fixedDeltaTime * bee.velocity;
        }
        
    }

    private void _SpawnBee(Vector3 pos, int team) {
        Bee bee;
        if (beePool.Count == 0) { // 使用对象池
            bee = new Bee();
        }
        else {
            bee = beePool[beePool.Count - 1];
            beePool.RemoveAt(beePool.Count - 1);
        }
        bee.Init(pos, team);
        beelist.Add(bee);
        teamOfBees[team].Add(bee);

        if (beeMatrix[batchIdx].Count == beeMaxBatch) {
            batchIdx++;
            if (beeMatrix.Count == batchIdx) {
                beeMatrix.Add(new List<Matrix4x4>());
                beeColor.Add(new List<Vector4>());
            }
        }

        beeMatrix[batchIdx].Add(Matrix4x4.identity);
        beeColor[batchIdx].Add(teamColor[team]);
    }

    private void DeleteBee(Bee bee) {
        beelist.Remove(bee);
        beePool.Add(bee);
        teamOfBees[bee.GetTeam()].Remove(bee);
        if (beeMatrix[batchIdx].Count == 0 && batchIdx > 0) {
            batchIdx--;
        }
        beeMatrix[batchIdx].RemoveAt(beeMatrix[batchIdx].Count - 1);
        beeColor[batchIdx].RemoveAt(beeColor[batchIdx].Count - 1);
    }

}
