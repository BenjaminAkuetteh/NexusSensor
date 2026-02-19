using UnityEngine;

public class TwoFingerResetGesture : MonoBehaviour
{
    [SerializeField] private AAC_UIControllerV2 aac;
    [SerializeField] private float maxTapDuration = 0.25f;
    [SerializeField] private float maxMovePixels = 25f;

    private float _startTime;
    private Vector2 _p0Start, _p1Start;
    private bool _tracking;

    private void Update()
    {
        if (Input.touchCount == 2 && !_tracking)
        {
            var t0 = Input.GetTouch(0);
            var t1 = Input.GetTouch(1);

            if (t0.phase == TouchPhase.Began || t1.phase == TouchPhase.Began)
            {
                _tracking = true;
                _startTime = Time.unscaledTime;
                _p0Start = t0.position;
                _p1Start = t1.position;
            }
        }

        if (!_tracking) return;

        if (Input.touchCount != 2)
        {
            _tracking = false;
            return;
        }

        var a = Input.GetTouch(0);
        var b = Input.GetTouch(1);

        if (Vector2.Distance(a.position, _p0Start) > maxMovePixels ||
            Vector2.Distance(b.position, _p1Start) > maxMovePixels)
        {
            _tracking = false;
            return;
        }

        bool ended = (a.phase == TouchPhase.Ended || a.phase == TouchPhase.Canceled) &&
                     (b.phase == TouchPhase.Ended || b.phase == TouchPhase.Canceled);

        if (ended)
        {
            float dt = Time.unscaledTime - _startTime;
            if (dt <= maxTapDuration)
            {
                if (aac != null) aac.HardReset();
            }
            _tracking = false;
        }
    }
}
