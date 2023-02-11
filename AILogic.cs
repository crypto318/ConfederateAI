﻿using UnityEngine;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class AILogic : MonoBehaviour
{
    AICore core;

    public Transform patrolTarget = null;
    public Transform aiTarget = null;
    Vector3 target;
    float distanceToEnemy;
    bool canAttack = true;
    public bool hasRandomMovement = false;
    bool wayPointSelected;
    GameObject SelectedWayPoint;
    public int rotSpeed = 1;
    public float maxDistance = 1;
    Transform myTransform;
    Transform _destination;
    Transform followTarget = null;
    public bool followPath;
    public Waypoint currentWaypoint;
    int direction;

    bool playerSighted = false;
    public AudioClip playerSightedClip;

    void Awake()
    {
        core = GetComponent<AICore>();
    }

    void Start()
    {
        followTarget = GameObject.FindGameObjectWithTag("Player").transform;

        myTransform = transform;
        aiTarget = followTarget.transform;
        canAttack = true;
        direction = Mathf.RoundToInt(Random.Range(0f, 1f));

        if (followPath)
        {
            SortWaypoints();
            SelectedWayPoint = currentWaypoint.gameObject;
            _destination = SelectedWayPoint.transform;
            if (!hasRandomMovement)
            {
                hasRandomMovement = true;
            }
        }
    }

    void Update()
    {
        MainLogic();
    }

    void MainLogic()
    {
        distanceToEnemy = Vector3.Distance(aiTarget.position, myTransform.position);

        if (distanceToEnemy < core.stats.Sight)
        {
            if (canAttack)
            {
                Attack();
            }

            if (aiTarget.GetComponent<PlayerTargetting>() && !playerSighted)
            {
                AudioSource audioSource = GetComponent<AudioSource>();
                audioSource.clip = playerSightedClip;
                audioSource.Play();
                playerSighted = true;
            }
        }
        else
        {
            if (core.follow != null)
            {
                if (core.follow.isFollowing)
                {
                    FollowPlayer();
                }
                else
                {
                    FreeBehaviour();
                }
            }
            else
            {
                FreeBehaviour();
            }

            if (playerSighted)
            {
                playerSighted = false;
            }
        }
    }

    void FreeBehaviour()
    {
        if (hasRandomMovement)
        {
            if (followPath)
            {
                FollowPath();
            }
            else
            {
                MoveAround();
            }
        }
        else
        {
            Guard();
        }
    }

    void MoveAround()
    {
        if (!wayPointSelected)
        {
            GetRandomWaypoint();
        }

        float distance;
        bool atLoc;
        LocDistCheck(out distance, out atLoc);

        if (distance < 1 || atLoc)
        {
            wayPointSelected = false;
        }

        _destination = SelectedWayPoint.transform;
        AssignTarget();
        core.movement.isWalking();
    }

    void FollowPath()
    {
        float distance;
        bool atLoc;
        LocDistCheck(out distance, out atLoc);

        if (distance < 1 || atLoc)
        {
            bool shouldBranch = false;
            if (currentWaypoint.branches != null && currentWaypoint.branches.Count > 0)
            {
                shouldBranch = Random.Range(0f, 1f) <= currentWaypoint.branchRatio ? true : false;
            }

            if (shouldBranch)
            {
                currentWaypoint = currentWaypoint.branches[Random.Range(0, currentWaypoint.branches.Count - 1)];
                SelectedWayPoint = currentWaypoint.gameObject;
            }
            else
            {
                if (direction == 0)
                {
                    if (currentWaypoint.nextWaypoint != null)
                    {
                        currentWaypoint = currentWaypoint.nextWaypoint;
                    }
                    else
                    {
                        currentWaypoint = currentWaypoint.previousWaypoint;
                        direction = 1;
                    }
                }
                else if (direction == 1)
                {
                    if (currentWaypoint.previousWaypoint != null)
                    {
                        currentWaypoint = currentWaypoint.previousWaypoint;
                    }
                    else
                    {
                        currentWaypoint = currentWaypoint.nextWaypoint;
                        direction = 0;
                    }
                }
            }
        }
        SelectedWayPoint = currentWaypoint.gameObject;
        _destination = SelectedWayPoint.transform;
        AssignTarget();
        core.movement.isWalking();
    }

    void SortWaypoints()
    {
        List<Transform> pathWaypoints = core.waypoints.pathWaypoints;

        pathWaypoints.Sort(delegate (Transform t1, Transform t2)
        {
            return Vector3.Distance(t1.position, myTransform.position).CompareTo(Vector3.Distance(t2.position, myTransform.position));
        });

        currentWaypoint = pathWaypoints[0].GetComponent<Waypoint>();
    }

    void GetRandomWaypoint()
    {
        if (core.faction.Faction == 1)
        {
            int index = Random.Range(0, core.waypoints.enemywaypoints.Length);
            SelectedWayPoint = core.waypoints.enemywaypoints[index];
        }
        else
        {
            int index = Random.Range(0, core.waypoints.allyWaypoints.Length);
            SelectedWayPoint = core.waypoints.allyWaypoints[index];
        }

        wayPointSelected = true;
    }

    void Guard()
    {
        if (myTransform.position != new Vector3(core.save.startPos.x, myTransform.position.y, core.save.startPos.z))
        {
            if (core.stats.patrolTimer == 0)
            {
                patrol();
            }
            else
            {
                returnHome();
            }
        }
        else
        {
            if (core.stats.patrolTimer == 0)
            {
                patrol();
            }
            else
            {
                home();
            }
        }
    }

    void home()
    {
        if (myTransform.position != new Vector3(core.save.startPos.x, myTransform.position.y, core.save.startPos.z))
        {
            returnHome();
        }
        else
        {
            transform.rotation = core.save.startRot;
            Idle();
        }
    }

    void returnHome()
    {
        core.navAgent.SetDestination(core.save.startPos);
        core.movement.isWalking();
    }

    void patrol()
    {
        if (patrolTarget != null)
        {
            if (myTransform.position == new Vector3(patrolTarget.position.x, myTransform.position.y, patrolTarget.position.z))
            {
                resetPatrolTimer();
                returnHome();
            }
            else
            {
                _destination = patrolTarget;
                core.movement.isWalking();
                AssignTarget();
            }
        }
        else
        {
            resetPatrolTimer();
        }
    }

    void FollowPlayer()
    {
        float DistanceToLeader = Vector3.Distance(followTarget.position, myTransform.position);

        if (DistanceToLeader > 8f)
        {
            core.movement.isRunning();
            _destination = followTarget;
        }
        else
        {
            if (DistanceToLeader > 5f)
            {
                core.movement.isWalking();
                _destination = followTarget;
            }
            else if (DistanceToLeader < 5f)
            {
                Idle();
            }
        }

        AssignTarget();
    }

    void Attack()
    {
        if (distanceToEnemy <= maxDistance)
        {
            atAttack();
        }
        else
        {
            moveToAttack();
        }
    }

    void moveToAttack()
    {
        _destination = aiTarget;
        AssignTarget();

        if (Vector3.Distance(aiTarget.position, myTransform.position) > 2f)
        {
            core.movement.isRunning();
        }
        else
        {
            core.movement.isWalking();
        }
    }

    void atAttack()
    {
        core.attack.Target = aiTarget;
        rotateToTarget();
        Idle();
    }

    void Idle()
    {
        core.movement.isIdle();
        _destination = transform;
    }

    void resetPatrolTimer()
    {
        core.stats.patrolTimer = core.stats.patrolWaitTime;
    }

    void AssignTarget()
    {
        if (core.navAgent.enabled)
        {
            Vector3 targetVector = _destination.transform.position;
            core.navAgent.SetDestination(targetVector);
        }
    }

    void LocDistCheck(out float distance, out bool atLoc)
    {
        distance = Vector3.Distance(SelectedWayPoint.transform.position, transform.position);
        atLoc = myTransform.position == new Vector3(SelectedWayPoint.transform.position.x, myTransform.position.y, SelectedWayPoint.transform.position.z);
    }

    void rotateToTarget()
    {
        myTransform.rotation = Quaternion.Slerp(myTransform.rotation, Quaternion.LookRotation(aiTarget.position - myTransform.position), rotSpeed * Time.deltaTime);
    }
}
