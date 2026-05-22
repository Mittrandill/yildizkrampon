using Godot;

public partial class MatchActor : Node2D
{
	public int Team { get; private set; }
	public string Role { get; private set; } = "support";
	public string ActorName { get; private set; } = "Oyuncu";
	public bool IsGoalkeeper { get; private set; }
	public bool Controlled { get; private set; }
	public Vector2 HomePosition { get; private set; }
	public float Stamina { get; private set; } = 100f;
	public float MaxStamina { get; private set; } = 100f;
	public bool WantsPass { get; private set; }
	public bool ShotCharging { get; private set; }
	public float ShotCharge { get; private set; }
	public bool HasCharacterVisual => _anim != null;
	public bool HasNaturalCharacterColors => _anim?.Modulate == Colors.White;
	public bool IsSaving => _saveAnimTimer > 0f;
	public bool IsCelebrating => _celebrationTimer > 0f;
	public bool IsSliding => _slideTimer > 0f;
	public string CurrentVisualAnimation => _anim?.Animation.ToString() ?? "";
	public Vector2 CurrentVisualScale => _anim?.Scale ?? Vector2.Zero;
	public Vector2 CurrentVisualPosition => _anim?.Position ?? Vector2.Zero;
	public bool HasActionTrail => _actionTrail?.Visible == true;
	public Color KitColor => _bodyColor;

	private NeighborhoodMatchController? _controller;
	private MatchBall? _ball;
	private Color _bodyColor = Colors.White;
	private AnimatedSprite2D? _anim;
	private ColorRect? _staminaBar;
	private ColorRect? _passIndicator;
	private ColorRect? _shotBack;
	private ColorRect? _shotFill;
	private ColorRect? _kitPatch;
	private ColorRect? _celebrationMark;
	private Polygon2D? _controlledMarker;
	private Line2D? _actionTrail;
	private Vector2 _lastMoveDirection = Vector2.Right;
	private Vector2 _aiTargetMemory = Vector2.Zero;
	private float _moveSpeed = 94f;
	private float _sprintSpeed = 130f;
	private float _kickCooldown;
	private float _aiDecisionTimer;
	private float _passRequestTimer;
	private float _kickAnimTimer;
	private float _passAnimTimer;
	private float _saveAnimTimer;
	private float _saveCooldown;
	private Vector2 _saveDiveDirection = Vector2.Zero;
	private float _keeperHoldTimer;
	private float _celebrationTimer;
	private float _dribbleTimer;
	private float _headerCooldown;
	private float _headerAnimTimer;
	private float _slideTimer;
	private float _slideCooldown;
	private float _aiTackleThinkTimer;
	private float _dribbleVisualTimer;
	private Vector2 _slideDirection = Vector2.Right;
	private float _moveVisualAmount;
	private bool _sprintingVisual;
	private bool _pressingVisual;

	private const float SlideDuration = 0.42f;

	private int SprintRating => Controlled ? GameManager.Instance.Speed : Role == "forward" ? 38 : 34;
	private int StaminaRating => Controlled ? GameManager.Instance.Stamina : IsGoalkeeper || Role == "defender" ? 40 : 34;
	private int ShootingRating => Controlled ? GameManager.Instance.ShotPower : Role == "forward" ? 39 : Team == 1 ? 35 : 32;
	private int PassingRating => Controlled ? GameManager.Instance.Skill : Team == 1 ? 35 : 33;
	private int DecisionRating => Controlled ? GameManager.Instance.SoccerIq : IsGoalkeeper ? 42 : Team == 1 ? 34 : 33;

	public void Setup(NeighborhoodMatchController controller, MatchBall ball, int team, Vector2 home, Color color, bool goalkeeper, bool human, string role, string displayName)
	{
		_controller = controller;
		_ball = ball;
		Team = team;
		Role = role;
		ActorName = displayName;
		HomePosition = home;
		_aiTargetMemory = home;
		_bodyColor = color;
		IsGoalkeeper = goalkeeper;
		Controlled = human;
		Position = home;

		float condition = Controlled ? ConditionMultiplier() : 1f;
		_moveSpeed = 94f * Mathf.Lerp(0.88f, 1.16f, Mathf.Clamp(StaminaRating / 80f, 0f, 1f)) * condition;
		_sprintSpeed = 130f * Mathf.Lerp(0.88f, 1.18f, Mathf.Clamp(SprintRating / 80f, 0f, 1f)) * condition;
		BuildVisuals();
	}

	public override void _Ready()
	{
		BuildVisuals();
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_controller == null || _ball == null)
			return;

		float dt = (float)delta;
		_kickCooldown = Mathf.Max(_kickCooldown - dt, 0f);
		_kickAnimTimer = Mathf.Max(_kickAnimTimer - dt, 0f);
		_passAnimTimer = Mathf.Max(_passAnimTimer - dt, 0f);
		_saveAnimTimer = Mathf.Max(_saveAnimTimer - dt, 0f);
		_saveCooldown = Mathf.Max(_saveCooldown - dt, 0f);
		_keeperHoldTimer = Mathf.Max(_keeperHoldTimer - dt, 0f);
		_celebrationTimer = Mathf.Max(_celebrationTimer - dt, 0f);
		_dribbleTimer = Mathf.Max(_dribbleTimer - dt, 0f);
		_headerCooldown = Mathf.Max(_headerCooldown - dt, 0f);
		_headerAnimTimer = Mathf.Max(_headerAnimTimer - dt, 0f);
		_slideCooldown = Mathf.Max(_slideCooldown - dt, 0f);
		_aiTackleThinkTimer = Mathf.Max(_aiTackleThinkTimer - dt, 0f);
		_dribbleVisualTimer = Mathf.Max(_dribbleVisualTimer - dt, 0f);
		_aiDecisionTimer = Mathf.Max(_aiDecisionTimer - dt, 0f);
		_passRequestTimer = Mathf.Max(_passRequestTimer - dt, 0f);
		_moveVisualAmount = 0f;
		_sprintingVisual = false;
		_pressingVisual = false;

		if (_controller.IsMatchPaused())
		{
			UpdateIndicators();
			ZIndex = (int)Position.Y;
			return;
		}

		if (_slideTimer > 0f)
		{
			UpdateSlide(dt);
			Position = _controller.ClampActorPosition(Position, Team, IsGoalkeeper);
			_controller.ResolveActorOverlap(this);
			UpdateIndicators();
			ZIndex = (int)Position.Y;
			return;
		}

		if (Controlled)
			UpdateHuman(dt);
		else
			UpdateAi(dt);

