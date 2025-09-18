using System.Collections;
using System.Linq;
using GlobalEnums;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using UnityEngine;

namespace LostSinner.Behaviours;

/// <summary>
/// Modifies the behavior of the First Sinner boss.
/// </summary>
[RequireComponent(typeof(tk2dSpriteAnimator))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayMakerFSM))]
internal class Sinner : MonoBehaviour {
    private const float GroundY = 13;

    private tk2dSpriteAnimator _anim = null!;
    private Rigidbody2D _body = null!;
    private PlayMakerFSM _control = null!;
    private Transform _heroTransform = null!;

    private void Awake() {
        StartCoroutine(SetupBoss());
    }

    /// <summary>
    /// Set up the modded boss.
    /// </summary>
    private IEnumerator SetupBoss() {
        yield return AssetManager.ManuallyLoadBundles();
        yield return AssetManager.Initialize();

        GetComponents();
        ChangeBlackThreadVoice();
        ChangeTextures();
        ModifyDamage();
        IncreaseHealth();
        RemoveStuns();
        ModifyFsm();
    }

    /// <summary>
    /// Fetch necessary <see cref="Component">components</see> used by this behavior.
    /// </summary>
    private void GetComponents() {
        _anim = GetComponent<tk2dSpriteAnimator>();
        _body = GetComponent<Rigidbody2D>();
        _control = gameObject.LocateMyFSM("Control");
        _heroTransform = HeroController.instance.transform;
    }

    /// <summary>
    /// Distort the boss's voice to be like other void-corrupted enemies.
    /// </summary>
    private void ChangeBlackThreadVoice() {
        var voiceAudio = transform.Find("Audio Loop Voice").GetComponent<AudioSource>();
        var blackThreadMixerGroup =
            voiceAudio.outputAudioMixerGroup.audioMixer.FindMatchingGroups("Actors VoiceBlackThread");
        voiceAudio.outputAudioMixerGroup = blackThreadMixerGroup[0];
    }

    /// <summary>
    /// Change the <see cref="Texture2D">texture</see> atlases of the boss.
    /// </summary>
    private void ChangeTextures() {
        var sprite = GetComponent<tk2dSprite>();
        var cln = sprite.Collection;
        for (int materialIndex = 0; materialIndex < cln.materials.Length; materialIndex++) {
            cln.materials[materialIndex].mainTexture = AssetManager.Get<Texture2D>($"atlas{materialIndex}");            
        }
    }

    /// <summary>
    /// Modify the damage behavior of the boss.
    /// </summary>
    private void ModifyDamage() {
        var damageFlags = DamagePropertyFlags.Void;
        foreach (var damageHero in GetComponentsInChildren<DamageHero>(true)) {
            var multiWounderFsm = damageHero.gameObject.LocateMyFSM("hornet_multi_wounder");
            if (multiWounderFsm) {
                multiWounderFsm.Fsm.GetFsmBool("z3 Force Black Threaded").Value = true;
                continue;
            }

            damageHero.damageDealt = 2;
            damageHero.damagePropertyFlags = damageFlags;
        }
    }

    /// <summary>
    /// Raise the boss's <see cref="HealthManager">health</see>.
    /// </summary>
    private void IncreaseHealth() {
        var health = GetComponent<HealthManager>();
        #if DEBUG
        health.hp = 100;
        #endif
        health.hp = 2000;
    }

    /// <summary>
    /// Remove the boss's ability to be stunned.
    /// </summary>
    private void RemoveStuns() {
        Destroy(gameObject.LocateMyFSM("Stun Control"));
    }

    /// <summary>
    /// Update the boss's <see cref="PlayMakerFSM">state machine</see>.
    /// </summary>
    private void ModifyFsm() {
        AddAbyssTendrilsToCharge();
        AddVomitGlobAttack();
        MakeFacingSecondSlash();
        ShortenBindTime();
        ModifyPhase2();
    }

    /// <summary>
    /// Spawn abyss tendrils while the boss is performing a charging slice.
    /// </summary>
    private void AddAbyssTendrilsToCharge() {
        var groundTendrilPrefab = AssetManager.Get<GameObject>("Lost Lace Ground Tendril");
        if (!groundTendrilPrefab) {
            return;
        }

        var sinnerStates = _control.FsmStates;
        foreach (var sinnerState in sinnerStates) {
            if (sinnerState.Name == "Slice Charge") {
                var chargeActions = sinnerState.Actions.ToList();
                chargeActions.Insert(0, new SpawnObjectFromGlobalPoolOverTime {
                    gameObject = groundTendrilPrefab,
                    spawnPoint = gameObject,
                    position = Vector3.up * (GroundY - 11.2f),
                    rotation = Vector3.one,
                    frequency = 0.25f
                });
                sinnerState.Actions = chargeActions.ToArray();
            }
        }
    }

