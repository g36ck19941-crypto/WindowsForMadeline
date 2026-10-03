using System.Collections;
using System.Reflection;
using CelesteDesktop.Player;
using CelesteDesktop.Simulation.Core;

var tests = new (string Name, Action Body)[]
{
    ("reference constants match recorded source facts", ReferenceConstantsMatch),
    ("input axes are bounded", InputAxesAreBounded),
    ("initial grounded tick has no false landing", InitialGroundedTickIsQuiet),
    ("ground run reaches max speed in exact ticks", GroundRunReachesMax),
    ("ground friction reaches zero", GroundFrictionStops),
    ("air control uses reduced acceleration", AirControlIsReduced),
    ("same-direction overspeed uses reduction rate", OverspeedUsesReduction),
    ("facing follows nonzero horizontal input", FacingFollowsInput),
    ("normal gravity advances speed", NormalGravityAdvances),
    ("held jump applies half gravity near apex", HeldJumpUsesHalfGravity),
    ("normal fall speed is capped", NormalFallIsCapped),
    ("external fall limit rejects nonpositive values", ExternalFallLimitRejectsInvalid),
    ("external fall limit caps player speed", ExternalFallLimitCapsSpeed),
    ("removing external fall limit restores normal gravity", ExternalFallLimitRemovalRestoresGravity),
    ("fast fall target ramps by five per tick", FastFallTargetRamps),
    ("fast fall target returns toward normal", FastFallTargetRecovers),
    ("ground jump sets reference speed and timer", GroundJumpStarts),
    ("jump applies horizontal input boost", JumpAppliesHorizontalBoost),
    ("ordinary jump without platform lift reports zero inheritance", JumpWithoutLiftReportsZero),
    ("jump inherits horizontal platform velocity", JumpInheritsHorizontalLift),
    ("jump clamps excessive horizontal platform velocity", JumpClampsHorizontalLift),
    ("jump clamps upward platform velocity", JumpClampsUpwardLift),
    ("jump rejects downward platform velocity", JumpRejectsDownwardLift),
    ("coyote jump works on the sixth airborne tick", CoyoteJumpWorksAtBoundary),
    ("coyote jump expires after six airborne ticks", CoyoteJumpExpires),
    ("air press remains buffered for five ticks", JumpBufferWorksAtBoundary),
    ("expired buffer does not jump", JumpBufferExpires),
    ("held variable jump preserves upward speed", HeldVariableJumpPreservesSpeed),
    ("released jump cancels variable hold", ReleasedJumpCancelsVariableHold),
    ("horizontal collision zeros horizontal speed", HorizontalCollisionZerosSpeed),
    ("horizontal collision retains incoming wall speed", HorizontalCollisionRetainsSpeed),
    ("active wall retention is not overwritten", ActiveWallRetentionIsStable),
    ("wall speed restores when the wall clears", WallSpeedRestoresWhenClear),
    ("reverse input cancels wall speed retention", ReverseInputCancelsWallRetention),
    ("jump cancels wall speed retention", JumpCancelsWallRetention),
    ("external velocity cancels wall speed retention", ExternalVelocityCancelsWallRetention),
    ("wall speed retention expires after four ticks", WallSpeedRetentionExpires),
    ("upward collision corrects around a right corner", UpwardCornerCorrectsRight),
    ("upward collision corrects around a left corner", UpwardCornerCorrectsLeft),
    ("upward corner correction prefers horizontal travel", UpwardCornerPrefersTravelDirection),
    ("upward corner correction is bounded to four pixels", UpwardCornerCorrectionIsBounded),
    ("downward collision never applies corner correction", DownwardCollisionDoesNotCorrect),
    ("one-way platform permits upward Player passage", OneWayPlatformPermitsUpwardPassage),
    ("one-way platform catches Player from above", OneWayPlatformCatchesPlayerFromAbove),
    ("exact one-way top contact lands in the same tick", ExactOneWayTopContactLandsImmediately),
    ("Player stands stably on one-way platform", PlayerStandsOnOneWayPlatform),
    ("explicit input starts bounded one-way drop-through", OneWayDropThroughStarts),
    ("clearing one-way platform rearms drop-through", OneWayDropThroughCompletesAndRearms),
    ("ordinary Solid rejects one-way drop-through", OrdinarySolidRejectsDropThrough),
    ("one-way drop-through expires explicitly", OneWayDropThroughExpires),
    ("one-way platform replay is deterministic", OneWayReplayIsDeterministic),
    ("floor collision zeros vertical speed", FloorCollisionZerosSpeed),
    ("ceiling collision cancels variable jump", CeilingCollisionCancelsVariableJump),
    ("landing and left-ground events are explicit", GroundTransitionEventsAreExplicit),
    ("snapshots expose immutable events", SnapshotEventsAreImmutable),
    ("orchestrated update requires an active step", UpdateRequiresActiveStep),
    ("controller updates at most once per tick", UpdateRunsOncePerTick),
    ("same input replay is deterministic", ReplayIsDeterministic),
    ("controller surface accepts snapshots not live input devices", SurfaceHasNoLiveInputDependency)
};

tests = tests.Concat(DuckingCases.Tests).ToArray();

var failed = 0;
foreach (var test in tests)
{
    try
    {
        test.Body();
        Console.WriteLine($"PASS {test.Name}");
    }
    catch (Exception exception)
    {
        failed++;
        Console.Error.WriteLine($"FAIL {test.Name}");
        Console.Error.WriteLine(exception);
    }
}

