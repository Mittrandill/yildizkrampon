using Godot;

/// Scene builder — run: dotnet build && godot --headless --script scenes/BuildMatch.cs
/// FIFA 94 tarzı yatay saha, 7v7 (6 saha oyuncusu + 1 kaleci per takim)
public partial class BuildMatch : SceneBuilderBase
{
    private const float W  = 1920f;
    private const float H  = 768f;
    private const float CX = W / 2f;
    private const float CY = H / 2f;
    private const float GT = 279f;
    private const float GB = 489f;
    private const float GH = GB - GT;
    private const float PA_W = 240f;
    private const float PA_H = 380f;
    private const float PA_Y = CY - PA_H / 2f;

    public override void _Initialize()
    {
        GD.Print("Generating: Match (FIFA 7v7)");
        var temp = new Node();
        var root = new MatchManager();
        root.Name = "Match";
        temp.AddChild(root);
        _BuildPitch(root);
        _BuildWalls(root);
        _BuildGoalTriggers(root);
        var pNode = new Node2D(); pNode.Name = "Players"; root.AddChild(pNode);
        _BuildGK(pNode, "GKRed",   true,  new Vector2(55, CY));
        _BuildFP(pNode, "DefRed1", true,  new Vector2(320, 230), "DEF", 0, 0.18f, 0.30f);
        _BuildFP(pNode, "DefRed2", true,  new Vector2(320, 490), "DEF", 1, 0.18f, 0.64f);
        _BuildFP(pNode, "MidRed1", true,  new Vector2(630, 270), "MID", 2, 0.38f, 0.35f);
        _BuildFP(pNode, "MidRed2", true,  new Vector2(630, 450), "MID", 3, 0.38f, 0.59f);
        _BuildFP(pNode, "FwdRed1", true,  new Vector2(890, 290), "FWD", 4, 0.56f, 0.38f);
        _BuildFP(pNode, "FwdRed2", true,  new Vector2(890, 440), "FWD", 5, 0.56f, 0.57f);
        _BuildGK(pNode, "GKBlue",  false, new Vector2(W - 55, CY));
        _BuildFP(pNode, "DefBlu1", false, new Vector2(W-320, 230), "DEF", 0, 0.18f, 0.30f);
        _BuildFP(pNode, "DefBlu2", false, new Vector2(W-320, 490), "DEF", 1, 0.18f, 0.64f);
        _BuildFP(pNode, "MidBlu1", false, new Vector2(W-630, 270), "MID", 2, 0.38f, 0.35f);
        _BuildFP(pNode, "MidBlu2", false, new Vector2(W-630, 450), "MID", 3, 0.38f, 0.59f);
        _BuildFP(pNode, "FwdBlu1", false, new Vector2(W-890, 290), "FWD", 4, 0.56f, 0.38f);
        _BuildFP(pNode, "FwdBlu2", false, new Vector2(W-890, 440), "FWD", 5, 0.56f, 0.57f);
        _BuildBall(root);
        _BuildCamera(root);
        _BuildHUD(root);
        temp.RemoveChild(root);
        temp.Free();
        PackAndSave(root, "res://scenes/match.tscn");
    }

