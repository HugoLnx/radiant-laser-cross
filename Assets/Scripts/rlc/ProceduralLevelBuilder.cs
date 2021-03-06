﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

namespace rlc
{

    public enum GameOverReason
    {
        destroyed, timeout, hard_reset
    }

    /* Build and present levels.
     * Build levels as sequences of waves to play.
    */
    public class ProceduralLevelBuilder : MonoBehaviour
    {
        const string DEFAULT_TITLE = "RADIANT LASER CROSS";

        public List<Wave> waves_lvl_1_easy = new List<Wave>();
        public List<Wave> waves_lvl_2_challenging = new List<Wave>();
        public List<Wave> waves_lvl_3_hard = new List<Wave>();
        public List<Wave> waves_lvl_4_hardcore = new List<Wave>();

        public List<Wave> boss_lvl_1_challenging = new List<Wave>();
        public List<Wave> boss_lvl_2_hard = new List<Wave>();
        public List<Wave> boss_lvl_3_hardcore = new List<Wave>();

        public int end_level = 4;
        private int current_level_number = 1;
        private int current_wave_number = 1;
        private List<WaveInfo> current_level_waves_selection;

        public UnityEngine.Object laser_cross_prefab;
        public Color default_background_color;

        public Text progress_display;
        public Text title_display;
        public float default_title_display_duration_secs = 5.0f;
        public float title_display_duration_secs = 3.0f;

        private TimeoutSystem timeout;
        private IEnumerator timeout_gameover_display;
        private float timeout_gameover_deplay = 3.0f;

        private int title_display_count = 0;
        private int wave_start_count = 0;

        public enum State
        {
            ready, playing_wave, game_over
        }
        private State state = State.ready;
        private Wave current_wave;
        private IEnumerator<LevelStatus> level_progression;

        private enum WaveCategory
        {
            Wave, Boss
        }

        private class WaveInfo
        {
            public Wave wave;
            public WaveCategory category;
        }

        // Use this for initialization
        void Start()
        {
            timeout = GetComponent<TimeoutSystem>();
            timeout.on_timeout = () => game_over_timeout();
            reset_all();
        }

        // Update is called once per frame--*
        void Update()
        {
            if (state == State.game_over)
            {
                reset_all();
            }
        }


        public void game_over(GameOverReason reason)
        {
            if (state != State.playing_wave)
                return;
            state = State.game_over;

            if (reason == GameOverReason.timeout)
            {
                if (timeout_gameover_display != null)
                    StopCoroutine(timeout_gameover_display);
                timeout_gameover_display = game_over_timeout_display();
                StartCoroutine(timeout_gameover_display);
            }
            else
            {
                timeout.stop();
            }
        }

        private void game_over_timeout()
        {
            if (LaserCross.current)
            {
                LaserCross.current.die(GameOverReason.timeout);
            }
        }

        private IEnumerator game_over_timeout_display()
        {
            timeout.show_timeout(0.0f);
            yield return new WaitForSeconds(timeout_gameover_deplay);
            timeout.hide_timeout();
        }

        private void reset_all()
        {
            Debug.Log("==== RESET ALL ====");
            // TODO: make a not crude version XD

            clear_wave();

            Bullet.clear_bullets_from_game();

            set_theme_color(default_background_color);

            level_progression = null;

            const string LASER_CROSS_OBJECT_NAME = "laser_cross";

            GameObject laser_cross = GameObject.Find(LASER_CROSS_OBJECT_NAME);

            if (laser_cross == null || !laser_cross.GetComponent<LaserCross>().life_control.is_alive())
            {
                if (laser_cross != null)
                {
                    Destroy(laser_cross);
                }

                laser_cross = (GameObject)GameObject.Instantiate(laser_cross_prefab, Vector3.zero, Quaternion.identity);
                laser_cross.name = LASER_CROSS_OBJECT_NAME;
            }

            StartCoroutine(display_title("", DEFAULT_TITLE, default_title_display_duration_secs));

            state = State.ready;
        }