Console.WriteLine($"RESULT total={tests.Length} passed={tests.Length - failed} failed={failed}");
return failed == 0 ? 0 : 1;

static void ReferenceConstantsMatch()
{
    var tuning = NormalJumpTuning.ReferencePartial;
    Equal(90m, tuning.MaxRun);
    Equal(1000m, tuning.RunAcceleration);
    Equal(400m, tuning.OverspeedReduction);
    Equal(0.65m, tuning.AirControlMultiplier);
    Equal(900m, tuning.Gravity);
    Equal(40m, tuning.HalfGravityThreshold);
    Equal(160m, tuning.NormalMaxFall);
    Equal(240m, tuning.FastMaxFall);
    Equal(300m, tuning.FastFallAcceleration);
    Equal(-105m, tuning.JumpSpeed);
    Equal(40m, tuning.JumpHorizontalBoost);
    Equal(250m, tuning.MaximumHorizontalLiftSpeed);
    Equal(130m, tuning.MaximumUpwardLiftSpeed);
    Equal(4, tuning.WallSpeedRetentionTicks);
    Equal(4, tuning.UpwardCornerCorrectionPixels);
    Equal(60m, tuning.OneWayDropThroughSpeed);
    Equal(12, tuning.OneWayDropThroughTicks);
    Equal(6, tuning.CoyoteTicks);
    Equal(5, tuning.JumpBufferTicks);
    Equal(12, tuning.VariableJumpTicks);
}

static void InputAxesAreBounded()
{
    Throws<ArgumentOutOfRangeException>(() => _ = new PlayerInput(2, 0, false, false));
    Throws<ArgumentOutOfRangeException>(() => _ = new PlayerInput(0, -2, false, false));
}

static void ExternalFallLimitRejectsInvalid()
{
    Throws<ArgumentOutOfRangeException>(() => _ = new PlayerExternalEffects(0m));
    Throws<ArgumentOutOfRangeException>(() => _ = new PlayerExternalEffects(-1m));
}

static void ExternalFallLimitCapsSpeed()
{
    var fixture = Airborne(new SimVector(0m, 100m));
    var snapshot = fixture.Controller.Step(Input(), new PlayerExternalEffects(40m), fixture.World);
    Equal(40m, snapshot.Speed.Y);
    Equal(40m, snapshot.AppliedMaximumFallSpeed);
    Assert(snapshot.Events.Any(item => item.Kind == PlayerNormalEventKind.ExternalFallSpeedLimited), "External fall limit event was missing.");
}

static void ExternalFallLimitRemovalRestoresGravity()
{
    var fixture = Airborne(new SimVector(0m, 100m));
    _ = fixture.Controller.Step(Input(), new PlayerExternalEffects(40m), fixture.World);
    var snapshot = fixture.Controller.Step(Input(), fixture.World);
    Assert(snapshot.Speed.Y > 40m, "Normal gravity did not resume after the effect was removed.");
    Equal(fixture.Controller.MaxFall, snapshot.AppliedMaximumFallSpeed);
    Assert(snapshot.Events.All(item => item.Kind != PlayerNormalEventKind.ExternalFallSpeedLimited), "External fall limit leaked into the next tick.");
}

static void InitialGroundedTickIsQuiet()
{
    var fixture = Grounded();
    var snapshot = fixture.Controller.Step(Input(), fixture.World);
    Assert(snapshot.Grounded, "Player was not grounded.");
    Equal(0, snapshot.Events.Count);
}

static void GroundRunReachesMax()
{
    var fixture = Grounded();
    PlayerNormalSnapshot? snapshot = null;
    for (var tick = 0; tick < 6; tick++)
    {
        snapshot = fixture.Controller.Step(Input(moveX: 1), fixture.World);
    }
    Equal(90m, snapshot!.Speed.X);
}

static void GroundFrictionStops()
{
    var fixture = Grounded();
    for (var tick = 0; tick < 6; tick++)
    {
        fixture.Controller.Step(Input(moveX: 1), fixture.World);
    }
    for (var tick = 0; tick < 6; tick++)
    {
        fixture.Controller.Step(Input(), fixture.World);
    }
    Equal(0m, fixture.Controller.SpeedX);
}

static void AirControlIsReduced()
{
    var fixture = Airborne();
    var snapshot = fixture.Controller.Step(Input(moveX: 1), fixture.World);
    Equal(650m / SimulationConstants.TicksPerSecond, snapshot.Speed.X);
}

static void OverspeedUsesReduction()
{
    var fixture = Grounded(new SimVector(100m, 0m));
    var snapshot = fixture.Controller.Step(Input(moveX: 1), fixture.World);
    Equal(100m - (400m / SimulationConstants.TicksPerSecond), snapshot.Speed.X);
}

static void FacingFollowsInput()
{
    var fixture = Grounded();
    Equal(-1, fixture.Controller.Step(Input(moveX: -1), fixture.World).Facing);
    Equal(-1, fixture.Controller.Step(Input(), fixture.World).Facing);
}

static void NormalGravityAdvances()
{
    var fixture = Airborne();
    var snapshot = fixture.Controller.Step(Input(), fixture.World);
    Equal(15m, snapshot.Speed.Y);
}

