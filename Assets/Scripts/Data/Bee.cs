using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bee
{
    public Vector3 position;
    public Vector3 velocity;
    private int team;
    public Bee targetBee;
    public Resource carryResource;
    public float beeLifeTimer = 1f;
    public bool isAttacking = false;
    public bool isHoldingResource = false;
    private bool dead = false;
    
    public void Init(Vector3 beePositon, int beeTeam) {
        position = beePositon;
        velocity = Vector3.zero;
        team = beeTeam;

        targetBee = null;
        carryResource = null;
    }

    public int GetTeam() {
        return team;
    }

    public bool IsDead() {
        return dead == true;
    }
    public void SetDead(bool death) {
        dead = death;
    }
}