    /// <summary>
    /// Add a new attack where the boss spawns abyss vomit globs.
    /// </summary>
    private void AddVomitGlobAttack() {
        var vomitRoutineState = new FsmState(_control.Fsm);
        vomitRoutineState.Name = "Vomit Routine";
        vomitRoutineState.Actions = [
            new InvokeCoroutine(VomitGlobAttack, true)
        ];

        _control.Fsm.States = _control.FsmStates.Append(vomitRoutineState).ToArray();

        var vomitEvent = new FsmEvent("VOMIT");
        var vomitTransition = new FsmTransition {
            ToFsmState = vomitRoutineState,
            ToState = vomitRoutineState.Name,
            FsmEvent = vomitEvent,
        };

        var idleState = _control.FsmStates.First(state => state.Name == "Idle");
        var vomitToIdleTransition = new FsmTransition {
            ToFsmState = idleState,
            ToState = "Idle",
            FsmEvent = FsmEvent.Finished,
        };

        vomitRoutineState.Transitions = [vomitToIdleTransition];

        var vomitTracker = new FsmInt("Ct Vomit");
        vomitTracker.Value = 0;
        var vomitMax = new FsmInt("Vomit Max");
        vomitMax.Value = 2;
        var vomitMissed = new FsmInt("Ms Vomit");
        vomitMissed.Value = 0;
        var vomitMissedMax = new FsmInt("Ms Vomit");
        vomitMissedMax.Value = 3;
        _control.FsmVariables.IntVariables = _control.FsmVariables.IntVariables.Append(vomitTracker).ToArray();
        foreach (var sinnerState in _control.FsmStates) {
            if (sinnerState.Name is "P1 Early" or "P1" or "P2" or "No Tele Event" or "Tele Event") {
                sinnerState.Transitions = sinnerState.Transitions.Append(vomitTransition).ToArray();
                int actionIndex = 0;
                if (sinnerState.Name is "P1 Early" or "P1") {
                    actionIndex = 1;
                }

                if (sinnerState.Actions[actionIndex] is SendRandomEventV3 randomAttack) {
                    randomAttack.events = randomAttack.events.Append(vomitEvent).ToArray();
                    randomAttack.weights = randomAttack.weights.Append(0.5f).ToArray();
                    randomAttack.trackingInts = randomAttack.trackingInts.Append(vomitTracker).ToArray();
                    randomAttack.eventMax = randomAttack.eventMax.Append(vomitMax).ToArray();
                    randomAttack.trackingIntsMissed = randomAttack.trackingIntsMissed.Append(vomitMissed).ToArray();
                    randomAttack.missedMax = randomAttack.missedMax.Append(vomitMissedMax).ToArray();
                }
            }
        }
    }

    /// <summary>
    /// Make the second slash of the double slash attack face the player.
    /// </summary>
    private void MakeFacingSecondSlash() {
        var slash3State = _control.FsmStates.FirstOrDefault(state => state.Name == "Slash 3");
        if (slash3State != null) {
            var slashActions = slash3State.Actions.ToList();
            slashActions.Insert(0, new InvokeMethod(FaceHero));
            slash3State.Actions = slashActions.ToArray();
        }
    }