		RecoverStamina(dt);
		Position = _controller.ClampActorPosition(Position, Team, IsGoalkeeper);
		_controller.ResolveActorOverlap(this);
		UpdateIndicators();
		ZIndex = (int)Position.Y;
	}

	public bool HasActivePassRequest() => Controlled && _passRequestTimer > 0f;

	private void UpdateHuman(float dt)
	{
		var direction = Input.GetVector("move_left", "move_right", "move_up", "move_down");
		bool sprinting = Input.IsActionPressed("sprint") && Stamina > 8f && direction.LengthSquared() > 0.01f;
		Position += direction * (sprinting ? _sprintSpeed : _moveSpeed) * dt;
		_moveVisualAmount = Mathf.Clamp(direction.Length(), 0f, 1f);
		_sprintingVisual = sprinting;
		if (sprinting)
			UseStamina(9.5f * dt);

		if (direction.LengthSquared() > 0.01f)
		{
			_lastMoveDirection = direction.Normalized();
			if (_ball!.Holder == this)
				UpdateDribbleTouch(_lastMoveDirection, sprinting, dt);
		}
		else if (_ball!.Holder == this)
		{
			_ball.HoldOffset = _lastMoveDirection * 20f;
		}

		TryHeaderBall(Input.IsActionJustPressed("shoot"), Input.IsActionJustPressed("kick_pass"));
		TryClaimBall();

		if (Input.IsActionJustPressed("shoot") && CanReachBall())
		{
			ShotCharging = true;
			ShotCharge = 0f;
		}
		if (ShotCharging && Input.IsActionPressed("shoot"))
		{
			ShotCharge = Mathf.Min(ShotCharge + dt / 0.85f, 1f);
		}
		if (ShotCharging && Input.IsActionJustReleased("shoot"))
		{
			Vector2 aim = HumanAimDirection();
			float power = Mathf.Lerp(310f, 640f, ShotCharge) * RatingFactor(ShootingRating, 0.92f, 1.16f) * StaminaPowerFactor();
			// Hold longer = more aerial: quick tap stays ground, full charge goes max height.
			float liftRatio = Mathf.Lerp(0.06f, 0.55f, ShotCharge);
			TryKick(aim, power, liftRatio);
			ShotCharging = false;
			ShotCharge = 0f;
		}

		if (Input.IsActionJustPressed("kick_pass") && CanReachBall())
			TryHumanPass();

		if (Input.IsActionJustPressed("slide"))
			TryStandingTackle(true);

		if (Input.IsActionJustPressed("request_pass"))
			_passRequestTimer = 2.4f;
	}

	private void UpdateAi(float dt)
	{
		Vector2 rawTarget = _controller!.GetAiTarget(this);
		bool shouldPress = _controller.ShouldActorPress(this);
		_pressingVisual = shouldPress && _ball!.Holder != this;
		if (_ball!.Holder == null && shouldPress)
			rawTarget = _ball.Position;

		WantsPass = _controller.ActorWantsPass(this);

		// Higher blend → faster reaction. Pressing and ball-carrying actors
		// need snappier tracking; off-ball positioning can be smoother.
		float blend = shouldPress       ? 0.42f
		            : _ball.Holder == this ? 0.52f
		            : _ball.Holder != null && _ball.Holder.Team == Team ? 0.28f
		            : 0.16f;
		_aiTargetMemory = _aiTargetMemory.Lerp(rawTarget, blend);
		Vector2 target = IsGoalkeeper ? _controller.GetGoalkeeperTarget(this) : _aiTargetMemory;

		TryHeaderBall(false, false);
		TryClaimBall();
		if (_ball.Holder != this)
			TryStandingTackle(false);

		if (_ball.Holder == this && _aiDecisionTimer <= 0f)
			MakeBallDecision();

		if (_ball.Holder == this)
		{
			KeepPossessionMoving();
			target = _controller.GetAiTarget(this);
			_aiTargetMemory = _aiTargetMemory.Lerp(target, 0.72f);
			target = _aiTargetMemory;
			UpdateDribbleTouch(_lastMoveDirection, _sprintingVisual, dt);
		}

		Vector2 toTarget = target - Position;
		if (toTarget.Length() > 9f)
		{
			Vector2 moveDirection = SteeredDirection(toTarget.Normalized());
			_lastMoveDirection = moveDirection;

			float dist          = toTarget.Length();
			float staminaFactor = Mathf.Lerp(0.84f, 1.04f, Mathf.Clamp(Stamina / 100f, 0f, 1f));

			// ── Sprint conditions ────────────────────────────────────────────
			// 1. Pressing: always sprint toward the ball holder.
			// 2. Loose ball: nearest actor sprints to claim it.
			// 3. Long run (>140 px): sprint into space — makes diagonal runs
			//    and positioning runs actually look dynamic.
			// 4. Ball carrier in clear space: burst of speed on dribble.
			bool pressSprint   = shouldPress && Stamina > 14f;
			bool looseSprint   = _ball.Holder == null && dist > 55f && Stamina > 18f;
			bool longRunSprint = dist > 140f && Stamina > 22f
			                     && (_ball.Holder == null || _ball.Holder.Team == Team);
			bool carrierSprint = _ball.Holder == this && Stamina > 26f
			                     && _controller.IsLaneClearForCarry(Position,
			                         Position + _controller.GetAttackDirection(Team) * 130f, Team);

			bool aiSprint = pressSprint || looseSprint || longRunSprint || carrierSprint;

			float speed = aiSprint
			    ? _sprintSpeed * staminaFactor
			    : _moveSpeed   * staminaFactor;

			if (aiSprint)
				UseStamina((shouldPress ? 5.5f : 3.8f) * dt);

			Position += moveDirection * Mathf.Min(dist, speed * dt);
			_moveVisualAmount = 1f;
			_sprintingVisual  = aiSprint;
		}
	}

	private void MakeBallDecision()
	{
		if (_controller == null || _ball == null)
			return;

		if (IsGoalkeeper)
		{
			if (TryGoalkeeperDistribution())
			{
				_aiDecisionTimer = 0.55f;
				return;
			}

			_aiDecisionTimer = 0.32f;
			return;
		}

		MatchActor? requestedTarget = _controller.GetPassRequestTarget(Team);
		if (requestedTarget != null)
		{
			// Small lead so the ball arrives ahead of the receiver, not underfoot.
			Vector2 reqLead = _controller.GetAttackDirection(Team) * 16f;
			if (TryKick(requestedTarget.Position + reqLead - Position, PassPowerTo(requestedTarget) * RatingFactor(PassingRating, 0.92f, 1.12f), 0.07f, true))
			{
				_controller.ShowRefereeMessage("Pas");
				return;
			}
		}

		MatchActor? passTarget = _controller.FindBestPassTarget(this);
		bool hasThroughPass = _controller.TryFindThroughPass(this, out Vector2 throughTarget, out MatchActor? throughRunner);
		float passScore = _controller.EvaluatePass(this, passTarget);
		float throughScore = hasThroughPass && throughRunner != null ? _controller.EvaluatePass(this, throughRunner) + 0.24f : 0f;
		float shotScore = _controller.EvaluateShot(this) + (ShootingRating - 35f) / 180f + (DecisionRating - 35f) / 220f;
		float carryScore = _controller.EvaluateCarry(this);
		float goalDistance = Position.DistanceTo(new Vector2(_controller.GoalXForTeam(Team), _controller.GoalCenterY()));
		float selfPressure = _controller.GetPressure(this);

		if (selfPressure > 0.68f && passTarget != null && passScore > 0.24f)
		{
			Vector2 pressLead = _controller.GetAttackDirection(Team) * 12f;
			if (TryKick(passTarget.Position + pressLead - Position, PassPowerTo(passTarget) * RatingFactor(PassingRating, 0.94f, 1.12f), 0.10f, true))
			{
				_controller.ShowRefereeMessage("Baski altinda pas");
				_aiDecisionTimer = 0.18f;
				return;
			}
		}

		// Forwards are more trigger-happy; other roles need a cleaner chance.
		float minShotScore = Role == "forward" ? 0.22f : 0.28f;
		if (!IsGoalkeeper && _controller.CanActorShoot(this) && goalDistance < 390f && shotScore > minShotScore && shotScore >= Mathf.Max(passScore - 0.08f, carryScore - 0.12f))
		{
			TryKick(_controller.GetShotDirection(this), 595f * RatingFactor(ShootingRating, 0.94f, 1.18f) * StaminaPowerFactor(), 0.05f);
			_controller.ShowRefereeMessage("Sut");
		}
		else if (hasThroughPass && throughScore > Mathf.Max(passScore, carryScore) && TryKick(throughTarget - Position, PassPowerToPoint(throughTarget) * RatingFactor(PassingRating, 0.92f, 1.13f), selfPressure > 0.55f ? 0.24f : 0.10f, true))
		{
			_controller.ShowRefereeMessage(selfPressure > 0.55f ? "Havadan ara pas" : "Ara pas");
		}
		else if (passTarget != null && passScore > Mathf.Max(0.42f, carryScore + 0.02f))
		{
			float lift = selfPressure > 0.62f || !_controller.IsPassLaneClear(Position, passTarget.Position, Team) ? 0.22f : 0.08f;
			// Lead the receiver — pass to where they're running, not where they stand.
			Vector2 passLead = _controller.GetAttackDirection(Team) * 22f;
			TryKick(passTarget.Position + passLead - Position, PassPowerTo(passTarget) * RatingFactor(PassingRating, 0.9f, 1.12f), lift, true);
		}
		else
		{
			CarryBall();
		}

		_aiDecisionTimer = GD.Randf() * 0.22f + 0.22f;
	}

	private bool TryGoalkeeperDistribution()
	{
		if (_controller == null || _ball == null || _ball.Holder != this)
			return false;

		MatchActor? target = _controller.FindGoalkeeperOutlet(this);
		if (_keeperHoldTimer > 0f && target == null)
			return false;

		if (target != null && _keeperHoldTimer <= 0.18f)
		{
			float distance = Position.DistanceTo(target.Position);
			Vector2 direction = target.Position - Position;
			if (distance < 210f && _ball != null)
			{
				if (direction.LengthSquared() <= 0.01f)
					return false;
				_lastMoveDirection = direction.Normalized();
				_ball.Roll(direction, Mathf.Clamp(distance * 1.14f + 150f, 240f, 430f), this, Team);
				_passAnimTimer = 0.30f;
				_kickCooldown = 0.28f;
				_controller.ShowRefereeMessage("Kaleci elle baslatti");
				return true;
			}

			float lift = distance > 300f ? 0.14f : 0.08f;
			if (TryKick(direction, Mathf.Clamp(distance * 1.18f + 220f, 360f, 560f), lift, true))
			{
				_controller.ShowRefereeMessage("Kaleci ayakla pas verdi");
				return true;
			}
		}

		if (_keeperHoldTimer > 0f)
			return false;

		Vector2 clearDirection = (_controller.GetAttackDirection(Team) + new Vector2(0f, Position.Y < _controller.GoalCenterY() ? 0.22f : -0.22f)).Normalized();
		if (TryKick(clearDirection, 410f, 0.10f, true))
		{
			_controller.ShowRefereeMessage("Kaleci oyunu kurdu");
			return true;
		}
		return false;
	}

	private void CarryBall()
	{
		if (_controller == null || _ball == null)
			return;

		Vector2 carryDirection = _controller.GetAttackDirection(Team);
		Vector2 goalVector = (new Vector2(_controller.GoalXForTeam(Team), _controller.GoalCenterY()) - Position).Normalized();
		carryDirection = carryDirection.Lerp(goalVector, 0.72f).Normalized();
		float centerPull = Mathf.Clamp((Position.Y - _controller.GoalCenterY()) / 210f, -1f, 1f);
		carryDirection = (carryDirection + new Vector2(0f, -centerPull * 0.58f)).Normalized();
		if (Position.Y < _controller.FieldBounds.Position.Y + 96f)
			carryDirection = (carryDirection + Vector2.Down * 0.85f).Normalized();
		else if (Position.Y > _controller.FieldBounds.End.Y - 96f)
			carryDirection = (carryDirection + Vector2.Up * 0.85f).Normalized();

		_ball.HoldOffset = carryDirection * 22f;
		_lastMoveDirection = carryDirection;
		Vector2 next = Position + carryDirection * 124f;
		next.X = Mathf.Clamp(next.X, _controller.FieldBounds.Position.X + 48f, _controller.FieldBounds.End.X - 48f);
		next.Y = Mathf.Clamp(next.Y, _controller.FieldBounds.Position.Y + 58f, _controller.FieldBounds.End.Y - 58f);
		_aiTargetMemory = next;
		UseStamina(0.75f);
	}

	private void KeepPossessionMoving()
	{
		if (_controller == null || _ball == null)
			return;

		Vector2 goalVector = (new Vector2(_controller.GoalXForTeam(Team), _controller.GoalCenterY()) - Position).Normalized();
		Vector2 direction = _controller.GetAttackDirection(Team).Lerp(goalVector, 0.66f).Normalized();
		float centerPull = Mathf.Clamp((Position.Y - _controller.GoalCenterY()) / 210f, -1f, 1f);
		direction = (direction + new Vector2(0f, -centerPull * 0.42f)).Normalized();
		_ball.HoldOffset = direction * 21f;
		_lastMoveDirection = direction;
	}

	public void SetHomePosition(Vector2 value)
	{
		HomePosition = value;
	}

	public void StartGoalCelebration()
	{
		_celebrationTimer = 1.65f;
		_kickAnimTimer = 0f;
		_passAnimTimer = 0f;
		_saveAnimTimer = 0f;
	}

	private void UpdateDribbleTouch(Vector2 direction, bool sprinting, float dt)
	{
		if (_ball == null || _ball.Holder != this || direction.LengthSquared() <= 0.01f)
			return;

		Vector2 forward = direction.Normalized();
		Vector2 side = new Vector2(-forward.Y, forward.X);
		float touchSpacing = sprinting ? 0.17f : 0.24f;
		float reach = sprinting ? 28f : 23f;
		float sideBob = Mathf.Sin(Time.GetTicksMsec() / 1000f * (sprinting ? 16f : 11f)) * 3.2f;
		_ball.HoldOffset = forward * reach + side * sideBob;
		if (_dribbleTimer <= 0f)
		{
			_ball.DribbleNudge(forward, sprinting ? 7.5f : 5.2f);
			_dribbleTimer = touchSpacing;
			_dribbleVisualTimer = 0.16f;
		}
	}

	private bool TryClaimBall()
	{
		if (_controller == null || _ball == null)
			return false;
		if (_ball.Holder != null || _ball.FreeAfterKick > 0f || _ball.IsAirborne())
			return false;

		float speed = _ball.Velocity.Length();
		float controlRadius = speed < 260f ? 31f : speed < 360f ? 27f : 0f;
		controlRadius *= RatingFactor(Controlled ? GameManager.Instance.Skill : PassingRating, 0.9f, 1.18f);
		if (controlRadius > 0f && Position.DistanceTo(_ball.Position) < controlRadius)
		{
			bool claimed = _ball.Claim(this, _controller.GetAttackDirection(Team));
			if (claimed && IsGoalkeeper)
			{
				_keeperHoldTimer = 0.78f;
				_controller.ShowRefereeMessage("Kaleci kontrol etti");
			}
			return claimed;
		}
		return false;
	}

	private bool TryHeaderBall(bool forceShot, bool forcePass)
	{
		if (_controller == null || _ball == null || _headerCooldown > 0f || _ball.Holder != null)
			return false;
		if (_ball.Height < 16f || _ball.Height > 78f || Position.DistanceTo(_ball.Position) > 28f)
			return false;

		bool shouldShoot = forceShot || (!Controlled && _controller.CanActorShoot(this) && Position.DistanceTo(new Vector2(_controller.GoalXForTeam(Team), _controller.GoalCenterY())) < 310f);
		bool shouldPass = forcePass || (!shouldShoot && !IsGoalkeeper);
		Vector2 direction;
		float power;
		float lift;
		if (shouldShoot)
		{
			direction = _controller.GetShotDirection(this);
			power = 455f * RatingFactor(ShootingRating, 0.9f, 1.12f);
			lift = 0.02f;
			_controller.ShowRefereeMessage("Kafa vurusu");
		}
		else if (shouldPass)
		{
			MatchActor? target = _controller.FindBestPassTarget(this);
			direction = target != null ? target.Position - Position : _controller.GetAttackDirection(Team);
			power = target != null ? PassPowerTo(target) * 0.92f : 330f;
			lift = 0.10f;
			_controller.ShowRefereeMessage("Kafayla pas");
		}
		else
		{
			direction = _controller.GetAttackDirection(Team);
			power = 340f;
			lift = 0.06f;
			_controller.ShowRefereeMessage("Hava topu uzaklasti");
		}

		_lastMoveDirection = direction.LengthSquared() > 0.01f ? direction.Normalized() : _lastMoveDirection;
		_ball.Kick(_lastMoveDirection, power, this, Team, lift);
		_headerCooldown = 0.48f;
		_headerAnimTimer = 0.26f;
		UseStamina(2f);
		return true;
	}

	private bool CanReachBall() => _ball != null && (_ball.Holder == this || Position.DistanceTo(_ball.Position) <= 30f);

	private bool TryKick(Vector2 direction, float power, float liftRatio = 0.18f, bool pass = false)
	{
		if (_ball == null || _kickCooldown > 0f || !CanReachBall())
			return false;
		if (direction.LengthSquared() > 0.01f)
			_lastMoveDirection = direction.Normalized();
		_ball.Kick(direction, power, this, Team, liftRatio);
		_kickCooldown = 0.32f;
		if (pass)
			_passAnimTimer = 0.24f;
		else
			_kickAnimTimer = 0.26f;
		UseStamina(3f);
		return true;
	}

	private void TryHumanPass()
	{
		if (_controller == null)
			return;

		Vector2 inputDirection = Input.GetVector("move_left", "move_right", "move_up", "move_down");
		bool lofted = Input.IsActionPressed("sprint");
		float lift = lofted ? 0.26f : 0.08f;
		float powerBonus = lofted ? 42f : 0f;
		if (inputDirection.LengthSquared() > 0.01f)
		{
			if (TryKick(inputDirection.Normalized(), (410f + powerBonus) * RatingFactor(PassingRating, 0.9f, 1.15f), lift, true) && lofted)
				_controller.ShowRefereeMessage("Havadan pas");
			return;
		}

		MatchActor? target = _controller.FindBestPassTarget(this);
		if (target != null)
		{
			if (TryKick(target.Position - Position, (PassPowerTo(target) + powerBonus) * RatingFactor(PassingRating, 0.92f, 1.15f), lift, true) && lofted)
				_controller.ShowRefereeMessage("Havadan pas");
		}
		else if (TryKick(_lastMoveDirection, (390f + powerBonus) * RatingFactor(PassingRating, 0.9f, 1.15f), lift, true) && lofted)
		{
			_controller.ShowRefereeMessage("Havadan pas");
		}
	}

	private float PassPowerTo(MatchActor target)
	{
		float distance = Position.DistanceTo(target.Position);
		return Mathf.Clamp(distance * 1.22f + 245f, 330f, 590f);
	}

	private float PassPowerToPoint(Vector2 target)
	{
		float distance = Position.DistanceTo(target);
		return Mathf.Clamp(distance * 1.16f + 255f, 340f, 610f);
	}

	private void TryStandingTackle(bool forced)
	{
		if (_controller == null || _ball == null || IsGoalkeeper || Stamina < 14f || _slideCooldown > 0f)
			return;
		if (!forced)
		{
			if (_ball.Holder == null || _ball.Holder.Team == Team || !_controller.ShouldActorPress(this))
				return;
			if (_aiTackleThinkTimer > 0f)
				return;
			_aiTackleThinkTimer = 0.48f + GD.Randf() * 0.55f;
			float tackleDistance = Position.DistanceTo(_ball.Holder.Position);
			float roleChance = Role == "defender" ? 0.22f : Role == "support" ? 0.14f : 0.07f;
			if (tackleDistance < 26f || tackleDistance > 52f || Stamina < 32f || GD.Randf() > roleChance)
				return;
		}
		Vector2 direction = Controlled
			? Input.GetVector("move_left", "move_right", "move_up", "move_down")
			: _ball.Holder != null ? _ball.Holder.Position - Position : _lastMoveDirection;
		if (direction.LengthSquared() <= 0.01f)
			direction = _lastMoveDirection.LengthSquared() > 0.01f ? _lastMoveDirection : _controller.GetAttackDirection(Team);
		TryStartSlide(direction.Normalized());
	}

	public bool TryStartSlide(Vector2 direction)
	{
		if (_controller == null || IsGoalkeeper || Stamina < 14f || _slideCooldown > 0f)
			return false;
		_slideDirection = direction.LengthSquared() > 0.01f ? direction.Normalized() : _controller.GetAttackDirection(Team);
		_lastMoveDirection = _slideDirection;
		_slideTimer = SlideDuration;
		_slideCooldown = Controlled ? 1.05f : 2.85f;
		_moveVisualAmount = 1f;
		_sprintingVisual = true;
		UseStamina(11f);
		_controller.ShowRefereeMessage("Kayarak mudahale");
		return true;
	}

	private void UpdateSlide(float dt)
	{
		if (_controller == null || _ball == null)
			return;

		float progress = 1f - _slideTimer / SlideDuration;
		float speed = Mathf.Lerp(282f, 62f, progress);
		Position += _slideDirection * speed * dt;
		_moveVisualAmount = 1f;
		_sprintingVisual = true;
		_slideTimer = Mathf.Max(_slideTimer - dt, 0f);

		float contactRadius = 34f;
		if (_ball.Holder != null && _ball.Holder.Team != Team && Position.DistanceTo(_ball.Holder.Position) <= contactRadius + 6f)
		{
			float chance = 0.52f + Mathf.Clamp(Stamina / 100f, 0f, 1f) * 0.20f;
			if (Controlled)
				chance += 0.28f;
			chance = Mathf.Clamp(chance, 0f, 0.96f);
			if (GD.Randf() <= chance)
			{
				_ball.Release();
				Vector2 pokeDirection = (_slideDirection + _controller.GetAttackDirection(Team) * 0.45f).Normalized();
				_ball.Position = Position + pokeDirection * 20f;
				_ball.FreeAfterKick = 0f;
				_ball.Claim(this, _controller.GetAttackDirection(Team), true);
				_aiDecisionTimer = 0f;
				_slideTimer = Mathf.Min(_slideTimer, 0.12f);
				_controller.ShowRefereeMessage("Kayarak top alma");
			}
			else
			{
				_controller.ShowRefereeMessage("Kayma kacti");
			}
		}

		if (_ball.Holder == null && !_ball.IsAirborne() && Position.DistanceTo(_ball.Position) <= contactRadius)
		{
			_ball.Claim(this, _controller.GetAttackDirection(Team), true);
			_slideTimer = Mathf.Min(_slideTimer, 0.12f);
			_controller.ShowRefereeMessage("Kayarak top kontrolu");
		}
	}

	public bool TryGoalkeeperSave()
	{
		if (!IsGoalkeeper || _controller == null || _ball == null)
			return false;
		if (_saveCooldown > 0f || _ball.Holder != null || _ball.LastTouchTeam == Team || _ball.Velocity.Length() < 250f)
			return false;

		float goalX = _controller.DefenseGoalXForTeam(Team);
		if (Mathf.Abs(_ball.Position.X - goalX) > 96f)
			return false;
		if (_ball.Position.Y < _controller.GoalY.X - 48f || _ball.Position.Y > _controller.GoalY.Y + 48f)
			return false;

		float saveRange = _ball.Height > 44f ? 25f : 34f;
		float reachDistance = Position.DistanceTo(_ball.Position);
		if (reachDistance > saveRange)
			return false;

		Vector2 saveDirection = (_ball.Position - Position).LengthSquared() > 0.01f
			? (_ball.Position - Position).Normalized()
			: -_controller.GetAttackDirection(Team);
		if (Mathf.Abs(_ball.Position.Y - Position.Y) > 8f)
			saveDirection = new Vector2(0f, Mathf.Sign(_ball.Position.Y - Position.Y));
		_lastMoveDirection = saveDirection;
		_saveDiveDirection = saveDirection;
		_saveAnimTimer = 0.44f;
		_saveCooldown = 0.62f;

		float centerOffset = Mathf.Abs(_ball.Position.Y - Position.Y);
		bool centered = centerOffset < 18f;
		float shotSpeed = _ball.Velocity.Length();
		float difficulty =
			Mathf.Clamp(centerOffset / 38f, 0f, 1f) * 0.34f +
			Mathf.Clamp((shotSpeed - 300f) / 420f, 0f, 1f) * 0.28f +
			Mathf.Clamp(_ball.Height / 72f, 0f, 1f) * 0.20f +
			Mathf.Clamp(reachDistance / saveRange, 0f, 1f) * 0.18f;
		float ability = 0.43f + DecisionRating / 190f + (centered ? 0.12f : 0f);
		float saveChance = Mathf.Clamp(0.34f + ability - difficulty, 0.08f, centered ? 0.78f : 0.56f);
		bool controlledParry = shotSpeed < 590f && _ball.Height < 40f && reachDistance < saveRange * 0.96f && difficulty < ability + 0.08f;
		if (!controlledParry && (difficulty > ability + 0.12f || (difficulty > ability - 0.22f && GD.Randf() > saveChance)))
		{
			_controller.ShowRefereeMessage("Kaleci uzandi ama yetisemedi");
			return false;
		}

		bool easyCatch = shotSpeed < 365f && _ball.Height < 20f && centerOffset < 24f && reachDistance < saveRange * 0.72f;
		float catchChance = centered ? 0.62f + DecisionRating / 230f : 0.28f + DecisionRating / 340f;
		catchChance += shotSpeed < 390f ? 0.18f : 0f;
		catchChance -= Mathf.Clamp(_ball.Height / 58f, 0f, 0.28f);
		if ((easyCatch || GD.Randf() < Mathf.Clamp(catchChance, 0.16f, 0.88f)) && shotSpeed < 470f && _ball.Height < 34f)
		{
			_ball.Claim(this, _controller.GetAttackDirection(Team), true);
			_controller.ShowRefereeMessage("Kaleci tuttu");
			_keeperHoldTimer = easyCatch ? 0.88f : 0.62f;
			_aiDecisionTimer = 0.18f;
			return true;
		}

		float side = Mathf.Sign(_ball.Position.Y - _controller.GoalCenterY());
		if (Mathf.Abs(side) < 0.01f)
			side = GD.Randf() < 0.5f ? -1f : 1f;
		Vector2 clearDirection = (_controller.GetAttackDirection(Team) * 0.58f + new Vector2(0f, side * 0.82f)).Normalized();
		float parryPower = Mathf.Clamp(shotSpeed * 0.42f + 92f + DecisionRating * 1.4f, 185f, 355f);
		_ball.Deflect(clearDirection, parryPower, this, _ball.Height > 36f ? 0.08f : 0.03f);
		_controller.ShowRefereeMessage("Kaleci kurtardi");
		_aiDecisionTimer = 0.4f;
		return true;
	}

	private Vector2 HumanAimDirection()
	{
		if (_controller == null)
			return _lastMoveDirection;
		Vector2 aim = Input.GetVector("move_left", "move_right", "move_up", "move_down");
		if (aim.LengthSquared() <= 0.01f)
			aim = _controller.CanActorShoot(this) ? _controller.GetShotDirection(this) : _lastMoveDirection;
		else if (_controller.CanActorShoot(this) && aim.Normalized().Dot(_controller.GetAttackDirection(Team)) > 0.2f)
			aim = aim.Normalized().Lerp(_controller.GetShotDirection(this), 0.58f);
		if (aim.LengthSquared() <= 0.01f)
			aim = _controller.GetAttackDirection(Team);
		return aim.Normalized();
	}

	private Vector2 SteeredDirection(Vector2 baseDirection)
	{
		if (_controller == null)
			return baseDirection;
		Vector2 steer = baseDirection;
		foreach (var other in _controller.Actors)
		{
			if (other == this)
				continue;
			float distance = Position.DistanceTo(other.Position);
			if (distance > 0.01f && distance < 36f)
				steer += (Position - other.Position).Normalized() * ((36f - distance) / 36f) * 0.9f;
		}
		return steer.LengthSquared() <= 0.01f ? baseDirection : steer.Normalized();
	}

	private void UseStamina(float amount)
	{
		Stamina = Mathf.Max(Stamina - amount, 0f);
		MaxStamina = Mathf.Max(MaxStamina - amount * 0.008f, 78f);
	}

	private void RecoverStamina(float dt)
	{
		if (_ball?.Holder == this)
			Stamina = Mathf.Min(Stamina + 3.5f * dt, MaxStamina);
		else
			Stamina = Mathf.Min(Stamina + 8.5f * dt, MaxStamina);
	}

	private float ConditionMultiplier()
	{
		var gm = GameManager.Instance;
		float multiplier = 0.9f;
		multiplier += Mathf.Clamp(gm.Energy / 100f, 0f, 1f) * 0.12f;
		multiplier += Mathf.Clamp(gm.Morale / 100f, 0f, 1f) * 0.06f;
		multiplier -= Mathf.Clamp(gm.Fatigue / 100f, 0f, 1f) * 0.12f;
		return Mathf.Clamp(multiplier, 0.78f, 1.12f);
	}

	private static float RatingFactor(int rating, float low, float high)
	{
		return Mathf.Lerp(low, high, Mathf.Clamp(rating / 80f, 0f, 1f));
	}

	private float StaminaPowerFactor() => Mathf.Lerp(0.86f, 1f, Mathf.Clamp(Stamina / 100f, 0f, 1f));

	private void BuildVisuals()
	{
		if (_anim != null)
			return;

		var shadow = new Sprite2D
		{
			Texture = MakeShadowTexture(),
			Position = new Vector2(0, -1),
			ZIndex = -1
		};
		AddChild(shadow);

		_anim = new AnimatedSprite2D
		{
			Name = "Anim",
			Position = new Vector2(0, -33f),
			Scale = new Vector2(0.30f, 0.30f),
			Modulate = Colors.White,
			ZIndex = 4
		};
		_anim.SpriteFrames = BuildFrames();
		// Use the kit-recolor shader to give each team its own jersey colour.
		// Falls back to the plain white-bg-removal shader if the new one is missing.
		var kitShader = GD.Load<Shader>("res://shaders/kit_color.gdshader");
		if (kitShader != null)
		{
			var mat = new ShaderMaterial { Shader = kitShader };
			mat.SetShaderParameter("kit_color", _bodyColor);
			_anim.Material = mat;
		}
		else
		{
			var bgShader = GD.Load<Shader>("res://shaders/remove_white_bg.gdshader");
			if (bgShader != null) _anim.Material = new ShaderMaterial { Shader = bgShader };
		}
		AddChild(_anim);
		_anim.Play("idle_down");

		_actionTrail = new Line2D
		{
			Name = "ActionTrail",
			Width = 3f,
			DefaultColor = new Color(0.93f, 0.86f, 0.60f, 0.0f),
			ZIndex = 3,
			Visible = false
		};
		AddChild(_actionTrail);

		// Kit patch is no longer needed — the shader handles jersey colouring.

		_celebrationMark = new ColorRect
		{
			Position = new Vector2(-12, -62),
			Size = new Vector2(24, 5),
			Color = new Color(1f, 0.9f, 0.18f, 0.92f),
			Visible = false,
			ZIndex = 8
		};
		AddChild(_celebrationMark);

		_controlledMarker = new Polygon2D
		{
			Position = new Vector2(0, -46),
			Polygon = new[] { new Vector2(0, 0), new Vector2(-7, -9), new Vector2(7, -9) },
			Color = new Color(1f, 0.92f, 0.18f),
			Visible = Controlled
		};
		AddChild(_controlledMarker);

		_passIndicator = new ColorRect
		{
			Position = new Vector2(-7, -47),
			Size = new Vector2(14, 5),
			Color = new Color(0.98f, 0.9f, 0.22f),
			Visible = false
		};
		AddChild(_passIndicator);

		_staminaBar = new ColorRect { Position = new Vector2(-10, 18), Size = new Vector2(20, 3), Color = new Color(0.22f, 0.9f, 0.28f) };
		AddChild(_staminaBar);
		_shotBack = new ColorRect { Position = new Vector2(-14, -54), Size = new Vector2(28, 4), Color = new Color(0.08f, 0.08f, 0.08f, 0.9f), Visible = false };
		AddChild(_shotBack);
		_shotFill = new ColorRect { Position = new Vector2(-13, -53), Size = new Vector2(0, 2), Color = new Color(0.95f, 0.82f, 0.2f), Visible = false };
		AddChild(_shotFill);
	}

	private void UpdateIndicators()
	{
		if (_anim != null)
		{
			Vector2 visualDirection = VisualFacingDirection();
			_anim.FlipH = visualDirection.X < -0.05f;
			string dir = VisualDirectionName(visualDirection);
			string animName = _ball?.Holder == this || VelocityVisualMoving() ? "walk_" + dir : "idle_" + dir;
			if (_anim.Animation != animName)
				_anim.Play(animName);

			float time = Time.GetTicksMsec() / 1000f;
			bool idlePose = _moveVisualAmount <= 0.05f && _kickAnimTimer <= 0f && _passAnimTimer <= 0f && _saveAnimTimer <= 0f && _headerAnimTimer <= 0f && _slideTimer <= 0f;
			float moveRate = _pressingVisual ? 20f : _sprintingVisual ? 18f : 11f;
			float moveHeight = _pressingVisual ? 2.4f : _sprintingVisual ? 2f : 1.2f;
			float bob = _moveVisualAmount > 0.05f ? Mathf.Sin(time * moveRate) * moveHeight : 0f;
			float idleBreath = idlePose ? Mathf.Sin(time * (IsGoalkeeper ? 3.8f : 2.8f) + Team * 1.7f) : 0f;
			float keeperReady = idlePose && IsGoalkeeper ? Mathf.Sin(time * 4.6f + Team * 0.9f) : 0f;
			float dribblePulse = _dribbleVisualTimer > 0f ? Mathf.Sin((_dribbleVisualTimer / 0.16f) * Mathf.Pi) : 0f;
			float actionLean = 0f;
			if (_kickAnimTimer > 0f)
				actionLean = Mathf.Sin((_kickAnimTimer / 0.26f) * Mathf.Pi) * 0.34f;
			else if (_passAnimTimer > 0f)
				actionLean = Mathf.Sin((_passAnimTimer / 0.30f) * Mathf.Pi) * 0.18f;
			else if (_saveAnimTimer > 0f)
				actionLean = 1.18f;
			else if (_headerAnimTimer > 0f)
				actionLean = Mathf.Sin((_headerAnimTimer / 0.26f) * Mathf.Pi) * 0.32f;
			else if (_slideTimer > 0f)
				actionLean = 0.92f;
			else if (_pressingVisual)
				actionLean = 0.10f;
			else if (dribblePulse > 0f)
				actionLean = 0.08f * dribblePulse;
			float celebrationBob = _celebrationTimer > 0f ? Mathf.Sin(Time.GetTicksMsec() / 1000f * 18f) * 4.5f : 0f;
			float saveProgress = _saveAnimTimer > 0f ? 1f - _saveAnimTimer / 0.44f : 0f;
			float diveAmount = _saveAnimTimer > 0f ? Mathf.Sin(saveProgress * Mathf.Pi) : 0f;
			float slideProgress = _slideTimer > 0f ? 1f - _slideTimer / SlideDuration : 0f;
			float slideAmount = _slideTimer > 0f ? Mathf.Sin(Mathf.Clamp(slideProgress, 0f, 1f) * Mathf.Pi) : 0f;
			Vector2 diveOffset = _saveDiveDirection * 42f * diveAmount;
			float airLift = diveAmount * 28f; // keeper rises off the ground at dive peak
			Vector2 slideOffset = _slideDirection * 14f * slideAmount;
			Vector2 headerOffset = _headerAnimTimer > 0f ? new Vector2(0f, -7f * Mathf.Sin((_headerAnimTimer / 0.26f) * Mathf.Pi)) : Vector2.Zero;
			float kickStep = _kickAnimTimer > 0f ? Mathf.Sin((_kickAnimTimer / 0.26f) * Mathf.Pi) * 4f : 0f;
			float passStep = _passAnimTimer > 0f ? Mathf.Sin(Mathf.Clamp(_passAnimTimer / 0.30f, 0f, 1f) * Mathf.Pi) * 3f : 0f;
			float pressureStep = _pressingVisual ? Mathf.Sin(time * 12f) * 1.6f : 0f;
			float saveYOffset = Mathf.Abs(_saveDiveDirection.Y) > 0.35f ? diveOffset.Y * 0.85f : diveOffset.Y * 0.35f;
			_anim.Position = new Vector2(diveOffset.X + slideOffset.X + visualDirection.X * (kickStep + passStep + dribblePulse * 2.3f + pressureStep) + keeperReady * 1.5f, -33f + bob + idleBreath * 0.75f + celebrationBob + saveYOffset + slideOffset.Y * 0.28f + headerOffset.Y + (_saveAnimTimer > 0f ? -airLift : 0f) + (_slideTimer > 0f ? 8f : 0f) + visualDirection.Y * (kickStep + passStep + dribblePulse * 2.1f) * 0.35f);
			if (_saveAnimTimer > 0f)
			{
				// Body tilts along the dive direction and rolls during the arc peak.
				float diveRoll = Mathf.Abs(_saveDiveDirection.X) > 0.1f
					? Mathf.Sign(_saveDiveDirection.X) * diveAmount * 0.58f
					: 0f;
				_anim.Rotation = Mathf.Clamp(_saveDiveDirection.Y, -1f, 1f) * 0.60f
					+ diveRoll
					+ (_anim.FlipH ? -0.16f : 0.16f);
			}
			else if (_slideTimer > 0f)
				_anim.Rotation = Mathf.Clamp(_slideDirection.Y, -1f, 1f) * 0.16f + (_slideDirection.X < 0f ? -0.10f : 0.10f);
			else
				_anim.Rotation = actionLean * (_anim.FlipH ? -1f : 1f) + keeperReady * 0.025f;
			Vector2 baseScale = _saveAnimTimer > 0f ? new Vector2(0.27f, 0.38f) : _slideTimer > 0f ? new Vector2(0.35f, 0.24f) : _kickAnimTimer > 0f ? new Vector2(0.32f, 0.29f) : _passAnimTimer > 0f ? new Vector2(0.31f, 0.30f) : _headerAnimTimer > 0f ? new Vector2(0.31f, 0.33f) : _pressingVisual ? new Vector2(0.315f, 0.292f) : dribblePulse > 0f ? new Vector2(0.305f + dribblePulse * 0.01f, 0.30f - dribblePulse * 0.006f) : new Vector2(0.30f, 0.30f);
			baseScale *= _controller?.VisualScaleForFieldY(Position.Y) ?? 1f;
			_anim.Scale = idlePose ? new Vector2(baseScale.X + idleBreath * 0.004f, baseScale.Y - idleBreath * 0.004f) : baseScale;
			UpdateActionTrail(diveAmount, slideAmount, diveOffset, slideOffset);
		}
		else
		{
			UpdateActionTrail(0f, 0f, Vector2.Zero, Vector2.Zero);
		}

		if (_staminaBar != null)
		{
			_staminaBar.Size = new Vector2(20f * Stamina / 100f, 3f);
			_staminaBar.Color = Stamina < 30f ? new Color(0.92f, 0.22f, 0.18f) : Stamina < 62f ? new Color(0.95f, 0.75f, 0.22f) : new Color(0.22f, 0.9f, 0.28f);
		}
		if (_passIndicator != null)
			_passIndicator.Visible = WantsPass && !Controlled;
		if (_shotBack != null && _shotFill != null)
		{
			_shotBack.Visible = Controlled && ShotCharging;
			_shotFill.Visible = Controlled && ShotCharging;
			_shotFill.Size = new Vector2(26f * ShotCharge, 2f);
		}

		if (_kitPatch != null)
		{
			_kitPatch.Visible = _saveAnimTimer <= 0f && _slideTimer <= 0f;
			_kitPatch.Position = new Vector2(_anim?.FlipH == true ? -7 : -9, -44 + (_moveVisualAmount > 0.05f ? Mathf.Sin(Time.GetTicksMsec() / 1000f * (_pressingVisual ? 18f : 11f)) * 1f : 0f));
		}
		if (_celebrationMark != null)
			_celebrationMark.Visible = _celebrationTimer > 0f;
	}

	private void UpdateActionTrail(float diveAmount, float slideAmount, Vector2 diveOffset, Vector2 slideOffset)
	{
		if (_actionTrail == null)
			return;

		_actionTrail.ClearPoints();
		_actionTrail.Visible = false;

		if (_saveAnimTimer > 0f && diveAmount > 0.04f)
		{
			Vector2 direction = _saveDiveDirection.LengthSquared() > 0.01f ? _saveDiveDirection.Normalized() : Vector2.Right;
			Vector2 side = new(-direction.Y, direction.X);
			Vector2 center = new Vector2(diveOffset.X * 0.55f, -25f + diveOffset.Y * 0.22f);
			_actionTrail.DefaultColor = new Color(0.82f, 0.92f, 1f, 0.30f * diveAmount);
			_actionTrail.Width = 3.2f;
			_actionTrail.AddPoint(center - direction * 30f + side * 5f);
			_actionTrail.AddPoint(center - direction * 12f);
			_actionTrail.AddPoint(center + direction * 5f - side * 3f);
			_actionTrail.Visible = true;
		}
		else if (_slideTimer > 0f && slideAmount > 0.04f)
		{
			Vector2 direction = _slideDirection.LengthSquared() > 0.01f ? _slideDirection.Normalized() : Vector2.Right;
			Vector2 side = new(-direction.Y, direction.X);
			Vector2 center = new Vector2(slideOffset.X * 0.40f, -13f + slideOffset.Y * 0.18f);
			_actionTrail.DefaultColor = new Color(0.78f, 0.67f, 0.42f, 0.34f * slideAmount);
			_actionTrail.Width = 4.2f;
			_actionTrail.AddPoint(center - direction * 25f + side * 6f);
			_actionTrail.AddPoint(center - direction * 9f);
			_actionTrail.AddPoint(center + direction * 4f - side * 4f);
			_actionTrail.Visible = true;
		}
	}

	private bool VelocityVisualMoving() => _moveVisualAmount > 0.05f;

	private Vector2 VisualFacingDirection()
	{
		if (_controller == null)
			return Vector2.Right;
		if (IsGoalkeeper && _saveAnimTimer > 0f && Mathf.Abs(_saveDiveDirection.Y) > 0.35f)
			return _saveDiveDirection.Y < 0f ? Vector2.Up : Vector2.Down;
		if (IsGoalkeeper)
			return _controller.GetAttackDirection(Team);
		if (Mathf.Abs(_lastMoveDirection.Y) > Mathf.Abs(_lastMoveDirection.X) + 0.18f)
			return _lastMoveDirection.Y < 0f ? Vector2.Up : Vector2.Down;
		float side = Mathf.Abs(_lastMoveDirection.X) > 0.05f ? _lastMoveDirection.X : _controller.GetAttackDirection(Team).X;
		return side < 0f ? Vector2.Left : Vector2.Right;
	}

	private static string VisualDirectionName(Vector2 direction)
	{
		if (Mathf.Abs(direction.Y) > Mathf.Abs(direction.X))
			return direction.Y < 0f ? "up" : "down";
		return "right";
	}

	private SpriteFrames BuildFrames()
	{
		var frames = new SpriteFrames();
		string[] names = { "idle_down", "walk_down", "idle_up", "walk_up", "idle_right", "walk_right" };
		foreach (var name in names)
		{
			frames.AddAnimation(name);
			frames.SetAnimationLoop(name, true);
			frames.SetAnimationSpeed(name, name.StartsWith("walk") ? 8f : 2f);
		}

		// Team 1 (away / blue) uses the npc_blue sprite set; Team 0 uses the player sprites.
		string p = Team == 1
			? "res://assets/characters/npc_blue"
			: "res://assets/characters/player";

		Texture2D south = Tex($"{p}_south.png");
		Texture2D north = Tex($"{p}_north.png");
		Texture2D side  = Tex($"{p}_side.png");
		Fill(frames, "idle_down", south);
		Fill(frames, "walk_down", south, Tex($"{p}_walk_south_a.png"), south, Tex($"{p}_walk_south_b.png"));
		Fill(frames, "idle_up", north);
		Fill(frames, "walk_up", north, Tex($"{p}_walk_north_a.png"), north, Tex($"{p}_walk_north_b.png"));
		Fill(frames, "idle_right", side);
		Fill(frames, "walk_right", side, Tex($"{p}_walk_a.png"), side, Tex($"{p}_walk_side_b.png"));
		return frames;
	}

	private static void Fill(SpriteFrames frames, string name, params Texture2D[] textures)
	{
		foreach (var texture in textures)
			frames.AddFrame(name, texture);
	}

	private static Texture2D Tex(string path)
	{
		if (ResourceLoader.Exists(path))
			return GD.Load<Texture2D>(path);
		var img = Image.CreateEmpty(48, 72, false, Image.Format.Rgba8);
		img.Fill(Colors.White);
		return ImageTexture.CreateFromImage(img);
	}

	private static Texture2D MakeShadowTexture()
	{
		var img = Image.CreateEmpty(34, 12, false, Image.Format.Rgba8);
		img.Fill(new Color(0f, 0f, 0f, 0f));
		for (int y = 0; y < 12; y++)
		{
			for (int x = 0; x < 34; x++)
			{
				float nx = (x - 16.5f) / 16.5f;
				float ny = (y - 5.5f) / 5.5f;
				float d = nx * nx + ny * ny;
				if (d <= 1f)
					img.SetPixel(x, y, new Color(0f, 0f, 0f, (1f - d) * 0.12f));
			}
		}
		return ImageTexture.CreateFromImage(img);
	}
}