static void HeldJumpUsesHalfGravity()
{
    var fixture = Airborne();
    var snapshot = fixture.Controller.Step(Input(jumpHeld: true), fixture.World);
    Equal(7.5m, snapshot.Speed.Y);
}

static void NormalFallIsCapped()
{
    var fixture = Airborne();
    PlayerNormalSnapshot? snapshot = null;
    for (var tick = 0; tick < 20; tick++)
    {
        snapshot = fixture.Controller.Step(Input(), fixture.World);
    }
    Equal(160m, snapshot!.Speed.Y);
}

static void FastFallTargetRamps()
{
    var fixture = Airborne(new SimVector(0m, 160m));
    var snapshot = fixture.Controller.Step(Input(moveY: 1), fixture.World);
    Equal(165m, snapshot.MaxFall);
    Equal(165m, snapshot.Speed.Y);
}

static void FastFallTargetRecovers()
{
    var fixture = Airborne(new SimVector(0m, 160m));
    for (var tick = 0; tick < 3; tick++)
    {
        fixture.Controller.Step(Input(moveY: 1), fixture.World);
    }
    var before = fixture.Controller.MaxFall;
    var snapshot = fixture.Controller.Step(Input(), fixture.World);
    Equal(before - (300m / SimulationConstants.TicksPerSecond), snapshot.MaxFall);
}

static void GroundJumpStarts()
{
    var fixture = Grounded();
    var snapshot = fixture.Controller.Step(Input(jumpPressed: true, jumpHeld: true), fixture.World);
    Equal(-105m, snapshot.Speed.Y);
    Equal(11, snapshot.VariableJumpTicks);
    Assert(snapshot.Events.Any(item => item.Kind == PlayerNormalEventKind.Jumped), "Jump event missing.");
    Assert(!snapshot.Grounded, "Jump snapshot remained grounded.");
}

static void JumpWithoutLiftReportsZero()
{
    var fixture = Grounded();
    var snapshot = fixture.Controller.Step(Input(jumpPressed: true, jumpHeld: true), fixture.World);
    Equal(SimVector.Zero, snapshot.AppliedLiftSpeed);
    Assert(snapshot.Events.All(item => item.Kind != PlayerNormalEventKind.LiftVelocityApplied), "A zero lift emitted an application event.");
}

static void JumpInheritsHorizontalLift()
{
    var fixture = Grounded();
    var snapshot = fixture.Controller.Step(
        Input(jumpPressed: true, jumpHeld: true),
        fixture.World,
        world => fixture.Floor!.Move(2m, 0m, world));
    Equal(new SimVector(120m, 0m), snapshot.AppliedLiftSpeed);
    Equal(120m, snapshot.Speed.X);
    Assert(snapshot.Events.Any(item => item.Kind == PlayerNormalEventKind.LiftVelocityApplied), "Horizontal lift event was missing.");
}

static void JumpClampsHorizontalLift()
{
    var fixture = Grounded();
    var snapshot = fixture.Controller.Step(
        Input(jumpPressed: true, jumpHeld: true),
        fixture.World,
        world => fixture.Floor!.Move(5m, 0m, world));
    Equal(new SimVector(250m, 0m), snapshot.AppliedLiftSpeed);
    Equal(250m, snapshot.Speed.X);
}

static void JumpClampsUpwardLift()
{
    var fixture = Grounded();
    var snapshot = fixture.Controller.Step(
        Input(jumpPressed: true, jumpHeld: true),
        fixture.World,
        world => fixture.Floor!.Move(0m, -3m, world));
    Equal(new SimVector(0m, -130m), snapshot.AppliedLiftSpeed);
    Equal(-235m, snapshot.Speed.Y);
}

static void JumpRejectsDownwardLift()
{
    var fixture = Grounded();
    var snapshot = fixture.Controller.Step(
        Input(jumpPressed: true, jumpHeld: true),
        fixture.World,
        world => fixture.Floor!.Move(0m, 2m, world));
    Equal(SimVector.Zero, snapshot.AppliedLiftSpeed);
    Equal(-105m, snapshot.Speed.Y);
}

static void JumpAppliesHorizontalBoost()
{
    var fixture = Grounded();
    var snapshot = fixture.Controller.Step(Input(1, 0, true, true), fixture.World);
    Equal((1000m / SimulationConstants.TicksPerSecond) + 40m, snapshot.Speed.X);
}

static void CoyoteJumpWorksAtBoundary()
{
    var fixture = NarrowGrounded();
    fixture.Controller.Step(Input(), fixture.World);
    fixture.Controller.Step(Input(), fixture.World, _ => fixture.Actor.MoveX(20m, fixture.World));
    for (var tick = 0; tick < 4; tick++)
    {
        fixture.Controller.Step(Input(), fixture.World);
    }
    var snapshot = fixture.Controller.Step(Input(jumpPressed: true, jumpHeld: true), fixture.World);
    Assert(snapshot.Events.Any(item => item.Kind == PlayerNormalEventKind.Jumped), "Sixth-tick coyote jump did not fire.");
}

static void CoyoteJumpExpires()
{
    var fixture = NarrowGrounded();
    fixture.Controller.Step(Input(), fixture.World);
    fixture.Controller.Step(Input(), fixture.World, _ => fixture.Actor.MoveX(20m, fixture.World));
    for (var tick = 0; tick < 5; tick++)
    {
        fixture.Controller.Step(Input(), fixture.World);
    }
    var snapshot = fixture.Controller.Step(Input(jumpPressed: true, jumpHeld: true), fixture.World);
    Assert(!snapshot.Events.Any(item => item.Kind == PlayerNormalEventKind.Jumped), "Expired coyote jump fired.");
}