    private void _BuildPitch(Node root)
    {
        var grass = new ColorRect();
        grass.Name = "Grass"; grass.Color = new Color(0.13f, 0.52f, 0.13f);
        grass.Position = Vector2.Zero; grass.Size = new Vector2(W, H); grass.ZIndex = -5;
        root.AddChild(grass);

        float sw = W / 10f;
        for (int i = 0; i < 10; i += 2)
        {
            var s = new ColorRect(); s.Name = "S" + i; s.Color = new Color(0.11f, 0.48f, 0.11f);
            s.Position = new Vector2(i * sw, 0); s.Size = new Vector2(sw, H); s.ZIndex = -4;
            root.AddChild(s);
        }

        Color lc = new Color(1f, 1f, 1f, 0.85f); float lw = 3f;
        _L(root, "LT", new Vector2(0, 0),  new Vector2(W, 0),  lc, lw);
        _L(root, "LB", new Vector2(0, H),  new Vector2(W, H),  lc, lw);
        _L(root, "LL", new Vector2(0, 0),  new Vector2(0, H),  lc, lw);
        _L(root, "LR", new Vector2(W, 0),  new Vector2(W, H),  lc, lw);
        _L(root, "LM", new Vector2(CX, 0), new Vector2(CX, H), lc, lw);
        _Circle(root, "CC", new Vector2(CX, CY), 80f, lc);
        _FRect(root, "CD", lc, new Vector2(CX - 4, CY - 4), new Vector2(8, 8));
        _PRct(root, "PL", new Vector2(0, PA_Y),     new Vector2(PA_W, PA_H), lc, lw);
        _PRct(root, "PR", new Vector2(W - PA_W, PA_Y), new Vector2(PA_W, PA_H), lc, lw);
        float gaw = 60f, gah = GH + 60f;
        _PRct(root, "GAL", new Vector2(0, CY - gah / 2f),    new Vector2(gaw, gah), lc, lw);
        _PRct(root, "GAR", new Vector2(W - gaw, CY - gah / 2f), new Vector2(gaw, gah), lc, lw);
        _FRect(root, "PDL", lc, new Vector2(176f, CY - 4), new Vector2(8, 8));
        _FRect(root, "PDR", lc, new Vector2(W - 184f, CY - 4), new Vector2(8, 8));

        var gnl = new ColorRect(); gnl.Name = "GNL"; gnl.Color = new Color(1, 1, 1, 0.10f);
        gnl.Position = new Vector2(-52, GT); gnl.Size = new Vector2(52, GH); gnl.ZIndex = -2;
        root.AddChild(gnl);
        var gnr = new ColorRect(); gnr.Name = "GNR"; gnr.Color = new Color(1, 1, 1, 0.10f);
        gnr.Position = new Vector2(W, GT); gnr.Size = new Vector2(52, GH); gnr.ZIndex = -2;
        root.AddChild(gnr);
        _FRect(root, "PTL", Colors.White, new Vector2(-4, GT - 3), new Vector2(8, 6));
        _FRect(root, "PBL", Colors.White, new Vector2(-4, GB - 3), new Vector2(8, 6));
        _FRect(root, "PTR", Colors.White, new Vector2(W - 4, GT - 3), new Vector2(8, 6));
        _FRect(root, "PBR", Colors.White, new Vector2(W - 4, GB - 3), new Vector2(8, 6));
    }

    private void _BuildWalls(Node root)
    {
        var walls = new Node2D(); walls.Name = "Walls"; root.AddChild(walls);
        _SW(walls, "WT",  new Vector2(CX, -15),      new Vector2(W + 200, 20));
        _SW(walls, "WB",  new Vector2(CX, H + 15),   new Vector2(W + 200, 20));
        _SW(walls, "GBL", new Vector2(-65, CY),       new Vector2(30, GH));
        _SW(walls, "GBR", new Vector2(W + 65, CY),   new Vector2(30, GH));
        _SW(walls, "PL1", new Vector2(-20, GT / 2f),          new Vector2(40, GT));
        _SW(walls, "PL2", new Vector2(-20, GB + (H - GB) / 2f), new Vector2(40, H - GB));
        _SW(walls, "PR1", new Vector2(W + 20, GT / 2f),       new Vector2(40, GT));
        _SW(walls, "PR2", new Vector2(W + 20, GB + (H - GB) / 2f), new Vector2(40, H - GB));
    }

    private void _BuildGoalTriggers(Node root)
    {
        var gl = new Area2D(); gl.Name = "GoalLeft"; gl.UniqueNameInOwner = true;
        gl.CollisionLayer = 0; gl.CollisionMask = 4;
        gl.Position = new Vector2(-40, CY);
        var glc = new CollisionShape2D(); var glr = new RectangleShape2D();
        glr.Size = new Vector2(80, GH); glc.Shape = glr; gl.AddChild(glc);
        root.AddChild(gl);

        var gr = new Area2D(); gr.Name = "GoalRight"; gr.UniqueNameInOwner = true;
        gr.CollisionLayer = 0; gr.CollisionMask = 4;
        gr.Position = new Vector2(W + 40, CY);
        var grc = new CollisionShape2D(); var grr = new RectangleShape2D();
        grr.Size = new Vector2(80, GH); grc.Shape = grr; gr.AddChild(grc);
        root.AddChild(gr);
    }

    private void _BuildBall(Node root)
    {
        var ball = new Football(); ball.Name = "Ball";
        ball.Position = new Vector2(CX, CY);
        ball.GravityScale = 0f; ball.LinearDamp = 2.2f; ball.AngularDamp = 6f;
        ball.CollisionLayer = 4; ball.CollisionMask = 16;
        var bc = new CollisionShape2D(); var br = new CircleShape2D(); br.Radius = 10f;
        bc.Shape = br; ball.AddChild(bc);
        var bs = new Sprite2D(); bs.Texture = GD.Load<Texture2D>("res://assets/img/football.png");
        bs.Scale = new Vector2(0.14f, 0.14f); ball.AddChild(bs);
        root.AddChild(ball);
    }

