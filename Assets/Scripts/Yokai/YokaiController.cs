using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class YokaiController : MonoBehaviour {

    #region Variables:
    [Header("--- Movement ---")]
    [SerializeField] private float rangeToKillPlayer;
    [SerializeField] private float walkSpeed;
    [SerializeField] private float runSpeed;

    [Header("--- Items ---")]
    [SerializeField] private GameObject[] equipableItems;
    [SerializeField] private float throwForce;

    [Header("--- Chase ---")]
    [Tooltip("The minimum distance from player the Yokai can spawn")]
    [SerializeField] private float chaseMinSpawnDistance;
    [Tooltip("For how long should the Yokai chase before it despawns")]
    [SerializeField] private float chaseTime;
    [Tooltip("How long is the player allowed to stay in the same room as the Yokai, before the Yokai starts chasing him")]
    [SerializeField] private float sameRoomChaseTime;
    [Tooltip("How long should the Yokai stay in the room it spawned, when the player has left the room")]
    [SerializeField] private float despawnFromRoomTime;

    [Header("--- Chance To Spawn On Room Enter ---")]
    [SerializeField, Range(0, 100)] private int chanceToSpawn;
    [SerializeField] private List<Transform> spawnsFloor_1;
    [SerializeField] private List<Transform> spawnsFloor_2;

    private YokaiBehaviour behaviour;
    private GameObject roomYokaiIsIn;
    private float chaseTimer = 0;
    private float sameRoomChaseTimer = 0;
    private float despawnFromRoomTimer = 0;
    private bool chasePlayer = false;
    private bool isChasing = false;
    public bool GetIsChasing() => isChasing;
    #endregion

    private void Awake() {

        behaviour = FindObjectOfType<YokaiBehaviour>();
    }

    private void Start() {

        Door[] doors = FindObjectsOfType<Door>();

        foreach (var door in doors) {
            if (door.GetFireEvent()) {
                door.OnDoorOpen += Door_OnDoorOpen;
            }
        }
        YokaiObserver.Instance.OnRunEventChase += Observer_OnRunEventChase;
        YokaiObserver.Instance.OnValidRoomEnter += Observer_OnValidRoomEnter;
    }

    private void Update() {

        if (chasePlayer) {

            isChasing = true;
            chaseTimer += Time.deltaTime;
            behaviour.ChasePlayer(rangeToKillPlayer);

            if (chaseTimer > chaseTime) {

                chasePlayer = false;
                isChasing = false;
                chaseTimer = 0;

                behaviour.DespawnCharacter();
            }
        }

        if (!isChasing) {

            if (YokaiObserver.Instance.GetRoomPlayerIsIn() == roomYokaiIsIn) {

                sameRoomChaseTimer += Time.deltaTime;
                despawnFromRoomTimer = 0;

                if (sameRoomChaseTimer >= sameRoomChaseTime) {

                    roomYokaiIsIn = null;
                    sameRoomChaseTimer = 0;
                    chasePlayer = true;
                }
            }
            else if (YokaiObserver.Instance.GetRoomPlayerIsIn() != roomYokaiIsIn && roomYokaiIsIn != null) {

                despawnFromRoomTimer += Time.deltaTime;
                sameRoomChaseTimer = 0;

                if (despawnFromRoomTimer >= despawnFromRoomTime) {

                    roomYokaiIsIn = null;
                    despawnFromRoomTimer = 0;
                    behaviour.DespawnCharacter();
                }
            }
        }
    }

    private void Observer_OnValidRoomEnter(object sender, YokaiObserver.OnValidRoomEnterEventArgs e) {

        if (isChasing || behaviour.IsActing()) {
            return;
        }

        int randomNumber = Random.Range(0, 101);

        if (randomNumber >= 0 && randomNumber <= chanceToSpawn) {

            behaviour.DespawnCharacter(); // For safery, make sure that the character is not in then scene before calling SpawnAtPosition()
            roomYokaiIsIn = e.room;
            List<Transform> validSpawns = YokaiBrain.GetValidSpawnPositions();
            int randomIndex = Random.Range(0, validSpawns.Count);
            behaviour.SpawnAtPosition(validSpawns[randomIndex]);
        }
    }

    private void Observer_OnRunEventChase(object sender, System.EventArgs e) {

        int floorIndex = YokaiObserver.Instance.PlayerFloorIndex();
        Transform playerTransform = YokaiObserver.Instance.GetPlayerTransform();
        Vector3 playerPosition = playerTransform.position;

        Transform spawnPosition;

        if (behaviour.IsActing()) {

            chasePlayer = true;
            return;
        }

        List<Transform> spawns = new();

        switch (floorIndex) {

            case 1:
            spawns = spawnsFloor_1;
            break;
            case 2:
            spawns = spawnsFloor_2;
            break;
        }

        spawnPosition = YokaiBrain.SelectPosition(spawns, chaseMinSpawnDistance, playerPosition);

        behaviour.SpawnAtPosition(spawnPosition);
        chasePlayer = true;
    }

    private void Door_OnDoorOpen(object sender, Door.OnDoorOpenEventArgs e) {

        if (e.isOpen) {

            behaviour.SpawnAtPosition(e.jumpscareTransform);
            behaviour.RunTowardsPosition(e.jumpscareTransform.position);
        }
    }
}