static void JumpBufferWorksAtBoundary()
{
    var fixture = FallingTowardFloor();
    fixture.Controller.Step(Input(jumpPressed: true, jumpHeld: true), fixture.World);
    PlayerNormalSnapshot snapshot = fixture.Controller.Step(Input(jumpHeld: true), fixture.World);
    while (!snapshot.Grounded)
    {
        snapshot = fixture.Controller.Step(Input(jumpHeld: true), fixture.World);
    }
    snapshot = fixture.Controller.Step(Input(jumpHeld: true), fixture.World);
    Assert(snapshot.Events.Any(item => item.Kind == PlayerNormalEventKind.Jumped), "Buffered jump did not fire on landing.");
}

static void JumpBufferExpires()
{
    var fixture = Airborne();
    fixture.Controller.Step(Input(jumpPressed: true, jumpHeld: true), fixture.World);
    for (var tick = 0; tick < 5; tick++)
    {
        fixture.Controller.Step(Input(jumpHeld: true), fixture.World);
    }
    Equal(0, fixture.Controller.JumpBufferTicksRemaining);
}

static void HeldVariableJumpPreservesSpeed()
{
    var fixture = Grounded();
    fixture.Controller.Step(Input(jumpPressed: true, jumpHeld: true), fixture.World);
    for (var tick = 0; tick < 5; tick++)
    {
        fixture.Controller.Step(Input(jumpHeld: true), fixture.World);
    }
    Equal(-105m, fixture.Controller.SpeedY);
}

static void ReleasedJumpCancelsVariableHold()
{
    var fixture = Grounded();
    fixture.Controller.Step(Input(jumpPressed: true, jumpHeld: true), fixture.World);
    var snapshot = fixture.Controller.Step(Input(), fixture.World);
    Equal(-90m, snapshot.Speed.Y);
    Equal(0, snapshot.VariableJumpTicks);
}

static void HorizontalCollisionZerosSpeed()
{
    var world = new SimulationWorld();
    var actor = new Actor("player", 0, 0, 2, 2);
    world.Add(actor);
    world.Add(new Solid("wall", 3, 0, 2, 10));
    var controller = new PlayerNormalController(actor, initialSpeed: new SimVector(120m, 0m));
    var snapshot = controller.Step(Input(moveX: 1), world);
    Equal(0m, snapshot.Speed.X);
    Assert(snapshot.Events.Any(item => item.Kind == PlayerNormalEventKind.HorizontalBlocked && item.SolidId == "wall"), "Horizontal block event missing.");
}

static void HorizontalCollisionRetainsSpeed()
{
    var fixture = WallCollision();
    var snapshot = fixture.Controller.Step(Input(moveX: 1), fixture.World);
    Equal(0m, snapshot.Speed.X);
    Equal(90m, snapshot.WallSpeedRetained);
    Equal(4, snapshot.WallSpeedRetentionTicks);
    Assert(snapshot.Events.Any(item => item.Kind == PlayerNormalEventKind.WallSpeedRetained && item.SolidId == "wall"), "Wall speed retention event missing.");
}

static void ActiveWallRetentionIsStable()
{
    var fixture = WallCollision();
    fixture.Controller.Step(Input(moveX: 1), fixture.World);
    var snapshot = fixture.Controller.Step(Input(moveX: 1), fixture.World);
    Equal(90m, snapshot.WallSpeedRetained);
    Equal(3, snapshot.WallSpeedRetentionTicks);
    Assert(snapshot.Events.All(item => item.Kind != PlayerNormalEventKind.WallSpeedRetained), "Active retention was overwritten by a second collision.");
}

static void WallSpeedRestoresWhenClear()
{
    var fixture = WallCollision();
    fixture.Controller.Step(Input(moveX: 1), fixture.World);
    var snapshot = fixture.Controller.Step(
        Input(moveX: 1),
        fixture.World,
        world => fixture.Wall.Move(20m, 0m, world));
    Equal(90m, snapshot.Speed.X);
    Equal(0m, snapshot.WallSpeedRetained);
    Equal(0, snapshot.WallSpeedRetentionTicks);
    Assert(snapshot.Events.Any(item => item.Kind == PlayerNormalEventKind.WallSpeedRestored), "Wall speed restore event missing.");
}

static void ReverseInputCancelsWallRetention()
{
    var fixture = WallCollision();
    fixture.Controller.Step(Input(moveX: 1), fixture.World);
    var snapshot = fixture.Controller.Step(Input(moveX: -1), fixture.World);
    Assert(snapshot.Speed.X < 0m, "Reverse input did not take control.");
    Equal(0m, snapshot.WallSpeedRetained);
    Equal(0, snapshot.WallSpeedRetentionTicks);
    Assert(snapshot.Events.Any(item => item.Kind == PlayerNormalEventKind.WallSpeedRetentionCancelled), "Wall speed cancellation event missing.");
}

