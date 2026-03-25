using System.Collections.Generic;
using FarmMergerBattle.Data;
using UnityEngine;

namespace FarmMergerBattle.Runtime
{
    public class HeroUnit : MonoBehaviour
    {
        private float _attackTimer;
        private float _damage;
        private float _attackSpeed;
        private float _attackRange;

        public void Initialize(HeroConfig config, Sprite fallbackSprite)
        {
            _damage = config.damage;
            _attackSpeed = config.attackSpeed;
            _attackRange = config.attackRange;

            var sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = config.battleVisual != null ? config.battleVisual : fallbackSprite;
        }

        public void Tick(float deltaTime, List<EnemyUnit> enemies)
        {
            _attackTimer += deltaTime;
            if (_attackTimer < 1f / Mathf.Max(0.01f, _attackSpeed))
            {
                return;
            }

            _attackTimer = 0f;
            EnemyUnit target = null;
            var closest = float.MaxValue;
            foreach (var enemy in enemies)
            {
                if (enemy == null)
                {
                    continue;
                }

                var distance = Mathf.Abs(enemy.transform.position.x - transform.position.x);
                if (distance > _attackRange)
                {
                    continue;
                }

                if (enemy.PositionToWall < closest)
                {
                    closest = enemy.PositionToWall;
                    target = enemy;
                }
            }

            target?.TakeDamage(_damage);
        }

        public void AddDamage(float value)
        {
            _damage += value;
        }

        public void AddAttackSpeed(float value)
        {
            _attackSpeed += value;
        }
    }
}
