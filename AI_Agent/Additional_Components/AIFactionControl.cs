﻿using UnityEngine;

[DisallowMultipleComponent]
public class AIFactionControl : MonoBehaviour
{
    public int Faction = 1;
    string FactionName = "Enemy";

    void Start()
    {
        if (Faction == 0)
        {
            SetToAlly();
        }
        else if (Faction == 1)
        {
            SetToEnemy();
        }
        else
        {
            Faction = 1;
            SetToEnemy();
        }
    }

    void SetToEnemy()
    {
        gameObject.tag = "Enemy";
        FactionName = "Enemy";
    }

    void SetToAlly()
    {
        gameObject.tag = "Ally";
        FactionName = "Ally";
    }
}