        public void new_game()
        {
            if (state == State.playing_wave)
                return;

            level_progression = make_level_progression();
            next_wave();
        }

        public void next_wave() // TODO: call this automatically when a wave is finished
        {
            if (level_progression == null)
                return;

            level_progression.MoveNext();
            if (level_progression.Current == LevelStatus.next_level)
            {
                // TODO: Do something to clarify that we changed level
                level_progression.MoveNext();
            }
        }

        private WaveInfo pick_a_wave_in(IList<Wave> wave_bag, WaveCategory wave_category = WaveCategory.Wave)
        {
            if (wave_bag.Count == 0)
            {
                Debug.LogErrorFormat("No ennemies in enemy wave bag: {0}", wave_bag);
                return null;
            }
            var random_idx = Random.Range(0, wave_bag.Count);
            var picked_wave = wave_bag[random_idx];
            WaveInfo result = new WaveInfo();
            result.wave = picked_wave;
            result.category = wave_category;
            return result;
        }

        private void clear_wave()
        {
            if (current_wave != null) // TODO: remove the previous wave progressively/"smoothly"
            {
                Destroy(current_wave.gameObject);
            }
        }

        private IEnumerator start_wave(WaveInfo wave_info)
        {
            clear_wave();

            state = State.playing_wave;

            set_theme_color(wave_info.wave.background_color);
            string progress_title = string.Format("Level {0} {2}- Wave {1}", current_level_number, current_wave_number, wave_info.category == WaveCategory.Boss ? "- Boss " : "");

            int wave_start_idx = ++wave_start_count; // Keep track of which wave we were starting.

            timeout.stop();
            if (wave_info.wave.timeout_secs > 0)
            {
                timeout.show_timeout(wave_info.wave.timeout_secs);
            }

            yield return display_title(progress_title, wave_info.wave.title, title_display_duration_secs);

            if (wave_start_idx != wave_start_count) // If another wave was started in-betwen, do nothing.
                yield break;

            current_wave = Instantiate(wave_info.wave);

            current_wave.on_finished += wave => next_wave();

            if (current_wave.timeout_secs > 0)
            {
                timeout.start(current_wave.timeout_secs);
            }
        }

        private void set_theme_color(Color color)
        {
            // TODO: transition in a progressive way
            Camera.main.backgroundColor = color;
            RenderSettings.skybox.color = color;
            RenderSettings.skybox.SetColor("_Color", color);
            if (RenderSettings.skybox.HasProperty("_Tint"))
                RenderSettings.skybox.SetColor("_Tint", color);
            else if (RenderSettings.skybox.HasProperty("_SkyTint"))
                RenderSettings.skybox.SetColor("_SkyTint", color);
        }

        private IEnumerator display_title(string progress_text, string title_text, float duration_secs)
        {
            if (title_display == null || progress_display == null)
            {
                Debug.LogError("Incomplete text set to display the title!");
                yield break;
            }

            int title_display_idx = ++title_display_count; // Keep track of which title display request we correspond to.

            Debug.LogFormat("Progress: {0}", progress_text);
            Debug.LogFormat("Title: {0}", title_text);
            title_display.text = title_text;
            title_display.enabled = true;
            progress_display.text = progress_text;
            progress_display.enabled = true;

            yield return new WaitForSeconds(duration_secs);
            if (title_display_idx != title_display_count) // No other title display was launched in between, otherwise we do nothing.
                yield break;

            title_display.enabled = false;
            progress_display.enabled = false;
            Debug.Log("Title hidden");
        }


        private enum LevelStatus {
            next_wave,      // We'll play the next wave
            next_level,     // We'll play the next level
            finished        // End of level sequence reached! The player won!
        }

