#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import os
import re
import shutil
import sys
from collections import defaultdict
from dataclasses import dataclass
from pathlib import Path


POOL_CARD_RE = re.compile(r"ModelDb\.Card<([A-Za-z0-9_]+)>\(\)")
CARD_TYPE_RE = re.compile(r"CardType\.([A-Za-z0-9_]+)")
CARD_RARITY_RE = re.compile(r"CardRarity\.([A-Za-z0-9_]+)")
SPECIAL_ROLES_WITHOUT_RARITY = {"Curse", "Quest"}


@dataclass(frozen=True)
class CardInfo:
    card_type: str | None
    card_rarity: str | None


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description=(
            "Extract cards from CardPools and classify cards in .reference by "
            "role/type/rarity."
        )
    )
    parser.add_argument(
        "--reference-root",
        default=".reference/CardExample",
        help="Root directory containing MegaCrit.Sts2.Core.Models.CardPools and Cards",
    )
    parser.add_argument(
        "--output",
        default=".reference/card_classified_by_role_type_rarity.json",
        help="Output JSON path",
    )
    parser.add_argument(
        "--path-root",
        default=".reference/card_classified_by_role_type_rarity_files",
        help=(
            "Output root directory for filesystem classification tree "
            "(role/type/rarity or role/type for Curse/Quest)"
        ),
    )
    parser.add_argument(
        "--materialize-mode",
        choices=["symlink", "copy"],
        default="symlink",
        help="How to place card class files in path tree",
    )
    parser.add_argument(
        "--skip-path-tree",
        action="store_true",
        help="Only write JSON, do not generate directory tree",
    )
    parser.add_argument(
        "--include-mock-pool",
        action="store_true",
        help="Include MockCardPool cards",
    )
    parser.add_argument(
        "--include-deprecated-pool",
        action="store_true",
        help="Include DeprecatedCardPool cards",
    )
    return parser.parse_args()


def pool_role_name(pool_file: Path) -> str:
    stem = pool_file.stem
    if stem.endswith("CardPool"):
        return stem[:-8]
    return stem


def parse_pools(
    pools_dir: Path,
    include_mock_pool: bool,
    include_deprecated_pool: bool,
) -> dict[str, list[str]]:
    role_to_cards: dict[str, list[str]] = {}
    skip_files = set()
    if not include_mock_pool:
        skip_files.add("MockCardPool.cs")
    if not include_deprecated_pool:
        skip_files.add("DeprecatedCardPool.cs")

    for pool_file in sorted(pools_dir.glob("*CardPool.cs")):
        if pool_file.name in skip_files:
            continue

        text = pool_file.read_text(encoding="utf-8")
        cards = POOL_CARD_RE.findall(text)
        if not cards:
            continue

        role = pool_role_name(pool_file)
        # Keep pool order while de-duplicating.
        seen: set[str] = set()
        ordered_cards: list[str] = []
        for card in cards:
            if card in seen:
                continue
            seen.add(card)
            ordered_cards.append(card)
        role_to_cards[role] = ordered_cards

    return role_to_cards


def extract_constructor_base_args(card_name: str, text: str) -> str:
    constructor_pattern = re.compile(
        rf"\b{re.escape(card_name)}\s*\([^)]*\)\s*:\s*base\s*\((.*?)\)\s*(?:\{{|=>)",
        re.DOTALL,
    )
    match = constructor_pattern.search(text)
    if not match:
        return ""
    return match.group(1)


def parse_card_file(card_file: Path) -> CardInfo:
    text = card_file.read_text(encoding="utf-8")
    card_name = card_file.stem
    base_args = extract_constructor_base_args(card_name, text)

    if base_args:
        card_type_match = CARD_TYPE_RE.search(base_args)
        card_rarity_match = CARD_RARITY_RE.search(base_args)
    else:
        # Fallback for unusual formatting.
        card_type_match = CARD_TYPE_RE.search(text)
        card_rarity_match = CARD_RARITY_RE.search(text)

    return CardInfo(
        card_type_match.group(1) if card_type_match else None,
        card_rarity_match.group(1) if card_rarity_match else None,
    )


def parse_cards(cards_dir: Path) -> dict[str, CardInfo]:
    card_info: dict[str, CardInfo] = {}
    for card_file in sorted(cards_dir.glob("*.cs")):
        card_info[card_file.stem] = parse_card_file(card_file)
    return card_info


