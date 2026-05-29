using System.Collections.Generic;
using UnityEngine;

public class PlayerDashAttack
{
    readonly Player _player;
    readonly float _defaultGravity;
    Actor _dashAttackTarget;
    float _dashAttackFloatTimer;
    float _dashAttackPlowTimer;
    float _dashAttackTimer;
    readonly HashSet<Actor> _dashAttackDamagedActors = new HashSet<Actor>();

    public PlayerDashAttack(Player player)
    {
        _player = player;
        _defaultGravity = player.gravity;
    }

    public void Tick()
    {
        if (_dashAttackTimer > 0f)
        {
            _dashAttackTimer -= Time.deltaTime;
        }

        if (_dashAttackPlowTimer > 0f)
        {
            _dashAttackPlowTimer -= Time.deltaTime;
        }

        if (_dashAttackFloatTimer > 0f)
        {
            _dashAttackFloatTimer -= Time.deltaTime;
            if (_dashAttackTarget != null && IsTargetInRegularAttackRange(_dashAttackTarget))
            {
                FinishDashAttackFloat();
            }
            else if (_dashAttackFloatTimer <= 0f)
            {
                ResetGravity();
            }
        }
        
    }

    public bool TryDashAttack()
    {
        if (_player == null) return false;
        if (_dashAttackTimer > 0f) return false;

        Vector3 direction = _player.mainCamera.transform.forward;
        float finalDashAttackRange = GetDashAttackRange(_player.Motor.Velocity, direction);

        if (!TryGetBestDashAttackTarget(finalDashAttackRange, out Actor enemy)) return false;
        Debug.Log(enemy.transform.position - _player.transform.position);
        
        float distanceToEnemy = Vector3.Distance(
            _player.mainCamera.transform.position,
            GetActorTargetPoint(enemy)
        );

        if (distanceToEnemy <= _player.regularAttackRange)
        {
            ResetGravity();
            return false;
        }
            
        Vector3 dashDirection = (enemy.transform.position - _player.transform.position).normalized;
        _dashAttackTarget = enemy;
        _player.gravity = 0;
        _dashAttackFloatTimer = Mathf.Max(0f, _player.dashAttackFloatTime);
        if (_dashAttackFloatTimer <= 0f)
        {
            ResetGravity();
        }
        _player.Dash(dashDirection, _player.dashSpeed, true);
        _dashAttackTimer = _player.dashAttackDuration;
            
        return true;
    }

    bool IsTargetInRegularAttackRange(Actor target)
    {
        float distanceToTarget = Vector3.Distance(
            _player.mainCamera.transform.position,
            GetActorTargetPoint(target)
        );

        return distanceToTarget <= _player.regularAttackRange;
    }

    void FinishDashAttackFloat()
    {
        if (!_player.IsSliding && !_player.IsJumpHeld)
        {
            _player.RequestDashAttackBrake(_player.dashAttackBrakeFactor);
        }

        ResetGravity();
    }

    void ResetGravity()
    {
        if (!Mathf.Approximately(_player.gravity, _defaultGravity))
        {
            _player.gravity = _defaultGravity;
        }

        _dashAttackFloatTimer = 0f;
        _dashAttackTarget = null;
    }

    public void BeginDashAttack()
    {
        _dashAttackPlowTimer = _player.dashAttackPlowDuration;
        _dashAttackDamagedActors.Clear();
    }

    public bool IsColliderValidForCollisions(Collider coll)
    {
        Actor actor = coll.GetComponentInParent<Actor>();
        if (_dashAttackPlowTimer > 0f && actor != null)
        {
            TryDamageDashAttackActor(actor);
            return false;
        }

        return true;
    }

    void TryDamageDashAttackActor(Actor actor)
    {
        if (_dashAttackTimer <= 0f) return;
        if (!_dashAttackDamagedActors.Add(actor)) return;

        Vector3 ragdollDirection = _player.Motor.Velocity.sqrMagnitude > 0.001f
            ? _player.Motor.Velocity.normalized
            : _player.transform.forward;

        actor.TakeDamage(_player.dashAttackDamage, ragdollDirection);
        _player.PlaySwordHitSound();
    }