        private IEnumerator<LevelStatus> make_level_progression()
        {
            /* Notes: this is a coroutine (see usage of `yield` below).
             * It is called each time we need to progress through the list
             * of waves/levels. Each time `yield return <...>` is called,
             * it returns immediately the value but keeps track of where it
             * was in this function. Next time this function is caleld, it will
             * resume where it was.
             * */
            for (current_level_number = 1; current_level_number <= end_level; ++current_level_number)
            {
                current_level_waves_selection = build_level(current_level_number);
                yield return LevelStatus.next_level;

                current_wave_number = 0;
                foreach (WaveInfo wave_info in current_level_waves_selection)
                {
                    ++current_wave_number;
                    StartCoroutine(start_wave(wave_info));
                    yield return LevelStatus.next_wave;
                }
            }

            yield return LevelStatus.finished;
        }


        private List<WaveInfo> build_level(int level_number)
        {
            if (level_number < 1)
            {
                Debug.LogErrorFormat("Wrong level number: {0}", level_number);
                return null;
            }

            List<WaveInfo> selected_waves = new List<WaveInfo>();

            switch (level_number)
            {
                case 1:
                    {
                        selected_waves.Add(pick_a_wave_in(waves_lvl_1_easy));
                        selected_waves.Add(pick_a_wave_in(waves_lvl_1_easy));
                        selected_waves.Add(pick_a_wave_in(waves_lvl_2_challenging));
                        selected_waves.Add(pick_a_wave_in(waves_lvl_1_easy));
                        selected_waves.Add(pick_a_wave_in(waves_lvl_2_challenging));
                        selected_waves.Add(pick_a_wave_in(boss_lvl_1_challenging, WaveCategory.Boss));
                        break;
                    }
                case 2:
                    {
                        selected_waves.Add(pick_a_wave_in(waves_lvl_1_easy));
                        selected_waves.Add(pick_a_wave_in(waves_lvl_2_challenging));
                        selected_waves.Add(pick_a_wave_in(waves_lvl_2_challenging));
                        selected_waves.Add(pick_a_wave_in(waves_lvl_1_easy));
                        selected_waves.Add(pick_a_wave_in(waves_lvl_2_challenging));
                        selected_waves.Add(pick_a_wave_in(waves_lvl_2_challenging));
                        selected_waves.Add(pick_a_wave_in(boss_lvl_2_hard, WaveCategory.Boss));
                        break;
                    }
                case 3:
                    {
                        selected_waves.Add(pick_a_wave_in(waves_lvl_2_challenging));
                        selected_waves.Add(pick_a_wave_in(waves_lvl_3_hard));
                        selected_waves.Add(pick_a_wave_in(waves_lvl_2_challenging));
                        selected_waves.Add(pick_a_wave_in(waves_lvl_3_hard));
                        selected_waves.Add(pick_a_wave_in(waves_lvl_2_challenging));
                        selected_waves.Add(pick_a_wave_in(waves_lvl_3_hard));
                        selected_waves.Add(pick_a_wave_in(waves_lvl_3_hard));
                        selected_waves.Add(pick_a_wave_in(boss_lvl_2_hard, WaveCategory.Boss));
                        break;
                    }
                case 4:
                    {
                        selected_waves.Add(pick_a_wave_in(waves_lvl_2_challenging));
                        selected_waves.Add(pick_a_wave_in(waves_lvl_3_hard));
                        selected_waves.Add(pick_a_wave_in(waves_lvl_3_hard));
                        selected_waves.Add(pick_a_wave_in(waves_lvl_2_challenging));
                        selected_waves.Add(pick_a_wave_in(waves_lvl_3_hard));
                        selected_waves.Add(pick_a_wave_in(waves_lvl_3_hard));
                        selected_waves.Add(pick_a_wave_in(waves_lvl_1_easy));
                        selected_waves.Add(pick_a_wave_in(boss_lvl_1_challenging, WaveCategory.Boss));
                        selected_waves.Add(pick_a_wave_in(boss_lvl_2_hard, WaveCategory.Boss));
                        selected_waves.Add(pick_a_wave_in(boss_lvl_2_hard, WaveCategory.Boss));
                        selected_waves.Add(pick_a_wave_in(boss_lvl_3_hardcore, WaveCategory.Boss));
                        break;
                    }
                default:
                    {
                        // TODO: for an "infinite mode", just put some kind of algorithm here.
                        return null;
                    }

            }

            return selected_waves;
        }

    }

}
