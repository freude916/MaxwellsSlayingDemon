using Godot;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Screens;

namespace MaxwellMod.Stash;

/// <summary>
///     暂存区 UI 按钮
///     显示暂存区卡牌数量，点击可查看暂存的牌
/// </summary>
public partial class NStashButton : Button
{
    private const double _animDuration = 0.5;
    private static readonly Vector2 _hideOffset = new(150f, 0f);
    private static readonly Vector2 _hoverScale = Vector2.One * 1.25f;
    private Tween? _bumpTween;
    private Label? _countLabel;
    protected Vector2 _hidePosition = new(-160f, 860f);
    private Control? _icon;
    private Player? _localPlayer;

    private Tween? _positionTween;

    private Vector2 _posOffset;
    protected Vector2 _showPosition = new(100f, 828f);

    public CardPile? StashPile { get; private set; }

    public override void _Ready()
    {
        try
        {
            GD.Print("[MaxwellMod] NStashButton _Ready called");

            _icon = GetNode<Control>("Icon");
            _countLabel = GetNode<Label>("CountContainer/Count");

            Visible = false;
            SetAnimInOutPositions();
            Disabled = true;

            // 连接按钮信号
            Pressed += OnPressed;
            MouseEntered += OnMouseEntered;
            MouseExited += OnMouseExited;

            GD.Print("[MaxwellMod] NStashButton _Ready complete");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[MaxwellMod] Error in NStashButton._Ready: {ex}");
        }
    }

    public void Initialize(Player player, CardPile stashPile)
    {
        try
        {
            GD.Print("[MaxwellMod] Initializing NStashButton");

            _localPlayer = player;
            StashPile = stashPile;

            // 订阅牌堆事件
            StashPile.CardAddFinished += OnCardAdded;
            StashPile.CardRemoveFinished += OnCardRemoved;

            UpdateCount();

            // 如果暂存区有牌，显示按钮
            if (StashPile.Cards.Count > 0)
            {
                GD.Print($"[MaxwellMod] Stash has {StashPile.Cards.Count} cards, showing button");
                Visible = true;
                Position = _showPosition;
                Disabled = false;
            }
            else
            {
                GD.Print("[MaxwellMod] Stash is empty, keeping button hidden");
            }

            GD.Print("[MaxwellMod] NStashButton initialization complete");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[MaxwellMod] Error in Initialize: {ex}");
        }
    }

    private void OnCardAdded()
    {
        GD.Print($"[MaxwellMod] OnCardAdded called! Stash count: {StashPile?.Cards.Count}");
        UpdateCount();

        // 添加卡牌时动画显示
        AnimIn();
        Disabled = false;

        GD.Print($"[MaxwellMod] OnCardAdded complete. Visible: {Visible}, Disabled: {Disabled}");
    }

    private void OnCardRemoved()
    {
        GD.Print($"[MaxwellMod] OnCardRemoved called! Stash count: {StashPile?.Cards.Count}");
        UpdateCount();

        if (StashPile?.Cards.Count == 0)
        {
            GD.Print("[MaxwellMod] Stash empty, calling AnimOut()");
            AnimOut();
        }
        else
        {
            GD.Print($"[MaxwellMod] Stash still has {StashPile.Cards.Count} cards, keeping button visible");
        }
    }

    private void UpdateCount()
    {
        if (_countLabel != null && StashPile != null) _countLabel.Text = StashPile.Cards.Count.ToString();
    }

    protected void SetAnimInOutPositions()
    {
        var viewport = GetViewport();
        if (viewport != null)
        {
            _posOffset = new Vector2(OffsetRight + 100f, 0f - OffsetBottom + 90f);
            var viewportSize = viewport.GetVisibleRect().Size;
            _showPosition = viewportSize - _posOffset;
            _hidePosition = _showPosition + _hideOffset;
        }
    }

    public void AnimIn()
    {
        Visible = true;
        _positionTween?.Kill();
        _positionTween = CreateTween();
        _positionTween.TweenProperty(this, "position", _showPosition, _animDuration)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Expo);
    }

    public void AnimOut()
    {
        _positionTween?.Kill();
        _positionTween = CreateTween();
        _positionTween.TweenProperty(this, "position", _hidePosition, _animDuration)
            .SetEase(Tween.EaseType.In)
            .SetTrans(Tween.TransitionType.Expo);
        _positionTween.Finished += () => { Visible = false; };
        Disabled = true;
    }

    private void OnPressed()
    {
        // 点击动画
        _bumpTween?.Kill();
        _bumpTween = CreateTween();
        if (_icon != null) _bumpTween.TweenProperty(_icon, "scale", Vector2.One, 0.05);

        // 显示暂存区牌堆界面
        if (StashPile != null && !StashPile.IsEmpty)
            // 使用原版卡牌堆查看器
            NCardPileScreen.ShowScreen(
                StashPile,
                new string[] { MegaInput.cancel }
            );
    }

    private void OnMouseEntered()
    {
        if (_icon != null && !Disabled)
        {
            var tween = CreateTween();
            tween.TweenProperty(_icon, "scale", _hoverScale, 0.1);
        }
    }

    private void OnMouseExited()
    {
        if (_icon != null)
        {
            var tween = CreateTween();
            tween.TweenProperty(_icon, "scale", Vector2.One, 0.1);
        }
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        if (StashPile != null)
        {
            StashPile.CardAddFinished -= OnCardAdded;
            StashPile.CardRemoveFinished -= OnCardRemoved;
        }

        _positionTween?.Kill();
        _bumpTween?.Kill();
    }
}