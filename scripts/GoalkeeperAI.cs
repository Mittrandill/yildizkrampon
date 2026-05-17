using Godot;

/// res://scripts/GoalkeeperAI.cs
/// Kaleci YZ — açı bisektörü pozisyonlama, şut dalışı, top dağıtımı.
public partial class GoalkeeperAI : CharacterBody2D
{
    public enum Team { Red, Blue }
    [Export] public Team GKTeam = Team.Red;

    // Kalecinin gol direği önündeki x pozisyonu
    private float _goalX;
    private float _goalCenterY = (FieldPlayer.GOAL_TOP + FieldPlayer.GOAL_BOT) / 2f;

    // Dalış durumu
    private bool  _isDiving   = false;
    private Vector2 _diveTarget = Vector2.Zero;
    private float _diveTimer  = 0f;
    private float _cooldown   = 0f;

    // Normal pozisyon limitleri
    private float _minX, _maxX;
    private const float GK_SPEED  = 145f;
    private const float DIVE_SPEED = 400f;
    private const float GK_DEPTH  = 55f; // gol çizgisinden ne kadar ileride durur

    public override void _Ready()
    {
        if (HasMeta("team")) GKTeam = ((string)GetMeta("team")) == "Red" ? Team.Red : Team.Blue;

        if (GKTeam == Team.Red)
        {
            _goalX = GK_DEPTH;
            _minX  = 15f;
            _maxX  = GK_DEPTH + 40f;
        }
        else
        {
            _goalX = FieldPlayer.PITCH_W - GK_DEPTH;
            _minX  = FieldPlayer.PITCH_W - GK_DEPTH - 40f;
            _maxX  = FieldPlayer.PITCH_W - 15f;
        }

        CollisionLayer = GKTeam == Team.Red ? 1u : 2u;
        CollisionMask  = 1 | 2 | 16;
        AddToGroup(GKTeam == Team.Red ? "team_red" : "team_blue");
        AddToGroup("goalkeepers");
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_cooldown > 0f) _cooldown -= (float)delta;

        var ball = Football.Instance;
        if (ball == null) { MoveAndSlide(); return; }

        if (_isDiving)
        {
            _ProcessDive(delta);
            return;
        }

        // Topu kalecinin yakınında kap
        if (_cooldown <= 0f && !ball.IsControlled)
        {
            float dist = GlobalPosition.DistanceTo(ball.GlobalPosition);
            if (dist < 36f)
            {
                // Topu al, dağıt
                _Distribute(ball);
                return;
            }
        }

        // Şutu tespit et → dalış
        if (!ball.IsControlled && _ShouldDive(ball))
        {
            _StartDive(ball);
            return;
        }

