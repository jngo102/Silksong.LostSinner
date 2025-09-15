using System.Linq;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using UnityEngine;

namespace LostSinner;

internal class PinProjectiles : MonoBehaviour {
    private void Awake() {
        CreateMorePins();
        ModifyFsm();
    }

    /// <summary>
    /// The number of extra pins to create.
    /// </summary>
    private const int AdditionalPins = 10;

    /// <summary>
    /// Create more pins.
    /// </summary>
    private void CreateMorePins() {
        for (int i = 0; i < AdditionalPins; i++) {
            var extraPin = Instantiate(transform.GetChild(0).gameObject, transform);
        }
    }

    /// <summary>
    /// Modify the pin projectiles' pattern control state machine.
    /// </summary>
    private void ModifyFsm() {
        var patternCtrl = gameObject.LocateMyFSM("Pattern Control");
        var fsm = patternCtrl.Fsm;

        var selfOwner = new FsmOwnerDefault {
            ownerOption = OwnerDefaultOption.UseOwner
        };
        var nextPinObj = fsm.GetFsmGameObject("Next Pin");
        var nextPinOwner = new FsmOwnerDefault {
            gameObject = nextPinObj
        };
        var posXFloat = fsm.GetFsmFloat("Pos X");
        var rotationFloat = fsm.GetFsmFloat("Rotation");
        var sceneCentreX = fsm.GetFsmFloat("Scene Centre X");

        foreach (var state in patternCtrl.FsmStates) {
            switch (state.name) {
                // Increase number of pins in the pin rain attack
                case "Rain 1": {
                    foreach (var action in state.Actions) {
                        if (action is SetIntValue setInt) {
                            setInt.intValue = 8;
                            break;
                        }
                    }

                    break;
                }
                // Decrease the spacing between pins in the pin rain attack
                case "Rain 2": {
                    foreach (var action in state.Actions) {
                        if (action is FloatAdd rain2FloatAdd) {
                            rain2FloatAdd.add = 3;
                            break;
                        }
                    }

                    break;
                }
                // Change sweep attack to link only to the Sweep R state
                case "Sweep Dir":
                    state.Actions = new FsmStateAction[] {
                        new SendEventByName {
                            eventTarget = FsmEventTarget.Self,
                            sendEvent = "FINISHED"
                        }
                    };

                    var sweepRState = patternCtrl.FsmStates.FirstOrDefault(s => s.name == "Sweep R");
                    if (sweepRState != null) {
                        state.Transitions = new FsmTransition[] {
                            new FsmTransition {
                                toFsmState = sweepRState,
                                toState = sweepRState.Name,
                                FsmEvent = FsmEvent.Finished
                            }
                        };
                    }

                    break;
                // Link the rightward sweep state directly to the leftward sweep state so both occur during the sweep attack 
                case "Sweep R":
                    var sweepLState = patternCtrl.FsmStates.FirstOrDefault(s => s.name == "Sweep L");
                    if (sweepLState != null) {
                        state.Transitions[0].toFsmState = sweepLState;
                        state.Transitions[0].toState = sweepLState.Name;
                    }

                    if (state.Actions[5] is SetPosition sweepRSetPosition) {
                        sweepRSetPosition.x = sceneCentreX.Value - 15;
                    }

                    break;
                case "Sweep L":
                    if (state.Actions[5] is SetPosition sweepLSetPosition) {
                        sweepLSetPosition.x = sceneCentreX.Value + 15;
                    }

                    break;
                // Add more pins to the rightward claw attack
                case "Claw R": {
                    var clawRActions = state.Actions.ToList();
                    var clawRExtraPinOffsets = new Vector2[] {
                        new Vector2(28, 18.7f),
                        new Vector2(35, 19.7f)
                    };
                    var clawRExtraPinRotations = new float[] { -62, -47 };

                    for (int pinIndex = 0; pinIndex < clawRExtraPinOffsets.Length; pinIndex++) {
                        var offset = clawRExtraPinOffsets[pinIndex];
                        var pinRot = clawRExtraPinRotations[pinIndex];

                        clawRActions.Add(new GetRandomChild {
                            gameObject = selfOwner,
                            storeResult = nextPinObj
                        });
                        clawRActions.Add(new GameObjectIsNull {
                            isNull = FsmEvent.Finished
                        });
                        clawRActions.Add(new FloatOperator {
                            float1 = offset.x,
                            storeResult = posXFloat
                        });
                        clawRActions.Add(new RandomFloat {
                            min = pinRot,
                            max = pinRot,
                            storeResult = rotationFloat
                        });
                        clawRActions.Add(new SetPosition {
                            gameObject = nextPinOwner,
                            x = posXFloat,
                            y = offset.y
                        });
                        clawRActions.Add(new SetRotation {
                            gameObject = nextPinOwner,
                            zAngle = rotationFloat
                        });
                        clawRActions.Add(new SendEventByName {
                            eventTarget = new FsmEventTarget {
                                target = FsmEventTarget.EventTarget.GameObject,
                                gameObject = nextPinOwner
                            },
                            sendEvent = "ATTACK"
                        });
                    }

                    state.Actions = clawRActions.ToArray();

                    break;
                }
                // Add more pins to the leftward claw attack
                case "Claw L":
                    var clawLActions = state.Actions.ToList();
                    var clawLExtraPinOffsets = new Vector2[] {
                        new Vector2(48, 18.7f),
                        new Vector2(40, 19.7f)
                    };
                    var clawLExtraPinRotations = new float[] { 242, 228 };

                    for (int pinIndex = 0; pinIndex < clawLExtraPinOffsets.Length; pinIndex++) {
                        var offset = clawLExtraPinOffsets[pinIndex];
                        var pinRot = clawLExtraPinRotations[pinIndex];

                        clawLActions.Add(new GetRandomChild {
                            gameObject = new FsmOwnerDefault(),
                            storeResult = nextPinObj
                        });
                        clawLActions.Add(new GameObjectIsNull {
                            isNull = FsmEvent.Finished
                        });
                        clawLActions.Add(new FloatOperator {
                            float1 = offset.x,
                            storeResult = posXFloat
                        });
                        clawLActions.Add(new RandomFloat {
                            min = pinRot,
                            max = pinRot,
                            storeResult = rotationFloat
                        });
                        clawLActions.Add(new SetPosition {
                            gameObject = nextPinOwner,
                            x = posXFloat,
                            y = offset.y
                        });
                        clawLActions.Add(new SetRotation {
                            gameObject = nextPinOwner,
                            zAngle = rotationFloat
                        });
                        clawLActions.Add(new SendEventByName {
                            eventTarget = new FsmEventTarget {
                                target = FsmEventTarget.EventTarget.GameObject,
                                gameObject = nextPinOwner
                            },
                            sendEvent = "ATTACK"
                        });
                    }

                    state.Actions = clawLActions.ToArray();

                    break;

                case "Pincer":
                    // Add more pins to the pincer attack
                    var actions = state.Actions.ToList();
                    if (actions[4] is RandomFloat pincerRandomFloat1) {
                        pincerRandomFloat1.min = -122;
                        pincerRandomFloat1.max = -118;
                    }

                    if (actions[12] is RandomFloat pincerRandomFloat2) {
                        pincerRandomFloat2.min = -62;
                        pincerRandomFloat2.max = -58;
                    }

                    var pincerExtraPinOffsetsX = new float[] { -15, 0, 15 };
                    var pincerExtraPinRotations = new float[] { -30, -90, -150 };

                    for (int pinIndex = 0; pinIndex < pincerExtraPinOffsetsX.Length; pinIndex++) {
                        float offsetX = pincerExtraPinOffsetsX[pinIndex];
                        float pinRot = pincerExtraPinRotations[pinIndex];

                        actions.Add(new GetRandomChild {
                            storeResult = nextPinObj
                        });
                        actions.Add(new GameObjectIsNull {
                            gameObject = nextPinObj,
                            isNull = FsmEvent.Finished
                        });
                        actions.Add(new GetPosition {
                            gameObject = selfOwner,
                            x = posXFloat
                        });
                        actions.Add(new FloatAdd {
                            floatVariable = posXFloat,
                            add = offsetX
                        });
                        actions.Add(new RandomFloat {
                            min = pinRot - 2,
                            max = pinRot + 2,
                            storeResult = rotationFloat
                        });
                        actions.Add(new SetPosition {
                            gameObject = nextPinOwner,
                            x = posXFloat,
                            y = 18
                        });
                        actions.Add(new SetRotation {
                            gameObject = nextPinOwner,
                            zAngle = rotationFloat
                        });
                        actions.Add(new SendEventByName {
                            sendEvent = "ATTACK"
                        });
                    }

                    state.Actions = actions.ToArray();
                    break;
            }
        }
    }
}