    float GetDashAttackRange(Vector3 currentVelocity, Vector3 dashDirection)
    {
        float momentumAlongDash = Vector3.Dot(currentVelocity, dashDirection.normalized);
        momentumAlongDash = Mathf.Max(0f, momentumAlongDash);

        float momentumBonus = Mathf.Clamp(
            momentumAlongDash * _player.dashAttackMomentumScale,
            0f,
            _player.dashAttackMaxMomentumBonus
        );

        float baseDashAttackRange = _player.dashSpeed * _player.dashAttackDuration;
        return baseDashAttackRange + momentumBonus;
    }

    bool TryGetBestDashAttackTarget(float maxWorldRange, out Actor bestEnemy)
    {
        bestEnemy = null;

        Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        float screenRadius = GetDashAttackScreenRadiusPixels();
        float bestScreenDistance = float.MaxValue;

        foreach (Actor enemy in Actor.ActiveActors)
        {
            if (!IsActorVisible(enemy)) continue;
            if (!TryGetActorScreenDistance(enemy, screenCenter, screenRadius, out float screenDistance)) continue;
            if (_player.transform.position.y - enemy.transform.position.y < _player.dashAttackHeightOffset) continue;

            Vector3 targetPoint = GetActorTargetPoint(enemy);
            float worldDistance = Vector3.Distance(_player.mainCamera.transform.position, targetPoint);
            if (worldDistance > maxWorldRange) continue;

            if (screenDistance < bestScreenDistance)
            {
                bestScreenDistance = screenDistance;
                bestEnemy = enemy;
            }
        }

        return bestEnemy != null;
    }

    bool IsActorVisible(Actor actor)
    {
        foreach (Renderer actorRenderer in actor.Renderers)
        {
            if (actorRenderer.isVisible) return true;
        }

        return false;
    }

    bool TryGetActorScreenDistance(Actor actor, Vector2 screenCenter, float screenRadius, out float screenDistance)
    {
        screenDistance = float.MaxValue;
        bool foundScreenBounds = false;

        foreach (Collider actorCollider in actor.Colliders)
        {
            if (!IsInEnemyLayer(actorCollider)) continue;
            if (!TryGetColliderScreenRect(actorCollider, out Rect screenRect)) continue;

            foundScreenBounds = true;
            float distanceToRect = GetDistanceToRect(screenCenter, screenRect);
            screenDistance = Mathf.Min(screenDistance, distanceToRect);
        }

        return foundScreenBounds && screenDistance <= screenRadius;
    }

    bool TryGetColliderScreenRect(Collider actorCollider, out Rect screenRect)
    {
        Bounds bounds = actorCollider.bounds;
        Vector3 min = bounds.min;
        Vector3 max = bounds.max;

        Vector3[] corners =
        {
            new Vector3(min.x, min.y, min.z),
            new Vector3(min.x, min.y, max.z),
            new Vector3(min.x, max.y, min.z),
            new Vector3(min.x, max.y, max.z),
            new Vector3(max.x, min.y, min.z),
            new Vector3(max.x, min.y, max.z),
            new Vector3(max.x, max.y, min.z),
            new Vector3(max.x, max.y, max.z)
        };

        bool foundPointInFront = false;
        Vector2 screenMin = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 screenMax = new Vector2(float.MinValue, float.MinValue);

        foreach (Vector3 corner in corners)
        {
            Vector3 screenPoint = _player.mainCamera.WorldToScreenPoint(corner);
            if (screenPoint.z <= 0f) continue;

            foundPointInFront = true;
            Vector2 guiPoint = new Vector2(screenPoint.x, Screen.height - screenPoint.y);
            screenMin = Vector2.Min(screenMin, guiPoint);
            screenMax = Vector2.Max(screenMax, guiPoint);
        }

        if (!foundPointInFront)
        {
            screenRect = new Rect();
            return false;
        }

        screenRect = Rect.MinMaxRect(screenMin.x, screenMin.y, screenMax.x, screenMax.y);
        return true;
    }

