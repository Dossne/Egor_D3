using System;
using FarmMergerBattle.Data;
using UnityEngine;

namespace FarmMergerBattle.Runtime
{
    public class EnemyUnit : MonoBehaviour
    {
        public event Action<EnemyUnit> Died;

        public EnemyConfig Config { get; private set; }
        public float PositionToWall => transform.position.x;

        private float _currentHp;
        private float _attackTimer;

        public void Initialize(EnemyConfig config, Sprite fallbackSprite)
        {
            Config = config;
            _currentHp = config.maxHealth;

            var sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = config.enemyVisual != null ? config.enemyVisual : fallbackSprite;
        }

        public void Tick(float deltaTime, float wallX, Action<float> damageWall)
        {
            var distanceToWall = transform.position.x - wallX;
            if (distanceToWall > Config.wallAttackRange)
            {
                transform.position += Vector3.left * (Config.moveSpeed * deltaTime);
                return;
            }

            _attackTimer += deltaTime;
            if (_attackTimer >= 1f / Mathf.Max(0.01f, Config.attackSpeed))
            {
                _attackTimer = 0f;
                damageWall?.Invoke(Config.damage);
            }
        }

        public void TakeDamage(float amount)
        {
            _currentHp -= amount;
            if (_currentHp <= 0f)
            {
                Died?.Invoke(this);
                Destroy(gameObject);
            }
        }
    }
}
