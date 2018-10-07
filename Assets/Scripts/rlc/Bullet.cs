﻿using UnityEngine;
using System.Collections;

namespace rlc
{
    public class Bullet : MonoBehaviour
    {
        public float speed = 10.0f;


        public Clan clan_who_fired = Clan.enemy;


        private ColoredBody my_body;
        private bool is_reflected = false;

        void Start()
        {
            my_body = GetComponent<ColoredBody>();
            if (my_body == null)
            {
                Debug.LogError("Bullet objects must have a ColoredBody component!");
            }
        }

        void Update()
        {
            Movement.move_forward(transform, speed);
        }

        private void OnCollisionEnter(Collision collision)
        {
            var body_hit = collision.collider.GetComponent<ColoredBody>();
            var bullet_hit = collision.collider.GetComponent<Bullet>();
            if (body_hit != null    // hit a colored body...
            && bullet_hit == null   // ... which is not another bullet...
            )
            {
                Debug.Log("OnCollisionEnter" + name + " and " + collision.gameObject.name);

                bool colors_matches = ColorSystem.colors_matches(body_hit.color_family, my_body.color_family);
                bool hitting_the_enemy = clan_who_fired != body_hit.clan; // ... we are either enemy bullet hitting player or the reverse...

                if (hitting_the_enemy && colors_matches)
                {
                    // ... We hit an enemy matching the right color!
                    body_hit.on_hit();

                    if (body_hit.surface_effect == ColoredBody.SurfaceEffect.reflective)
                        end_with_reflection(collision);
                }

                // We hit something solid, so the bullet will end anyway.
                if (body_hit.surface_effect == ColoredBody.SurfaceEffect.solid)
                    end_with_impact(collision);
            }

        }

        private void end_with_reflection(Collision collision)
        {
            if (is_reflected) // To avoid multiple reflective collisions
                return;

            is_reflected = true;
            play_impact_animation(); // TODO: replace by another impact?
            const float time_to_die = 1.5f;
            Destroy(gameObject, time_to_die);


            // Now for the rest of the lifetime, we just go in another direction
            transform.forward = random_opposite_direction(collision.contacts[0].normal);

            // As soon as the bullet is reflected, it can hit anybody matching it!
            clan_who_fired = Clan.none;
        }

        private static Vector3 random_opposite_direction(Vector3 initial_direction)
        {
            Vector3 new_direction = initial_direction.normalized;

            const float reflection_angle = 45.0f;
            new_direction = Quaternion.Euler(Random.Range(0.0f, reflection_angle), 0.0f, Random.Range(0.0f, reflection_angle)) * new_direction;

            return new_direction;
        }

        private void end_with_impact(Collision collision)
        {
            is_reflected = true;
            play_impact_animation();
            Destroy(gameObject);
        }

        private void play_impact_animation()
        {
            // TODO: add impact animation here
        }

    }
}