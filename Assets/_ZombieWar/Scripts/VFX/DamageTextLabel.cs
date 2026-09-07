using TMPro;
using UnityEngine;
using ZombieWar.Enemies;
using ZombieWar.Utils;

namespace ZombieWar.VFX
{
    // One floating number. It holds nothing but its own state; DamageTextManager moves, scales
    // and fades every label from a single Update so a screen full of numbers costs one loop.
    public sealed class DamageTextLabel : MonoBehaviour, IPoolable
    {
        private const string LogPrefix = "[VFX]";

        [SerializeField] private TMP_Text _text;

        private Transform _transform;
        private Color _baseColor;
        private float _amount;
        private float _alpha;

        public ZombieController Owner { get; private set; }
        public Vector3 Anchor { get; private set; }
        public float Age { get; set; }
        public float Amount => _amount;

        private void Awake()
        {
            _transform = transform;
            if (_text == null)
            {
                Debug.LogError($"{LogPrefix} {name} has no text assigned.", this);
            }
        }

        public void Show(ZombieController owner, float amount, Vector3 anchor)
        {
            Owner = owner;
            Anchor = anchor;
            Age = 0f;
            _amount = amount;
            Redraw();
        }

        public void Add(float amount)
        {
            _amount += amount;
            Age = 0f;
            Redraw();
        }

        public void SetStyle(float fontSize, Color color)
        {
            _text.fontSize = fontSize;
            _baseColor = color;
            _alpha = color.a;
            _text.color = color;
        }

        public void SetPosition(Vector3 position) => _transform.position = position;

        public void SetScale(float scale) => _transform.localScale = new Vector3(scale, scale, scale);

        public void SetAlpha(float alpha)
        {
            if (Mathf.Approximately(alpha, _alpha))
            {
                return;
            }

            _alpha = alpha;
            Color color = _baseColor;
            color.a = alpha;
            _text.color = color;
        }

        public void OnSpawned() { }

        public void OnDespawned()
        {
            Owner = null;
            Age = 0f;
            _amount = 0f;
        }

        private void Redraw() => _text.SetText("{0}", Mathf.Round(_amount));
    }
}
