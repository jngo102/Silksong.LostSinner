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
            var extraPin = Instantiate(transform.GetChild(4).gameObject, transform);
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

        foreach (var state in patternCtrl.FsmStates) {
            switch (state.name) {
                case "Rain 1": {
                    foreach (var action in state.Actions) {
                        if (action is SetIntValue setInt) {
                            setInt.intValue = 8;
                            break;
                        }
                    }

                    break;
                }
                case "Rain 2": {
                    foreach (var action in state.Actions) {
                        if (action is FloatAdd rain2FloatAdd) {
                            rain2FloatAdd.add = 3;
                            break;
                        }
                    }

                    break;
                }
                case "Claw L":
                    var clawLActions = state.Actions.ToList();
                    // if (clawLActions[3] is FloatOperator clawLfloatOp1) {
                    //     clawLfloatOp1.float1 = 12;   
                    // }
                    //
                    // if (clawLActions[10] is FloatOperator clawLfloatOp2) {
                    //     clawLfloatOp2.float1 = 16;
                    // }
                    //
                    // if (clawLActions[17] is FloatOperator clawLfloatOp3) {
                    //     clawLfloatOp3.float1 = 24;
                    // }

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
                case "Claw R": {
                    var clawRActions = state.Actions.ToList();
                    // if (clawRActions[3] is FloatOperator clawRfloatOp1) {
                    //     clawRfloatOp1.float1 = 12;   
                    // }
                    //
                    // if (clawRActions[10] is FloatOperator clawRfloatOp2) {
                    //     clawRfloatOp2.float1 = 16;
                    // }
                    //
                    // if (clawRActions[17] is FloatOperator clawRfloatOp3) {
                    //     clawRfloatOp3.float1 = 24;
                    // }

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
                case "Sweep Dir":
                    var sweepLState = patternCtrl.FsmStates.First(s => s.Name == "Sweep L");
                    var sweepRState = patternCtrl.FsmStates.First(s => s.Name == "Sweep R");

                    var transitionL = state.Transitions.First(transition => transition.EventName == "L");
                    transitionL.toFsmState = sweepRState;
                    transitionL.toState = sweepRState.Name;

                    var sweepRTransition = sweepRState.Transitions.First(transition => transition.FsmEvent == FsmEvent.Finished);
                    sweepRTransition.toFsmState = sweepLState;
                    sweepRTransition.toState = sweepLState.Name;
                    
                    break;
                case "Pincer":
                    var actions = state.Actions.ToList();

                    // if (actions[3] is FloatAdd pincerFloatAdd1) {
                    //     pincerFloatAdd1.add = 15;
                    // }

                    if (actions[4] is RandomFloat pincerRandomFloat1) {
                        pincerRandomFloat1.min = -122;
                        pincerRandomFloat1.max = -118;
                    }

                    // if (actions[11] is FloatAdd pincerFloatAdd2) {
                    //     pincerFloatAdd2.add = -15;
                    // }

                    if (actions[12] is RandomFloat pincerRandomFloat2) {
                        pincerRandomFloat2.min = -62;
                        pincerRandomFloat2.max = -58;
                    }

                    var pincerExtraPinOffsetsX = new float[] { -15, 15 };
                    var pincerExtraPinRotations = new float[] { -30, -150 };

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