def build_classification(
    role_to_cards: dict[str, list[str]],
    card_info: dict[str, CardInfo],
) -> dict[str, object]:
    # Normal roles: role -> type -> rarity -> [cards]
    # Special roles (Curse/Quest): role -> type -> [cards]
    output: dict[str, object] = {}

    for role in sorted(role_to_cards):
        cards = role_to_cards[role]

        if role in SPECIAL_ROLES_WITHOUT_RARITY:
            role_bucket: dict[str, list[str]] = defaultdict(list)
            for card in cards:
                info = card_info.get(card, CardInfo(None, None))
                card_type = info.card_type or "UnknownType"
                role_bucket[card_type].append(card)

            output[role] = {
                card_type: sorted(card_list)
                for card_type, card_list in sorted(role_bucket.items())
            }
            continue

        role_bucket_nested: dict[str, dict[str, list[str]]] = defaultdict(
            lambda: defaultdict(list)
        )
        for card in cards:
            info = card_info.get(card, CardInfo(None, None))
            card_type = info.card_type or "UnknownType"
            card_rarity = info.card_rarity or "UnknownRarity"
            role_bucket_nested[card_type][card_rarity].append(card)

        output[role] = {
            card_type: {
                rarity: sorted(card_list)
                for rarity, card_list in sorted(rarity_map.items())
            }
            for card_type, rarity_map in sorted(role_bucket_nested.items())
        }

    return output


def write_path_tree(
    role_to_cards: dict[str, list[str]],
    card_info: dict[str, CardInfo],
    cards_dir: Path,
    path_root: Path,
    materialize_mode: str,
) -> tuple[int, int]:
    if path_root.exists():
        shutil.rmtree(path_root)
    path_root.mkdir(parents=True, exist_ok=True)

    written = 0
    missing = 0

    for role in sorted(role_to_cards):
        for card in role_to_cards[role]:
            source_file = cards_dir / f"{card}.cs"
            if not source_file.exists():
                missing += 1
                continue

            info = card_info.get(card, CardInfo(None, None))
            card_type = info.card_type or "UnknownType"
            card_rarity = info.card_rarity or "UnknownRarity"

            if role in SPECIAL_ROLES_WITHOUT_RARITY:
                target_dir = path_root / role / card_type
            else:
                target_dir = path_root / role / card_type / card_rarity
            target_dir.mkdir(parents=True, exist_ok=True)

            target_file = target_dir / source_file.name
            if materialize_mode == "copy":
                shutil.copy2(source_file, target_file)
            else:
                rel_source = os.path.relpath(source_file, target_dir)
                target_file.symlink_to(rel_source)
            written += 1

    return written, missing


def main() -> int:
    args = parse_args()
    reference_root = Path(args.reference_root)
    pools_dir = reference_root / "MegaCrit.Sts2.Core.Models.CardPools"
    cards_dir = reference_root / "MegaCrit.Sts2.Core.Models.Cards"
    output_path = Path(args.output)
    path_root = Path(args.path_root)

    if not pools_dir.is_dir():
        print(f"Pool directory not found: {pools_dir}", file=sys.stderr)
        return 1

    if not cards_dir.is_dir():
        print(f"Cards directory not found: {cards_dir}", file=sys.stderr)
        return 1

    role_to_cards = parse_pools(
        pools_dir=pools_dir,
        include_mock_pool=args.include_mock_pool,
        include_deprecated_pool=args.include_deprecated_pool,
    )
    card_info = parse_cards(cards_dir)
    classification = build_classification(role_to_cards, card_info)

    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(
        json.dumps(classification, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
    )

    path_tree_note = "path tree skipped"
    if not args.skip_path_tree:
        written, missing = write_path_tree(
            role_to_cards=role_to_cards,
            card_info=card_info,
            cards_dir=cards_dir,
            path_root=path_root,
            materialize_mode=args.materialize_mode,
        )
        path_tree_note = (
            f"path_tree={path_root} ({args.materialize_mode}) "
            f"| tree_files={written} | missing_sources={missing}"
        )

    total_cards = sum(len(cards) for cards in role_to_cards.values())
    print(
        f"Wrote {output_path} | roles={len(role_to_cards)} "
        f"| pooled_cards={total_cards} | parsed_card_files={len(card_info)} "
        f"| {path_tree_note}"
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