static void JumpCancelsWallRetention()
{
    var fixture = WallCollision();
    fixture.Controller.Step(Input(moveX: 1), fixture.World);
    var snapshot = fixture.Controller.Step(Input(jumpPressed: true, jumpHeld: true), fixture.World);
    Equal(0m, snapshot.WallSpeedRetained);
    Equal(0, snapshot.WallSpeedRetentionTicks);
    Assert(snapshot.Events.Any(item => item.Kind == PlayerNormalEventKind.Jumped), "Jump event missing.");
    Assert(snapshot.Events.Any(item => item.Kind == PlayerNormalEventKind.WallSpeedRetentionCancelled), "Jump did not cancel wall retention.");
}

static void ExternalVelocityCancelsWallRetention()
{
    var fixture = WallCollision();
    fixture.Controller.Step(Input(moveX: 1), fixture.World);
    var effects = new PlayerExternalEffects(null, new ExternalVelocityEffect(-120m, -80m));
    var snapshot = fixture.Controller.Step(Input(), effects, fixture.World);
    Equal(0m, snapshot.WallSpeedRetained);
    Equal(0, snapshot.WallSpeedRetentionTicks);
    Equal(-120m, snapshot.Speed.X);
    Equal(-80m, snapshot.Speed.Y);
    Assert(snapshot.Events.Any(item => item.Kind == PlayerNormalEventKind.WallSpeedRetentionCancelled), "External velocity did not cancel wall retention.");
    Assert(snapshot.Events.Any(item => item.Kind == PlayerNormalEventKind.ExternalVelocityApplied), "External velocity event missing.");
}

static void WallSpeedRetentionExpires()
{
    var fixture = WallCollision();
    fixture.Controller.Step(Input(moveX: 1), fixture.World);
    PlayerNormalSnapshot? snapshot = null;
    for (var tick = 0; tick < 4; tick++)
    {
        snapshot = fixture.Controller.Step(Input(moveX: 1), fixture.World);
    }
    Equal(0m, snapshot!.WallSpeedRetained);
    Equal(0, snapshot.WallSpeedRetentionTicks);
    Assert(snapshot.Events.Any(item => item.Kind == PlayerNormalEventKind.WallSpeedRetentionExpired), "Wall speed expiration event missing.");
}

static void UpwardCornerCorrectsRight()
{
    var world = new SimulationWorld();
    var actor = new Actor("player", 0, 4, 2, 2);
    world.Add(actor);
    world.Add(new Solid("corner", -2, 1, 3, 2));
    var controller = new PlayerNormalController(actor, initialSpeed: new SimVector(0m, -120m));

    var snapshot = controller.Step(Input(moveX: 1, jumpHeld: true), world);

    Equal(1, snapshot.UpwardCornerCorrectionX);
    Equal(1, snapshot.Position.X);
    Equal(2, snapshot.Position.Y);
    Assert(snapshot.Speed.Y < 0m, "Upward speed was cancelled after a valid correction.");
    Assert(snapshot.Events.Any(item => item.Kind == PlayerNormalEventKind.UpwardCornerCorrected && item.SolidId == "corner"), "Corner correction event missing.");
    Assert(snapshot.Events.All(item => item.Kind != PlayerNormalEventKind.VerticalBlocked), "Corrected movement was also reported as blocked.");
}

static void UpwardCornerCorrectsLeft()
{
    var world = new SimulationWorld();
    var actor = new Actor("player", 0, 4, 2, 2);
    world.Add(actor);
    world.Add(new Solid("corner", 1, 1, 3, 2));
    var controller = new PlayerNormalController(actor, initialSpeed: new SimVector(0m, -120m));

    var snapshot = controller.Step(Input(moveX: -1, jumpHeld: true), world);

    Equal(-1, snapshot.UpwardCornerCorrectionX);
    Equal(-1, snapshot.Position.X);
    Equal(2, snapshot.Position.Y);
}

static void UpwardCornerPrefersTravelDirection()
{
    var world = new SimulationWorld();
    var actor = new Actor("player", 0, 4, 2, 2);
    world.Add(actor);
    world.Add(new Solid("narrow-ceiling", 0, 1, 3, 2));
    var controller = new PlayerNormalController(actor, initialSpeed: new SimVector(90m, -120m));

    var snapshot = controller.Step(Input(moveX: 1, jumpHeld: true), world);

    Equal(1, snapshot.UpwardCornerCorrectionX);
    Equal(3, snapshot.Position.X);
}

static void UpwardCornerCorrectionIsBounded()
{
    var world = new SimulationWorld();
    var actor = new Actor("player", 0, 4, 2, 2);
    world.Add(actor);
    world.Add(new Solid("wide-ceiling", -4, 1, 10, 2));
    var controller = new PlayerNormalController(actor, initialSpeed: new SimVector(0m, -120m));

    var snapshot = controller.Step(Input(jumpHeld: true), world);

    Equal(0, snapshot.UpwardCornerCorrectionX);
    Equal(0, snapshot.Position.X);
    Equal(3, snapshot.Position.Y);
    Equal(0m, snapshot.Speed.Y);
    Assert(snapshot.Events.Any(item => item.Kind == PlayerNormalEventKind.VerticalBlocked), "Uncorrectable ceiling did not report a block.");
}