    /// <summary>
    /// Shorten the duration of the boss's healing bind.
    /// </summary>
    private void ShortenBindTime() {
        var bindState = _control.FsmStates.FirstOrDefault(state => state.Name == "Bind Silk");
        if (bindState != null) {
            foreach (var action in bindState.Actions) {
                if (action is Wait wait) {
                    wait.time = 1f;
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Make changes when the boss enters phase 2.
    /// </summary>
    private void ModifyPhase2() {
        var p2StartState = _control.FsmStates.FirstOrDefault(state => state.name == "P2 Start");
        if (p2StartState != null) {
            p2StartState.Actions = p2StartState.Actions
                .Append(new InvokeMethod(IncreaseChargeSliceSpeed))
                .Append(new InvokeMethod(SpeedUpPhase2Pins))
                .Append(new InvokeMethod(SlashToCharge))
                .ToArray();
        }
    }

    /// <summary>
    /// Transition from the boss's double slash attack to its charging slice attack.
    /// </summary>
    private void SlashToCharge() {
        FsmState? afterSlashState = null;
        FsmState? sliceChargeAnticState = null;
        foreach (var state in _control.FsmStates) {
            if (state.Name == "After Slash") {
                afterSlashState = state;
            } else if (state.Name == "Slice Charge Antic") {
                sliceChargeAnticState = state;
            }
        }

        if (afterSlashState != null && sliceChargeAnticState != null) {
            afterSlashState.Transitions = new FsmTransition[] {
                new FsmTransition {
                    toFsmState = sliceChargeAnticState,
                    toState = afterSlashState.Name,
                    FsmEvent = FsmEvent.Finished
                }
            };
        }
    }

    /// <summary>
    /// Increase the speed of the charging slice attack.
    /// </summary>
    private void IncreaseChargeSliceSpeed() {
        var sliceChargeState = _control.FsmStates.First(state => state.Name == "Slice Charge");
        var sliceChargeActions = sliceChargeState.Actions;
        if (sliceChargeActions[1] is SetVelocityByScale setVelocity) {
            setVelocity.speed = -12;
        }

        if (sliceChargeActions[2] is AccelerateToXByScale accelerateToX) {
            accelerateToX.accelerationFactor = 0.65f;
            accelerateToX.targetSpeed = 40;
        }

        if (sliceChargeActions[6] is Wait wait) {
            wait.time = 1f;
        }
    }

    /// <summary>
    /// Increase the firing speed of pins in phase 2 of the boss fight. 
    /// </summary>
    private void SpeedUpPhase2Pins() {
        foreach (var pinFsm in FindObjectsByType<PlayMakerFSM>(FindObjectsSortMode.None)
                     .Where(fsm => fsm.name.Contains("FW Pin Projectile"))) {
            var fireState = pinFsm.FsmStates.First(state => state.Name == "Fire");
            var setVelAction = fireState.Actions.First(action => action is SetVelocityAsAngle) as SetVelocityAsAngle;
            setVelAction!.speed = 180;

            var threadPullState = pinFsm.FsmStates.First(state => state.Name == "Thread Pull");
            if (threadPullState.Actions[3] is Wait wait) {
                wait.time = 0.4f;
            }
        }
    }

    /// <summary>
    /// Perform the new abyss vomit glob attack.
    /// </summary>
    private IEnumerator VomitGlobAttack() {
        var abyssGlobPrefab = AssetManager.Get<GameObject>("Abyss Vomit Glob");
        if (abyssGlobPrefab == null) {
            yield break;
        }

        var audioPlayerPrefab = AssetManager.Get<GameObject>("Audio Player Actor Simple");
        if (audioPlayerPrefab == null) {
            yield break;
        }

        var spitClip = AssetManager.Get<AudioClip>("mini_mawlek_spit");
        if (spitClip == null) {
            yield break;
        }

        _anim.Play("Cast");
        FaceHero();
        _body.linearVelocity = new Vector2(0, 15f);

        float riseTime = 0.25f;
        float riseTimer = 0;
        yield return new WaitUntil(() => {
            riseTimer += Time.deltaTime;

            var newVelocityY = _body.linearVelocity.y * 0.85f;
            _body.linearVelocity = new Vector2(_body.linearVelocity.x, newVelocityY);

            return riseTimer >= riseTime;
        });

        _body.linearVelocity = new Vector2(_body.linearVelocity.x, 0);

        int shots = Random.Range(5, 8);
        for (int i = 0; i < shots; i++) {
            FlingUtils.SpawnAndFling(new FlingUtils.Config {
                Prefab = abyssGlobPrefab,
                SpeedMin = 20,
                SpeedMax = 30,
                AngleMin = 45,
                AngleMax = 135,
                AmountMin = 1,
                AmountMax = 1,
            }, transform, Vector3.up * 2);
            var audioPlayer = audioPlayerPrefab.Spawn(transform.position);
            var audioSource = audioPlayer.GetComponent<AudioSource>();
            audioSource.pitch = Random.Range(0.85f, 1.15f);
            audioSource.PlayOneShot(spitClip);
            yield return new WaitForSeconds(0.05f);
        }

        _control.SendEvent("FINISHED");
    }

    /// <summary>
    /// Face the player.
    /// </summary>
    private void FaceHero() {
        if (_heroTransform.position.x > transform.position.x && transform.localScale.x > 0 ||
            _heroTransform.position.x < transform.position.x && transform.localScale.x < 0) {
            Vector3 scale = transform.localScale;
            scale.x *= -1;
            transform.localScale = scale;
        }
    }

    private void OnDestroy() {
        AssetManager.UnloadManualBundles();
    }
}