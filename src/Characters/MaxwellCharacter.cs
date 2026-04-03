using BaseLib.Abstracts;
using Godot;
using MaxwellMod.Cards;
using MaxwellMod.PatchesNModels;
using MaxwellMod.Relics;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;

namespace MaxwellMod.Characters;

/// <summary>
///     Maxwell 人物定义
/// </summary>
public class MaxwellCharacter : PlaceholderCharacterModel
{
    // 角色名称颜色
    public override Color NameColor => new(0.1f, 0.14f, 0.49f);

    // 能量图标轮廓颜色
    public override Color EnergyLabelOutlineColor => new(0.1f, 0.1f, 0.5f);

    // 人物性别
    public override CharacterGender Gender => CharacterGender.Masculine;

    // 初始血量
    public override int StartingHp => 80;

    // 人物模型tscn路径
    public override string CustomVisualPath => "res://MaxwellMod/scenes/maxwell_character.tscn";

    // 卡牌拖尾场景 (使用默认)
    // public override string CustomTrailPath => "res://scenes/vfx/card_trail_ironclad.tscn";
    // 人物头像路径
    public override string CustomIconTexturePath => "res://MaxwellMod/images/ui/maxwell_icon.png";

    // 人物头像2号 (使用默认)
    // public override string CustomIconPath => "res://scenes/ui/character_icons/ironclad_icon.tscn";
    // 能量表盘tscn路径
    public override string CustomEnergyCounterPath => "res://MaxwellMod/scenes/maxwell_energy_counter.tscn";

    // 篝火休息场景 (使用默认)
    // public override string CustomRestSiteAnimPath => "res://scenes/rest_site/characters/ironclad_rest_site.tscn";
    // 商店人物场景
    public override string CustomMerchantAnimPath => "res://MaxwellMod/scenes/maxwell_merchant.tscn";
    // 多人模式-手指 (使用默认)
    // public override string CustomArmPointingTexturePath => null;
    // 多人模式剪刀石头布-石头 (使用默认)
    // public override string CustomArmRockTexturePath => null;
    // 多人模式剪刀石头布-布 (使用默认)
    // public override string CustomArmPaperTexturePath => null;
    // 多人模式剪刀石头布-剪刀 (使用默认)
    // public override string CustomArmScissorsTexturePath => null;

    // 人物选择背景
    public override string CustomCharacterSelectBg => "res://MaxwellMod/scenes/maxwell_select_bg.tscn";

    // 人物选择图标
    public override string CustomCharacterSelectIconPath => "res://MaxwellMod/images/char_select_maxwell.png";

    // 人物选择图标-锁定状态
    public override string CustomCharacterSelectLockedIconPath =>
        "res://MaxwellMod/images/char_select_maxwell_locked.png";

    // 人物选择过渡动画 (使用默认)
    // public override string CustomCharacterSelectTransitionPath => "res://materials/transitions/ironclad_transition_mat.tres";
    // 地图上的角色标记图标、表情轮盘上的角色头像 (使用默认)
    // public override string CustomMapMarkerPath => null;
    // 攻击音效 (使用默认)
    // public override string CustomAttackSfx => null;
    // 施法音效 (使用默认)
    // public override string CustomCastSfx => null;
    // 死亡音效 (使用默认)
    // public override string CustomDeathSfx => null;
    // 角色选择音效 (使用默认)
    // public override string CharacterSelectSfx => null;
    // 过渡音效 (不能删)
    public override string CharacterTransitionSfx => "event:/sfx/ui/wipe_ironclad";

    public override CardPoolModel CardPool => ModelDb.CardPool<MaxwellCardPool>();
    public override RelicPoolModel RelicPool => ModelDb.RelicPool<MaxwellRelicPool>();
    public override PotionPoolModel PotionPool => ModelDb.PotionPool<MaxwellPotionPool>();

    // 初始卡组
    public override IEnumerable<CardModel> StartingDeck =>
    [
        ModelDb.Card<Strike>(),
        ModelDb.Card<Strike>(),
        ModelDb.Card<Strike>(),
        ModelDb.Card<Strike>(),
        ModelDb.Card<Defend>(),
        ModelDb.Card<Defend>(),
        ModelDb.Card<Defend>(),
        ModelDb.Card<Defend>(),
        ModelDb.Card<HeatSource>(),
        ModelDb.Card<ColdSource>()
    ];

    // 初始遗物
    public override IReadOnlyList<RelicModel> StartingRelics =>
    [
        ModelDb.Relic<RecordingEye>()
    ];

    // 攻击建筑师的攻击特效列表
    public override List<string> GetArchitectAttackVfx()
    {
        return
        [
            "vfx/vfx_attack_blunt",
            "vfx/vfx_heavy_blunt",
            "vfx/vfx_attack_slash",
            "vfx/vfx_bloody_impact",
            "vfx/vfx_rock_shatter"
        ];
    }
}