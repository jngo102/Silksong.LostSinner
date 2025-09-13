using System.Collections;
using System.Linq;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using UnityEngine;
using Random = UnityEngine.Random;

namespace LostSinner;

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

    private bool _stopTendrilSpawn;

    private void Awake() {
        GetComponents();
        ChangeBlackThreadVoice();
        ChangeTextures();
        IncreaseHealth();
        RemoveStuns();
        ChangeFSM();
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
        var blackThreadMixerGroup = voiceAudio.outputAudioMixerGroup.audioMixer.FindMatchingGroups("Actors VoiceBlackThread");
        voiceAudio.outputAudioMixerGroup = blackThreadMixerGroup[0];
    }

    /// <summary>
    /// Change the <see cref="Texture2D">texture</see> atlases of the boss.
    /// </summary>
    private void ChangeTextures() {
        var sprite = GetComponent<tk2dSprite>();
        var cln = sprite.Collection;
        cln.materials[0].mainTexture = Plugin.AtlasTextures[0];
        cln.materials[1].mainTexture = Plugin.AtlasTextures[1];
    }

    /// <summary>
    /// Raise the boss's <see cref="HealthManager">health</see>.
    /// </summary>
    private void IncreaseHealth() {
        var health = GetComponent<HealthManager>();
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
    private void ChangeFSM() {
        AddAbyssTendrilsToCharge();
        AddVomitGlobAttack();
    }

    /// <summary>
    /// Spawn abyss tendrils while the boss is performing a charging slice.
    /// </summary>
    private void AddAbyssTendrilsToCharge() {
        var sinnerStates = _control.FsmStates;
        foreach (var sinnerState in sinnerStates) {
            if (sinnerState.Name == "Slice Charge") {
                var chargeActions = sinnerState.Actions.ToList();
                chargeActions.Insert(0, new InvokeCoroutine(SpawnTendrils, false));
                sinnerState.Actions = chargeActions.ToArray();
            } else if (sinnerState.Name == "Multislash") {
                var multislashActions = sinnerState.Actions.ToList();
                multislashActions.Insert(0, new InvokeMethod(StopTendrilSpawn));
                sinnerState.Actions = multislashActions.ToArray();
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
        vomitMax.Value = 3;
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
    /// Stop spawning abyss tendrils.
    /// </summary>
    private void StopTendrilSpawn() {
        _stopTendrilSpawn = true;
    }

    /// <summary>
    /// Spawn tendrils in intervals.
    /// </summary>
    private IEnumerator SpawnTendrils() {
        if (!AssetManager.TryGet<GameObject>("Lost Lace Ground Tendril", out var groundTendril) ||
            groundTendril == null) {
            yield break;
        }

        float interval = 0.3f;
        for (float time = 0; time < 1.5f; time += interval) {
            if (_stopTendrilSpawn) {
                _stopTendrilSpawn = false;
                break;
            }

            Instantiate(groundTendril, new Vector2(transform.position.x, GroundY), Quaternion.identity);
            yield return new WaitForSeconds(interval);
        }
    }

    /// <summary>
    /// Perform the new abyss vomit glob attack.
    /// </summary>
    private IEnumerator VomitGlobAttack() {
        if (!AssetManager.TryGet<GameObject>("Abyss Vomit Glob", out var abyssGlobPrefab)) {
            yield break;
        }

        if (!AssetManager.TryGet<GameObject>("Audio Player Actor Simple", out var audioPlayerPrefab)) {
            yield break;
        }

        if (!AssetManager.TryGet<AudioClip>("mini_mawlek_spit", out var spitClip)) {
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
        AssetManager.UnloadCustomBundles();
    }
}