# Copies the chosen raw captures (Docs/Walkthrough/raw, from the bot) to Docs/Walkthrough/shots with walkthrough names.
# Plain file copies: the pictures are not edited.
import os, shutil
R = os.path.join(os.path.dirname(__file__), "..", "..", "Docs", "Walkthrough"); raw, out = os.path.join(R, "raw"), os.path.join(R, "shots")
MAP = {
 "L0_01_menu.png": "menu.png", "L0_02_controls.png": "w-menu-controls.png",
 "L0_03_loading_30.png": "s2-loading-1-30pct.png", "L0_03_loading_60.png": "s2-loading-1-60pct.png", "L0_04_loading_100.png": "s2-loading-1-100pct.png",
 "L0_05_hud_goal.png": "w-hud-goal.png", "L0_06_journal.png": "w-journal.png", "L0_07_map.png": "w-map.png", "L0_08_guide.png": "w-guide.png",
 "L0_09_pause.png": "w-pause.png", "L0_09b_evidence_board.png": "w-evidence-board.png",
 "L0_10_intro_jeep.png": "s7-intro-10-04b_low_front.png", "L0_11_intro_side.png": "s7-intro-12-04d_side_tracking.png", "L0_12_intro_meera.png": "s7-intro-17-05b_two_shot.png",
 "L1_01_road_to_gate.png": "w-hud-goal.png", "L1_02_priest_talk.png": "v3s-talk-priest-start.png", "L1_03_goal_card.png": "v3s-02-goal-card-new.png",
 "L2_01_courier_sets_off.png": "v3z1-01-courier-sets-off.png", "L2_02_the_yard.png": "v3z1-03-the-yard.png", "L2_03_fight_hud.png": "v3z1-04-street-fight-hud.png", "L2_04_fight.png": "v3z1-04-street-fight.png",
 "L3_01_storeroom_wide.png": "v3z2-02-storeroom-wide.png", "L3_02_plinth_and_lantern.png": "v3z2-03-plinth-and-shard.png", "L3_03_pot_whole_lantern_lit.png": "v3z2-04-pot-whole-lantern-lit.png",
 "L3_04_lantern_on_stain.png": "v3z2-05-lantern-on-stain-grille-shadows.png", "L3_05_fight_lookout.png": "v3z2-07b-fight-lookout.png", "L3_06_chronicle_recovered.png": "v3z2-08-recovered-banner.png",
 "L4_01_arrive.png": "v3z3-01-arrive-gameplay-camera.png", "L4_02_blocks_on_plates.png": "v3z3-02-blocks-on-plates-gate-open.png", "L4_03_light_pattern.png": "v3z3-03-light-pattern-and-discs.png",
 "L4_04_discs_solved.png": "v3z3-04-discs-solved-chest-open.png", "L4_05_fight_guards.png": "v3z3-05-fight-vault-guards.png",
 "L5_01_arrive.png": "v3z4-01-arrive-gameplay-camera.png", "L5_02_tank_empty.png": "v3z4-02-tank-empty.png", "L5_03_tank_full.png": "v3z4-03-tank-full-panel-under-water.png",
 "L5_04_water_on_mark.png": "v3z4-04-water-on-the-long-mark.png", "L5_05_chamber.png": "v3z4-05-chamber-vessel.png", "L5_06_fight_knife_man.png": "v3z4-06-fight-knife-man-hud.png",
 "L6_01_plaza.png": "v3z5-02-plaza-before.png", "L6_02_mirrors_set.png": "v3z5-03-mirrors-set-spoke-blocks.png", "L6_03_beams_reach_wall.png": "v3z5-04-both-beams-reach-the-wall.png",
 "L6_04_door_open.png": "v3z5-05-hidden-door-open.png", "L6_05_fight_kishore.png": "v3z5-06-fight-kishore-hud.png",
 "L7_01_jungle_road.png": "v3z6-01-jungle-road.png", "L7_02_chase.png": "v3z6-02-chase.png", "L7_03_ruin_court.png": "v3z6-03-ruin-court.png", "L7_04_wave1.png": "v3z6-04-wave-1.png",
 "L7_05_wave2.png": "v3z6-05-wave-2-hud.png", "L7_06_bhaskar.png": "v3z6-06-bhaskar.png", "L7_07_madhav_gives_up.png": "v3z6-07-madhav-gives-up.png",
 "E_01_return.png": "v3s-talk-priest-return.png", "E_02_ending.png": "v3s-09-ending.png", "E_03_ending_final.png": "w-ending-final.png",
 "C_01_aryan.png": "w-char-aryan.png", "C_02_pujari.png": "w-char-pujari.png", "C_03_meera.png": "w-char-meera.png", "C_04_madhav.png": "v3m-madhav-storeroom.png",
 "C_05_robbers.png": "v3z1-04-street-fight.png", "C_06_kishore.png": "v3z5-06-fight-kishore-hud.png", "J_01_jeep.png": "w-jeep.png", "K_01_fight_hud.png": "v3z1-04-street-fight-hud.png",
}
os.makedirs(out, exist_ok=True); missing = []
for new, src in MAP.items():
    p = os.path.join(raw, src)
    if os.path.exists(p): shutil.copyfile(p, os.path.join(out, new))
    else: missing.append(f"{new} <- {src}")
print("copied", len(MAP) - len(missing), "missing:", missing)