        // Normal pozisyonlama
        Vector2 target = _GetPositioningTarget(ball);
        Vector2 dir    = (target - GlobalPosition);
        float   dist2  = dir.Length();
        if (dist2 > 4f)
        {
            Velocity = dir.Normalized() * GK_SPEED;
        }
        else
        {
            Velocity = Vector2.Zero;
        }
        MoveAndSlide();
    }

    private Vector2 _GetPositioningTarget(Football ball)
    {
        // Açı bisektörü: top ve gol merkezi arasında yay üzerinde dur
        Vector2 goalLine = new Vector2(_goalX, _goalCenterY);
        Vector2 toBall   = (ball.GlobalPosition - goalLine).Normalized();

        // Gol çizgisinden GK_DEPTH kadar içeride, top yönüne göre Y ayarla
        float targetY = Mathf.Clamp(
            _goalCenterY + (ball.GlobalPosition.Y - _goalCenterY) * 0.55f,
            FieldPlayer.GOAL_TOP + 20f,
            FieldPlayer.GOAL_BOT - 20f
        );

        float targetX = Mathf.Clamp(_goalX, _minX, _maxX);
        return new Vector2(targetX, targetY);
    }

    private bool _ShouldDive(Football ball)
    {
        if (_isDiving) return false;
        // Top kalemi tehdit ediyor mu?
        Vector2 vel = ball.LinearVelocity;
        if (vel.Length() < 200f) return false;

        // Top yolunu hesapla — gol çizgisini geçiyor mu?
        float t = _TimeToReachX(ball.GlobalPosition, vel, _goalX);
        if (t < 0f || t > 1.2f) return false; // ulaşamaz

        float arrivalY = ball.GlobalPosition.Y + vel.Y * t;
        return arrivalY >= FieldPlayer.GOAL_TOP - 40f && arrivalY <= FieldPlayer.GOAL_BOT + 40f;
    }

    private void _StartDive(Football ball)
    {
        _isDiving = true;
        _diveTimer = 0.7f;

        Vector2 vel = ball.LinearVelocity;
        float t     = _TimeToReachX(ball.GlobalPosition, vel, _goalX);
        float arrY  = t > 0 ? ball.GlobalPosition.Y + vel.Y * t : _goalCenterY;
        _diveTarget = new Vector2(_goalX, Mathf.Clamp(arrY, FieldPlayer.GOAL_TOP - 20f, FieldPlayer.GOAL_BOT + 20f));
    }

    private void _ProcessDive(double delta)
    {
        _diveTimer -= (float)delta;
        Vector2 dir  = (_diveTarget - GlobalPosition);
        float   dist = dir.Length();

        if (dist > 6f)
        {
            Velocity = dir.Normalized() * DIVE_SPEED;
        }
        else
        {
            Velocity = Vector2.Zero;
        }
        MoveAndSlide();

        // Topu yakaladı mı?
        var ball = Football.Instance;
        if (ball != null && !ball.IsControlled && GlobalPosition.DistanceTo(ball.GlobalPosition) < 40f)
        {
            _Distribute(ball);
            _isDiving = false;
            _diveTimer = 0f;
            return;
        }

        if (_diveTimer <= 0f)
            _isDiving = false;
    }

    private void _Distribute(Football ball)
    {
        // En açık takım arkadaşına at
        FieldPlayer? target = _FindOpenTeammate();
        Vector2 throwDir;
        float   speed;

        if (target != null)
        {
            throwDir = (target.GlobalPosition - GlobalPosition).Normalized();
            speed    = Mathf.Clamp(GlobalPosition.DistanceTo(target.GlobalPosition) * 1.5f, 300f, 600f);
        }
        else
        {
            // Güvenli ata (çıkış yönüne)
            throwDir = GKTeam == Team.Red ? Vector2.Right : Vector2.Left;
            speed    = 350f;
        }

        ball.Kick(throwDir * speed);
        _cooldown = 2.0f;
        _isDiving = false;
    }

    private FieldPlayer? _FindOpenTeammate()
    {
        string group = GKTeam == Team.Red ? "team_red" : "team_blue";
        FieldPlayer? best = null;
        float bestScore  = -9999f;

        foreach (Node n in GetTree().GetNodesInGroup(group))
        {
            if (n is not FieldPlayer fp) continue;

            // Baskı altında değil ve makul mesafede
            float dist = GlobalPosition.DistanceTo(fp.GlobalPosition);
            if (dist < 80f || dist > 700f) continue;

            // İleri yönde mi?
            float forwardness = GKTeam == Team.Red ? fp.GlobalPosition.X : FieldPlayer.PITCH_W - fp.GlobalPosition.X;

            // Baskı az mı?
            string opp = GKTeam == Team.Red ? "team_blue" : "team_red";
            float pressure = 0f;
            foreach (Node on in GetTree().GetNodesInGroup(opp))
            {
                if (on is not FieldPlayer of2) continue;
                float d = fp.GlobalPosition.DistanceTo(of2.GlobalPosition);
                if (d < 100f) pressure += 1f - d / 100f;
            }

            float score = forwardness * 0.5f - pressure * 50f;
            if (score > bestScore) { bestScore = score; best = fp; }
        }
        return best;
    }

    private float _TimeToReachX(Vector2 pos, Vector2 vel, float targetX)
    {
        float dx = targetX - pos.X;
        if (Mathf.Abs(vel.X) < 0.01f) return -1f;
        float t = dx / vel.X;
        return t > 0 ? t : -1f;
    }
}