    private void _BuildCamera(Node root)
    {
        var cam = new Camera2D(); cam.Name = "MatchCamera"; cam.UniqueNameInOwner = true;
        cam.Position = new Vector2(CX, CY);
        cam.LimitLeft = 0; cam.LimitRight = (int)W;
        cam.LimitTop = 0; cam.LimitBottom = (int)H;
        cam.PositionSmoothingEnabled = true; cam.PositionSmoothingSpeed = 5f;
        root.AddChild(cam);
    }

    private void _BuildFP(Node parent, string name, bool isRed, Vector2 pos,
        string role, int slot, float sx, float sy)
    {
        var p = new FieldPlayer(); p.Name = name; p.Position = pos;
        p.SetMeta("team", isRed ? "Red" : "Blue"); p.SetMeta("role", role);
        p.SetMeta("slot_index", slot); p.SetMeta("slot_x", sx); p.SetMeta("slot_y", sy);
        p.SetMeta("start_pos", pos);
        var col = new CollisionShape2D(); var cap = new CapsuleShape2D();
        cap.Radius = 14f; cap.Height = 28f; col.Shape = cap; p.AddChild(col);
        var sp = new Sprite2D();
        sp.Texture = GD.Load<Texture2D>(isRed
            ? "res://assets/img/player_sprite.png" : "res://assets/img/npc_blue.png");
        sp.Scale = new Vector2(0.16f, 0.16f); sp.Position = new Vector2(0, -6); p.AddChild(sp);
        parent.AddChild(p);
    }

    private void _BuildGK(Node parent, string name, bool isRed, Vector2 pos)
    {
        var gk = new GoalkeeperAI(); gk.Name = name; gk.Position = pos;
        gk.SetMeta("team", isRed ? "Red" : "Blue"); gk.SetMeta("start_pos", pos);
        var col = new CollisionShape2D(); var cap = new CapsuleShape2D();
        cap.Radius = 14f; cap.Height = 28f; col.Shape = cap; gk.AddChild(col);
        var sp = new Sprite2D();
        sp.Texture = GD.Load<Texture2D>(isRed
            ? "res://assets/img/player_sprite.png" : "res://assets/img/npc_blue.png");
        sp.Scale = new Vector2(0.16f, 0.16f); sp.Position = new Vector2(0, -6);
        sp.Modulate = isRed ? new Color(1f, 0.7f, 0.2f) : new Color(0.7f, 0.7f, 1f);
        gk.AddChild(sp);
        parent.AddChild(gk);
    }

