using System;
using System.Collections.Generic;
using UnityEngine;

namespace Storyborn
{
    // Original, short procedural cues. No physics, gameplay timers or global timeScale changes.
    public sealed class CombatFeedback : MonoBehaviour
    {
        public bool Paused;
        public bool Muted { get; private set; }
        const int ParticleLimit = 80;
        const int LabelLimit = 12;
        sealed class Particle
        {
            public SpriteRenderer view;
            public Vector2 velocity;
            public float life, total, gravity, drag;
            public Color color;
        }
        sealed class DamageLabel
        {
            public TextMesh view;
            public float life;
            public Vector3 origin;
            public Color color;
        }
        readonly List<Particle> particles = new List<Particle>(ParticleLimit);
        readonly List<DamageLabel> labels = new List<DamageLabel>(LabelLimit);
        readonly List<AudioClip> clips = new List<AudioClip>();
        readonly System.Random random = new System.Random(7183);
        Camera worldCamera;
        GameObject root;
        AudioSource source;
        Texture2D pixelTexture;
        Sprite pixel;
        Font font;
        AudioClip swing, finish, impact, injury, dash, landing, pickup, clear;
        bool initialized, audioPaused;

        public void Initialize(Camera camera)
        {
            worldCamera = camera;
            if (initialized) return;
            initialized = true;
            root = new GameObject("Combat feedback (temporary)");
            root.transform.SetParent(transform, false);
            pixelTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            pixelTexture.name = "Feedback pixel";
            pixelTexture.filterMode = FilterMode.Point;
            pixelTexture.SetPixel(0, 0, Color.white);pixelTexture.Apply();
            pixel = Sprite.Create(pixelTexture, new Rect(0, 0, 1, 1), Vector2.one * .5f, 1);
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;source.spatialBlend = 0;source.volume = .3f;source.mute = Muted;
            swing = MakeClip("Soft swing", .07f, 700, 180, .65f, .1f);
            finish = MakeClip("Heavy swing", .11f, 500, 90, .72f, .15f);
            impact = MakeClip("Impact", .08f, 210, 65, .5f, .4f);
            injury = MakeClip("Hurt", .14f, 130, 45, .4f, .5f);
            dash = MakeClip("Dash", .08f, 900, 360, .7f, .08f);
            landing = MakeClip("Landing", .05f, 110, 60, .7f, .1f);
            pickup = MakeClip("Pickup", .18f, 650, 1300, .03f, .32f);
            clear = MakeClip("Room clear", .38f, 500, 1000, .02f, .25f);
        }
        AudioClip MakeClip(string title, float duration, float startHz, float endHz, float noise, float tone)
        {
            const int rate = 22050;
            float[] samples = new float[Mathf.CeilToInt(duration * rate)];
            float phase = 0;
            for (int i = 0; i < samples.Length; i++)
            {
                float t = (float)i / samples.Length;
                phase += Mathf.Lerp(startHz, endHz, t) * Mathf.PI * 2 / rate;
                float envelope = Mathf.Min(t * 30, 1) * (1 - t) * (1 - t);
                samples[i] = ((float)(random.NextDouble() * 2 - 1) * noise + Mathf.Sin(phase) * tone) * envelope * .6f;
            }
            AudioClip clip = AudioClip.Create(title, samples.Length, 1, rate, false);
            clip.SetData(samples, 0);clips.Add(clip);return clip;
        }
        void Play(AudioClip clip, float volume = 1)
        {
            if (source != null && clip != null && !Muted && !Paused) source.PlayOneShot(clip, volume);
        }
        public void SetMuted(bool muted)
        { Muted = muted;if (source != null) source.mute = muted; }
        public void Swing(int combo)
        { Play(combo >= 2 ? finish : swing, combo >= 2 ? .8f : .65f); }
        public void Hit(Vector2 p, int damage, bool heavy)
        {
            if (!initialized) return;
            Play(impact, heavy ? 1 : .65f);
            Color gold = new Color(1f, .87f, .55f);
            for (int i = 0; i < (heavy ? 11 : 7); i++)
            {
                float angle = Range(-Mathf.PI, Mathf.PI);
                float speed = Range(2.5f, heavy ? 8 : 5);
                AddParticle(p, new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed,
                    new Vector2(Range(.06f, .14f), .06f), i % 3 == 0 ? Color.white : gold,
                    Range(.13f, .3f), 5, 5);
            }
            AddLabel(p + Vector2.up * .5f, damage, heavy);
        }
        public void Hurt(Vector2 p)
        {
            if (!initialized) return;
            Play(injury, .8f);
            for (int i = 0; i < 10; i++)
                AddParticle(p, new Vector2(Range(-5, 5), Range(1, 5)), Vector2.one * .1f,
                    new Color(1, .43f, .45f), Range(.18f, .4f), 12, 2);
        }
        public void Dash(Vector2 p, int facing)
        {
            if (!initialized) return;
            Play(dash, .6f);
            DashTrail(p, facing);
        }
        public void DashTrail(Vector2 p, int facing)
        {
            if (!initialized) return;
            for (int i = 0; i < 5; i++)
                AddParticle(p + new Vector2(-facing * Range(.1f, .55f), Range(-.5f, .5f)),
                    new Vector2(-facing * Range(1.5f, 4), 0), new Vector2(Range(.2f, .45f), .055f),
                    new Color(.64f, .95f, .89f, .7f), Range(.12f, .23f), 0, 1);
        }
        public void Land(Vector2 p)
        {
            if (!initialized) return;
            Play(landing, .35f);
            for (int i = 0; i < 6; i++)
                AddParticle(p, new Vector2(Range(-3.5f, 3.5f), Range(.4f, 1.2f)), new Vector2(.16f, .08f),
                    new Color(.84f, .85f, .68f, .6f), Range(.12f, .23f), 3, 7);
        }
        public void Pickup(Vector2 p)
        {
            if (!initialized) return;
            Play(pickup, .7f);
            for (int i = 0; i < 10; i++)
                AddParticle(p + new Vector2(Range(-.5f, .5f), Range(-.3f, .3f)),
                    new Vector2(Range(-.6f, .6f), Range(1, 2.8f)), Vector2.one * .08f,
                    new Color(.92f, .88f, .5f), Range(.25f, .5f), 0, 1);
        }
        public void ClearRoom() { Play(clear, .8f); }
        float Range(float min, float max) { return min + (float)random.NextDouble() * (max - min); }
        void AddParticle(Vector2 p, Vector2 velocity, Vector2 size, Color color, float life, float gravity, float drag)
        {
            if (particles.Count >= ParticleLimit)
            { Destroy(particles[0].view.gameObject);particles.RemoveAt(0); }
            var go = new GameObject("Feedback particle");go.transform.SetParent(root.transform, false);
            go.transform.position = p;go.transform.localScale = new Vector3(size.x, size.y, 1);
            var view = go.AddComponent<SpriteRenderer>();view.sprite = pixel;view.color = color;view.sortingOrder = 12;
            particles.Add(new Particle { view = view, velocity = velocity, color = color, life = life, total = life, gravity = gravity, drag = drag });
        }
        void AddLabel(Vector2 p, int value, bool heavy)
        {
            if (font == null) return;
            if (labels.Count >= LabelLimit) { Destroy(labels[0].view.gameObject);labels.RemoveAt(0); }
            var go = new GameObject("Damage number");go.transform.SetParent(root.transform, false);
            go.transform.position = p + new Vector2(Range(-.12f, .12f), 0);
            if (worldCamera != null) go.transform.rotation = worldCamera.transform.rotation;
            var text = go.AddComponent<TextMesh>();
            text.font = font;text.text = value.ToString();text.fontSize = 48;
            text.characterSize = heavy ? .065f : .05f;text.fontStyle = FontStyle.Bold;
            text.anchor = TextAnchor.MiddleCenter;text.alignment = TextAlignment.Center;
            Color color = heavy ? new Color(1, .86f, .42f) : new Color(1, .98f, .84f);
            text.color = color;
            var renderer = go.GetComponent<MeshRenderer>();renderer.sharedMaterial = font.material;renderer.sortingOrder = 15;
            labels.Add(new DamageLabel { view = text, origin = go.transform.position, life = .5f, color = color });
        }
        void Update()
        {
            if (!initialized) return;
            if (source != null && audioPaused != Paused)
            { if (Paused) source.Pause();else source.UnPause();audioPaused = Paused; }
            if (Paused) return;
            float dt = Mathf.Min(Time.unscaledDeltaTime, .05f);
            for (int i = particles.Count - 1; i >= 0; i--)
            {
                Particle p = particles[i];p.life -= dt;
                if (p.life <= 0 || p.view == null)
                { if (p.view != null) Destroy(p.view.gameObject);particles.RemoveAt(i);continue; }
                p.view.transform.position += (Vector3)(p.velocity * dt);
                p.velocity *= Mathf.Exp(-p.drag * dt);p.velocity.y -= p.gravity * dt;
                Color color = p.color;color.a *= Mathf.Clamp01(p.life / p.total);p.view.color = color;
            }
            for (int i = labels.Count - 1; i >= 0; i--)
            {
                DamageLabel l = labels[i];l.life -= dt;
                if (l.life <= 0 || l.view == null)
                { if (l.view != null) Destroy(l.view.gameObject);labels.RemoveAt(i);continue; }
                float progress = 1 - l.life / .5f;
                l.view.transform.position = l.origin + Vector3.up * (progress * .65f);
                Color color = l.color;color.a *= Mathf.Clamp01(l.life / .2f);l.view.color = color;
            }
        }
        void OnDestroy()
        {
            if (source != null) { source.Stop();Destroy(source); }
            foreach (AudioClip clip in clips) if (clip != null) Destroy(clip);
            clips.Clear();
            if (root != null) Destroy(root);
            particles.Clear();labels.Clear();
            if (pixel != null) Destroy(pixel);
            if (pixelTexture != null) Destroy(pixelTexture);
        }
    }
}
