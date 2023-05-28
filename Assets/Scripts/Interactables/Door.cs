using System;
using UnityEngine;

[System.Serializable]
public class Door : ItemOpen, IInteractable {

    public event EventHandler<OnDoorOpenEventArgs> OnDoorOpen;
    public class OnDoorOpenEventArgs {
        public bool isOpen;
        public Transform jumpscareTransform;
    }

    [Header("--- SFX ---")]
    [SerializeField] private AudioClip doorOpenSFX;
    [SerializeField] private AudioClip doorCloseSFX;

    [Header("--- Values ---")]
    [SerializeField] private bool needsKey = false;
    [HideInInspector]
    [SerializeField] private GameObject keyObject;

    [SerializeField] private bool fireEvent = false;
    [HideInInspector]
    [SerializeField] private Transform jumpscarePosition;

    private AudioSource audioSource;
    private PlayerInventory playerInventory;

    #region Custom Editor:
    public bool GetNeedsKey() => needsKey;
    public GameObject GetKeyObject() => keyObject;
    public void SetKeyObject(GameObject value) => keyObject = value;

    public bool GetFireEvent() => fireEvent;
    public Transform GetJumpscareTransform() => jumpscarePosition;
    public void SetJumpscarePosition(Transform value) => jumpscarePosition = value;
    #endregion

    private void Awake() {

        playerInventory = FindObjectOfType<PlayerInventory>();
        audioSource = GetComponentInChildren<AudioSource>();
        defaultPosition = transform.eulerAngles;
        enabled = false;
    }

    private void Update() {

        transform.localRotation = SlerpRotation();
        enabled = DisableUpdateMethod();
    }

    public override void Interact() {

        if (needsKey && !playerInventory.HasItem(keyObject)) {
            return;
        }
        if (enabled) {
            return;
        }

        animationPoints = 0;
        currentPosition = transform.eulerAngles;
        targetPosition = isOpen ? defaultPosition : NewRotation();

        AudioClip sfx = isOpen ? doorCloseSFX : doorOpenSFX;
        audioSource.pitch = UnityEngine.Random.Range(0.9f, 1.1f);
        audioSource.PlayOneShot(sfx, 0.65f);
        
        isOpen = !isOpen;

        if (fireEvent) {
            OnDoorOpen?.Invoke(this, new OnDoorOpenEventArgs {
                isOpen = isOpen,
                jumpscareTransform = jumpscarePosition
            });
        }

        enabled = true;
    }

    private Quaternion SlerpRotation() {

        Quaternion current = Quaternion.Euler(currentPosition);
        Quaternion target = Quaternion.Euler(targetPosition);
        animationPoints += openSpeed * Time.deltaTime;

        return Quaternion.Slerp(current, target, animCurve.Evaluate(animationPoints));
    }

    private float DotProduct() {

        Vector3 doorForward = transform.forward;
        Vector3 playerForward = Camera.main.transform.forward;

        return Vector3.Dot(doorForward, playerForward);
    }

    private Vector3 NewRotation() {

        // Open away from player
        if (DotProduct() > 0) { // both look at the same direction

            return defaultPosition + offsetAmount;
        }
        else {  // both look at the opposite direction

            return defaultPosition - offsetAmount;
        }
    }
}