static void DownwardCollisionDoesNotCorrect()
{
    var world = new SimulationWorld();
    var actor = new Actor("player", 0, 0, 2, 2);
    world.Add(actor);
    world.Add(new Solid("floor-edge", -2, 3, 3, 2));
    var controller = new PlayerNormalController(actor, initialSpeed: new SimVector(0m, 120m));

    var snapshot = controller.Step(Input(moveX: 1), world);

    Equal(0, snapshot.UpwardCornerCorrectionX);
    Equal(0, snapshot.Position.X);
    Equal(1, snapshot.Position.Y);
    Equal(0m, snapshot.Speed.Y);
    Assert(snapshot.Events.All(item => item.Kind != PlayerNormalEventKind.UpwardCornerCorrected), "Downward collision incorrectly applied upward correction.");
}

static void OneWayPlatformPermitsUpwardPassage()
{
    var world = new SimulationWorld();
    var actor = new Actor("player", 0, 8, 2, 2);
    world.Add(actor);
    world.Add(new OneWayPlatform("platform", -10, 5, 20, 2));
    var controller = new PlayerNormalController(actor, initialSpeed: new SimVector(0m, -180m));
    PlayerNormalSnapshot? snapshot = null;
    for (var tick = 0; tick < 3; tick++)
    {
        snapshot = controller.Step(Input(jumpHeld: true), world);
    }
    Assert(actor.Bounds.Bottom <= 5, "Player did not pass completely above the one-way platform.");
    Assert(snapshot!.Events.All(item => item.Kind != PlayerNormalEventKind.OneWayPlatformLanded), "Upward passage emitted a landing event.");
}

static void OneWayPlatformCatchesPlayerFromAbove()
{
    var fixture = FallingTowardOneWay();
    var snapshot = fixture.Controller.Step(Input(), fixture.World);
    Equal(3, snapshot.Position.Y);
    Equal(0m, snapshot.Speed.Y);
    Assert(snapshot.Grounded, "Player was not grounded after landing on the one-way platform.");
    Equal("platform", snapshot.GroundedOneWayPlatformId);
    Assert(snapshot.Events.Any(item => item.Kind == PlayerNormalEventKind.OneWayPlatformLanded && item.SolidId == "platform"), "One-way landing event was missing.");
}

static void ExactOneWayTopContactLandsImmediately()
{
    var world = new SimulationWorld();
    var actor = new Actor("player", 0, 0, 2, 2);
    world.Add(actor);
    world.Add(new OneWayPlatform("platform", -10, 3, 20, 2));
    var controller = new PlayerNormalController(
        actor,
        initialSpeed: new SimVector(0m, 60m));
    var snapshot = controller.Step(Input(), world);
    Equal(1, snapshot.Position.Y);
    Equal(0m, snapshot.Speed.Y);
    Equal("platform", snapshot.GroundedOneWayPlatformId);
    Assert(snapshot.Events.Any(item => item.Kind == PlayerNormalEventKind.OneWayPlatformLanded), "Exact top contact delayed its landing event.");
}

static void PlayerStandsOnOneWayPlatform()
{
    var fixture = FallingTowardOneWay();
    var landed = fixture.Controller.Step(Input(), fixture.World);
    var standing = fixture.Controller.Step(Input(), fixture.World);
    Equal(landed.Position, standing.Position);
    Assert(standing.Grounded, "Player did not remain grounded on the one-way platform.");
    Equal("platform", standing.GroundedOneWayPlatformId);
    Assert(standing.Events.All(item => item.Kind != PlayerNormalEventKind.OneWayPlatformLanded), "Stable standing repeated the landing event.");
}

static void OneWayDropThroughStarts()
{
    var fixture = StandingOnOneWay();
    _ = fixture.Controller.Step(Input(), fixture.World);
    var snapshot = fixture.Controller.Step(Input(dropThroughPressed: true), fixture.World);
    Equal("platform", snapshot.DropThroughPlatformId);
    Equal(11, snapshot.DropThroughTicksRemaining);
    Assert(!snapshot.Grounded, "Drop-through tick remained grounded.");
    Assert(snapshot.Position.Y > 0, "Drop-through did not move Player below the platform top.");
    Assert(snapshot.Events.Any(item => item.Kind == PlayerNormalEventKind.OneWayDropThroughStarted && item.SolidId == "platform"), "Drop-through start event was missing.");
}

static void OneWayDropThroughCompletesAndRearms()
{
    var world = new SimulationWorld();
    var actor = new Actor("player", 0, 0, 8, 11);
    world.Add(actor);
    world.Add(new OneWayPlatform("upper", -20, 11, 40, 2));
    world.Add(new OneWayPlatform("lower", -20, 30, 40, 2));
    var controller = new PlayerNormalController(actor);
    _ = controller.Step(Input(), world);
    var snapshot = controller.Step(Input(dropThroughPressed: true), world);
    var completed = false;
    for (var tick = 0; tick < 30 && snapshot.GroundedOneWayPlatformId != "lower"; tick++)
    {
        snapshot = controller.Step(Input(), world);
        completed |= snapshot.Events.Any(item => item.Kind == PlayerNormalEventKind.OneWayDropThroughCompleted && item.SolidId == "upper");
    }
    Assert(completed, "Clearing the upper platform did not complete drop-through.");
    Equal("lower", snapshot.GroundedOneWayPlatformId);

    var second = controller.Step(Input(dropThroughPressed: true), world);
    Equal("lower", second.DropThroughPlatformId);
    Assert(second.Events.Any(item => item.Kind == PlayerNormalEventKind.OneWayDropThroughStarted && item.SolidId == "lower"), "Drop-through did not rearm on a later platform.");
}

