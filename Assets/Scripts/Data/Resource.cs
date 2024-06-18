using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Resource
{
    private Bee beeHolder;
    private int gridX;
    private int gridY;
    public Vector3 postion;
    public Vector3 veclocity;
    public bool stacked {set; get;}
    public bool dead = false;

    public Resource(Vector3 targetPostion) {
        postion = targetPostion;
    }

    public Bee GetHolder() {
        return beeHolder;
    }

    public void SetHolder(Bee bee) {
        beeHolder = bee;
    }
}
