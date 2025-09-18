// Reference: https://github.com/fifty-six/HollowKnight.Vasi/blob/master/Vasi/InvokeCoroutine.cs

using System;
using System.Collections;
using HutongGames.PlayMaker;

namespace LostSinner;

public class InvokeCoroutine : FsmStateAction {
    private readonly Func<IEnumerator> _coroutine;
    private readonly bool _wait;

    public InvokeCoroutine(Func<IEnumerator> coroutine, bool wait) {
        _coroutine = coroutine;
        _wait = wait;
    }

    private IEnumerator Coroutine() {
        yield return _coroutine.Invoke();

        Finish();
    }

    public override void OnEnter() {
        Fsm.Owner.StartCoroutine(_wait ? Coroutine() : _coroutine.Invoke());

        if (!_wait) Finish();
    }
}