static void OrdinarySolidRejectsDropThrough()
{
    var fixture = Grounded();
    _ = fixture.Controller.Step(Input(), fixture.World);
    var snapshot = fixture.Controller.Step(Input(dropThroughPressed: true), fixture.World);
    Equal<string?>(null, snapshot.DropThroughPlatformId);
    Assert(snapshot.Grounded, "Ordinary Solid was bypassed by one-way drop-through input.");
    Assert(snapshot.Events.All(item => item.Kind != PlayerNormalEventKind.OneWayDropThroughStarted), "Solid contact emitted a one-way drop-through event.");
}

static void OneWayDropThroughExpires()
{
    var world = new SimulationWorld();
    var actor = new Actor("player", 0, 0, 8, 11);
    world.Add(actor);
    world.Add(new OneWayPlatform("deep", -20, 11, 40, 100));
    var controller = new PlayerNormalController(actor);
    _ = controller.Step(Input(), world);
    var snapshot = controller.Step(Input(dropThroughPressed: true), world);
    var expired = snapshot.Events.Any(item => item.Kind == PlayerNormalEventKind.OneWayDropThroughExpired);
    for (var tick = 0; tick < controller.Tuning.OneWayDropThroughTicks && !expired; tick++)
    {
        snapshot = controller.Step(Input(), world);
        expired = snapshot.Events.Any(item => item.Kind == PlayerNormalEventKind.OneWayDropThroughExpired && item.SolidId == "deep");
    }
    Assert(expired, "Bounded drop-through state did not expire explicitly.");
    Equal<string?>(null, snapshot.DropThroughPlatformId);
    Equal(0, snapshot.DropThroughTicksRemaining);
}

static void OneWayReplayIsDeterministic()
{
    Equal(RunOneWayReplay(), RunOneWayReplay());
}

static void FloorCollisionZerosSpeed()
{
    var world = new SimulationWorld();
    var actor = new Actor("player", 0, 1, 2, 2);
    world.Add(actor);
    world.Add(new Solid("floor", 0, 4, 8, 2));
    var controller = new PlayerNormalController(actor, initialSpeed: new SimVector(0m, 160m));
    var snapshot = controller.Step(Input(), world);
    Equal(0m, snapshot.Speed.Y);
    Assert(snapshot.Grounded, "Player did not land.");
}

static void CeilingCollisionCancelsVariableJump()
{
    var world = new SimulationWorld();
    var actor = new Actor("player", 0, 4, 2, 2);
    world.Add(actor);
    world.Add(new Solid("floor", 0, 6, 8, 2));
    world.Add(new Solid("ceiling", -10, 0, 28, 2));
    var controller = new PlayerNormalController(actor);
    controller.Step(Input(jumpPressed: true, jumpHeld: true), world);
    var snapshot = controller.Step(Input(jumpHeld: true), world);
    Equal(0m, snapshot.Speed.Y);
    Equal(0, snapshot.VariableJumpTicks);
    Assert(snapshot.Events.Any(item => item.Kind == PlayerNormalEventKind.VerticalBlocked && item.SolidId == "ceiling"), "Ceiling event missing.");
}

static void GroundTransitionEventsAreExplicit()
{
    var fixture = NarrowGrounded();
    fixture.Controller.Step(Input(), fixture.World);
    var left = fixture.Controller.Step(Input(), fixture.World, _ => fixture.Actor.MoveX(20m, fixture.World));
    Assert(left.Events.Any(item => item.Kind == PlayerNormalEventKind.LeftGround), "Left-ground event missing.");
}

static void SnapshotEventsAreImmutable()
{
    var fixture = Grounded();
    var snapshot = fixture.Controller.Step(Input(jumpPressed: true, jumpHeld: true), fixture.World);
    Assert(snapshot.Events is IList list && list.IsReadOnly, "Snapshot events are mutable.");
}

static void UpdateRequiresActiveStep()
{
    var fixture = Grounded();
    Throws<InvalidOperationException>(() => fixture.Controller.Update(Input(), fixture.World));
}

static void UpdateRunsOncePerTick()
{
    var fixture = Grounded();
    Throws<InvalidOperationException>(() => fixture.World.Step(world =>
    {
        fixture.Controller.Update(Input(), world);
        fixture.Controller.Update(Input(), world);
    }));
}

static void ReplayIsDeterministic()
{
    Equal(RunReplay(), RunReplay());
}