    float GetDistanceToRect(Vector2 point, Rect rect)
    {
        float closestX = Mathf.Clamp(point.x, rect.xMin, rect.xMax);
        float closestY = Mathf.Clamp(point.y, rect.yMin, rect.yMax);
        return Vector2.Distance(point, new Vector2(closestX, closestY));
    }

    float GetDashAttackScreenRadiusPixels()
    {
        return _player.dashAttackScreenRadius * Mathf.Min(Screen.width, Screen.height);
    }

    Vector3 GetActorTargetPoint(Actor actor)
    {
        return actor.Colliders.Length > 0 ? actor.Colliders[0].bounds.center : actor.transform.position;
    }

    bool IsInEnemyLayer(Collider actorCollider)
    {
        return _player.enemyLayerMask.value == 0 || (_player.enemyLayerMask.value & (1 << actorCollider.gameObject.layer)) != 0;
    }

    public void DrawDebugGUI()
    {
        if (_player == null) return;

        //DrawScreenSpaceTargetRadius();
        
        DrawSelectedDashTarget();
    }

    void DrawScreenSpaceTargetRadius()
    {
        Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        float radius = GetDashAttackScreenRadiusPixels();
        DrawScreenCircle(center, radius, Color.red);
    }

    void DrawSelectedDashTarget()
    {
        if (_player.mainCamera == null || _player.Motor == null) return;

        float finalDashAttackRange = GetDashAttackRange(_player.Motor.Velocity, _player.mainCamera.transform.forward);
        if (!TryGetBestDashAttackTarget(finalDashAttackRange, out Actor target)) return;
        if (!TryGetActorScreenRect(target, out Rect targetRect)) return;
        if (IsTargetInRegularAttackRange(target)) return;

        Vector2 center = targetRect.center;
        float radius = Mathf.Max(targetRect.width, targetRect.height) * 0.5f;
        DrawScreenCircle(center, radius, Color.green);
    }

    bool TryGetActorScreenRect(Actor actor, out Rect screenRect)
    {
        bool foundScreenBounds = false;
        Vector2 screenMin = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 screenMax = new Vector2(float.MinValue, float.MinValue);

        foreach (Collider actorCollider in actor.Colliders)
        {
            if (!IsInEnemyLayer(actorCollider)) continue;
            if (!TryGetColliderScreenRect(actorCollider, out Rect colliderRect)) continue;

            foundScreenBounds = true;
            screenMin = Vector2.Min(screenMin, colliderRect.min);
            screenMax = Vector2.Max(screenMax, colliderRect.max);
        }

        if (!foundScreenBounds)
        {
            screenRect = new Rect();
            return false;
        }

        screenRect = Rect.MinMaxRect(screenMin.x, screenMin.y, screenMax.x, screenMax.y);
        return true;
    }

    void DrawScreenCircle(Vector2 center, float radius, Color color)
    {
        const int segments = 64;
        Vector2 previousPoint = center + Vector2.right * radius;

        for (int i = 1; i <= segments; i++)
        {
            float angle = i / (float)segments * Mathf.PI * 2f;
            Vector2 nextPoint = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            DrawScreenLine(previousPoint, nextPoint, color, 2f);
            previousPoint = nextPoint;
        }
    }

    void DrawScreenLine(Vector2 start, Vector2 end, Color color, float thickness)
    {
        Color oldColor = GUI.color;
        Matrix4x4 oldMatrix = GUI.matrix;

        Vector2 delta = end - start;
        float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
        GUI.color = color;
        GUIUtility.RotateAroundPivot(angle, start);
        GUI.DrawTexture(new Rect(start.x, start.y - thickness * 0.5f, delta.magnitude, thickness), Texture2D.whiteTexture);

        GUI.matrix = oldMatrix;
        GUI.color = oldColor;
    }
}