    private void _BuildHUD(Node root)
    {
        var hud = new CanvasLayer(); hud.Name = "HUD"; hud.Layer = 10; root.AddChild(hud);
        var topBar = new ColorRect(); topBar.Color = new Color(0, 0, 0, 0.60f);
        topBar.SetAnchorsPreset(Control.LayoutPreset.TopWide); topBar.Size = new Vector2(1280, 44);
        hud.AddChild(topBar);
        var score = new Label(); score.Name = "ScoreLabel"; score.UniqueNameInOwner = true;
        score.Text = "0  -  0"; score.SetAnchorsPreset(Control.LayoutPreset.TopWide);
        score.HorizontalAlignment = HorizontalAlignment.Center; score.Position = new Vector2(0, 4);
        score.AddThemeFontSizeOverride("font_size", 28);
        score.AddThemeColorOverride("font_color", Colors.White); hud.AddChild(score);
        var timer = new Label(); timer.Name = "TimerLabel"; timer.UniqueNameInOwner = true;
        timer.Text = "00:00"; timer.SetAnchorsPreset(Control.LayoutPreset.TopRight);
        timer.Position = new Vector2(-110, 8); timer.AddThemeFontSizeOverride("font_size", 22);
        timer.AddThemeColorOverride("font_color", new Color(1f, 0.9f, 0.4f)); hud.AddChild(timer);
        var stateL = new Label(); stateL.Name = "StateLabel"; stateL.UniqueNameInOwner = true;
        stateL.Text = ""; stateL.SetAnchorsPreset(Control.LayoutPreset.Center);
        stateL.HorizontalAlignment = HorizontalAlignment.Center;
        stateL.Position = new Vector2(-200, -40); stateL.CustomMinimumSize = new Vector2(400, 0);
        stateL.AddThemeFontSizeOverride("font_size", 22);
        stateL.AddThemeColorOverride("font_color", new Color(1f, 0.9f, 0.2f)); hud.AddChild(stateL);
        var botBar = new ColorRect(); botBar.Color = new Color(0, 0, 0, 0.55f);
        botBar.SetAnchorsPreset(Control.LayoutPreset.BottomWide);
        botBar.Position = new Vector2(0, -52); botBar.Size = new Vector2(1280, 52);
        hud.AddChild(botBar);
        var staminaBar = new ProgressBar(); staminaBar.Name = "StaminaBar";
        staminaBar.UniqueNameInOwner = true; staminaBar.MinValue = 0;
        staminaBar.MaxValue = 100; staminaBar.Value = 100;
        staminaBar.SetAnchorsPreset(Control.LayoutPreset.BottomLeft);
        staminaBar.Position = new Vector2(10, -34); staminaBar.Size = new Vector2(200, 20);
        hud.AddChild(staminaBar);
        var slbl = new Label(); slbl.Text = "Kondisyon";
        slbl.SetAnchorsPreset(Control.LayoutPreset.BottomLeft);
        slbl.Position = new Vector2(10, -50); slbl.AddThemeFontSizeOverride("font_size", 12);
        slbl.AddThemeColorOverride("font_color", new Color(0.9f, 0.9f, 0.9f)); hud.AddChild(slbl);
        var powerBar = new ProgressBar(); powerBar.Name = "PowerBar";
        powerBar.UniqueNameInOwner = true; powerBar.MinValue = 0;
        powerBar.MaxValue = 100; powerBar.Value = 0;
        powerBar.SetAnchorsPreset(Control.LayoutPreset.BottomWide);
        powerBar.Position = new Vector2(490, -34); powerBar.Size = new Vector2(300, 20);
        hud.AddChild(powerBar);
        var plbl = new Label(); plbl.Text = "Sut Gucu";
        plbl.SetAnchorsPreset(Control.LayoutPreset.BottomWide);
        plbl.HorizontalAlignment = HorizontalAlignment.Center;
        plbl.Position = new Vector2(0, -50); plbl.AddThemeFontSizeOverride("font_size", 12);
        plbl.AddThemeColorOverride("font_color", new Color(1f, 0.8f, 0.2f)); hud.AddChild(plbl);
        var ctrl = new Label();
        ctrl.Text = "WASD:Hareket  SHIFT:Sprint  F:Pas  SPACE(tut):Sut  E:Faul";
        ctrl.SetAnchorsPreset(Control.LayoutPreset.BottomLeft);
        ctrl.Position = new Vector2(220, -50); ctrl.AddThemeFontSizeOverride("font_size", 11);
        ctrl.AddThemeColorOverride("font_color", new Color(0.8f, 0.8f, 0.8f)); hud.AddChild(ctrl);
        var miniMap = new MiniMap(); miniMap.Name = "MiniMap"; miniMap.UniqueNameInOwner = true;
        miniMap.SetAnchorsPreset(Control.LayoutPreset.BottomRight);
        miniMap.Position = new Vector2(-252, -104); miniMap.Size = new Vector2(240, 96);
        hud.AddChild(miniMap);
    }

    private static void _L(Node p, string n, Vector2 a, Vector2 b, Color c, float w)
    {
        var l = new Line2D(); l.Name = n; l.DefaultColor = c; l.Width = w; l.ZIndex = -3;
        l.AddPoint(a); l.AddPoint(b); p.AddChild(l);
    }

    private static void _Circle(Node p, string n, Vector2 center, float r, Color c)
    {
        var l = new Line2D(); l.Name = n; l.DefaultColor = c; l.Width = 3f; l.ZIndex = -3;
        int seg = 32;
        for (int i = 0; i <= seg; i++)
        {
            float a = i / (float)seg * Mathf.Tau;
            l.AddPoint(center + new Vector2(Mathf.Cos(a) * r, Mathf.Sin(a) * r));
        }
        p.AddChild(l);
    }

    private static void _PRct(Node p, string n, Vector2 pos, Vector2 size, Color c, float lw)
    {
        _L(p, n + "T", pos, pos + new Vector2(size.X, 0), c, lw);
        _L(p, n + "B", pos + new Vector2(0, size.Y), pos + size, c, lw);
        _L(p, n + "L", pos, pos + new Vector2(0, size.Y), c, lw);
        _L(p, n + "R", pos + new Vector2(size.X, 0), pos + size, c, lw);
    }

    private static void _FRect(Node p, string n, Color c, Vector2 pos, Vector2 size)
    {
        var r = new ColorRect(); r.Name = n; r.Color = c;
        r.Position = pos; r.Size = size; r.ZIndex = -1; p.AddChild(r);
    }

    private static void _SW(Node parent, string name, Vector2 center, Vector2 size)
    {
        var sb = new StaticBody2D(); sb.Name = name;
        sb.CollisionLayer = 16; sb.CollisionMask = 4;
        var col = new CollisionShape2D(); var sh = new RectangleShape2D();
        sh.Size = size; col.Shape = sh; col.Position = center;
        sb.AddChild(col); parent.AddChild(sb);
    }
}