static void SurfaceHasNoLiveInputDependency()
{
    var assembly = typeof(PlayerNormalController).Assembly;
    var exposed = assembly.GetExportedTypes()
        .SelectMany(type => type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
        .SelectMany(MemberTypes)
        .Where(type => type is not null)
        .Cast<Type>();
    var forbidden = new[] { "System.Windows", "Microsoft.Win32", "System.IO.", "System.Diagnostics", "System.Threading" };
    Assert(!exposed.Any(type => forbidden.Any(prefix => (type.FullName ?? string.Empty).StartsWith(prefix, StringComparison.Ordinal))),
        "A live input, GUI, file, clock, or platform type leaked into the public surface.");
}

static IEnumerable<Type?> MemberTypes(MemberInfo member) => member switch
{
    MethodInfo method => method.GetParameters().Select(parameter => parameter.ParameterType).Append(method.ReturnType),
    PropertyInfo property => [property.PropertyType],
    FieldInfo field => [field.FieldType],
    _ => []
};

static string RunReplay()
{
    var fixture = Grounded();
    var inputs = new[]
    {
        Input(moveX: 1),
        Input(1, 0, true, true),
        Input(1, 0, false, true),
        Input(1, 0, false, true),
        Input(),
        Input(moveY: 1)
    };
    return string.Join('|', inputs.Select(input =>
    {
        var snapshot = fixture.Controller.Step(input, fixture.World);
        return $"{snapshot.Tick}:{snapshot.Position.X},{snapshot.Position.Y}:{snapshot.Speed.X},{snapshot.Speed.Y}:{snapshot.WallSpeedRetained},{snapshot.WallSpeedRetentionTicks}:{snapshot.CoyoteTicks},{snapshot.JumpBufferTicks},{snapshot.VariableJumpTicks}:{string.Join(',', snapshot.Events.Select(item => item.Kind))}";
    }));
}

static string RunOneWayReplay()
{
    var fixture = StandingOnOneWay();
    var inputs = new[]
    {
        Input(),
        Input(dropThroughPressed: true),
        Input(),
        Input(),
        Input(),
        Input()
    };
    return string.Join('|', inputs.Select(input =>
    {
        var snapshot = fixture.Controller.Step(input, fixture.World);
        return $"{snapshot.Tick}:{snapshot.Position.X},{snapshot.Position.Y}:{snapshot.Speed.X},{snapshot.Speed.Y}:{snapshot.GroundedOneWayPlatformId}:{snapshot.DropThroughPlatformId},{snapshot.DropThroughTicksRemaining}:{string.Join(',', snapshot.Events.Select(item => item.Kind))}";
    }));
}

static PlayerInput Input(
    int moveX = 0,
    int moveY = 0,
    bool jumpPressed = false,
    bool jumpHeld = false,
    bool dropThroughPressed = false) => new(
        moveX,
        moveY,
        jumpPressed,
        jumpHeld,
        dropThroughPressed: dropThroughPressed);

static Fixture Grounded(SimVector initialSpeed = default)
{
    var world = new SimulationWorld();
    var actor = new Actor("player", 0, 0, 8, 11);
    var floor = new Solid("floor", -100, 11, 200, 4);
    world.Add(actor);
    world.Add(floor);
    return new Fixture(world, actor, floor, new PlayerNormalController(actor, initialSpeed: initialSpeed));
}

static Fixture Airborne(SimVector initialSpeed = default)
{
    var world = new SimulationWorld();
    var actor = new Actor("player", 0, 0, 8, 11);
    world.Add(actor);
    return new Fixture(world, actor, null, new PlayerNormalController(actor, initialSpeed: initialSpeed));
}

static Fixture NarrowGrounded()
{
    var world = new SimulationWorld();
    var actor = new Actor("player", 0, 0, 8, 11);
    var floor = new Solid("floor", 0, 11, 8, 4);
    world.Add(actor);
    world.Add(floor);
    return new Fixture(world, actor, floor, new PlayerNormalController(actor));
}

static Fixture FallingTowardFloor()
{
    var world = new SimulationWorld();
    var actor = new Actor("player", 0, 0, 2, 2);
    var floor = new Solid("floor", -10, 5, 20, 2);
    world.Add(actor);
    world.Add(floor);
    return new Fixture(world, actor, floor, new PlayerNormalController(actor, initialSpeed: new SimVector(0m, 60m)));
}

static OneWayFixture FallingTowardOneWay()
{
    var world = new SimulationWorld();
    var actor = new Actor("player", 0, 0, 2, 2);
    var platform = new OneWayPlatform("platform", -10, 5, 20, 2);
    world.Add(actor);
    world.Add(platform);
    return new OneWayFixture(
        world,
        actor,
        platform,
        new PlayerNormalController(actor, initialSpeed: new SimVector(0m, 240m)));
}

static OneWayFixture StandingOnOneWay()
{
    var world = new SimulationWorld();
    var actor = new Actor("player", 0, 0, 8, 11);
    var platform = new OneWayPlatform("platform", -20, 11, 40, 2);
    world.Add(actor);
    world.Add(platform);
    return new OneWayFixture(world, actor, platform, new PlayerNormalController(actor));
}

static WallFixture WallCollision()
{
    var world = new SimulationWorld();
    var actor = new Actor("player", 0, 0, 8, 11);
    var wall = new Solid("wall", 9, -20, 4, 40);
    var floor = new Solid("floor", -20, 11, 40, 4);
    world.Add(actor);
    world.Add(wall);
    world.Add(floor);
    return new WallFixture(
        world,
        actor,
        wall,
        new PlayerNormalController(actor, initialSpeed: new SimVector(90m, 0m)));
}

static void Throws<T>(Action action) where T : Exception
{
    try
    {
        action();
    }
    catch (T)
    {
        return;
    }
    throw new InvalidOperationException($"Expected {typeof(T).Name}.");
}

static void Equal<T>(T expected, T actual)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"Expected {expected}, got {actual}.");
    }
}

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

internal sealed record Fixture(
    SimulationWorld World,
    Actor Actor,
    Solid? Floor,
    PlayerNormalController Controller);

internal sealed record WallFixture(
    SimulationWorld World,
    Actor Actor,
    Solid Wall,
    PlayerNormalController Controller);

internal sealed record OneWayFixture(
    SimulationWorld World,
    Actor Actor,
    OneWayPlatform Platform,
    PlayerNormalController Controller);
