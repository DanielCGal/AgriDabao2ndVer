using UnityEngine;
using UnityEngine.Rendering;

namespace AgriDabao3D
{
    public class WeatherVisualController : MonoBehaviour
    {
        [Header("References")]
        public Transform followTarget;
        public Vector3 followOffset = new Vector3(0f, 8f, 0f);
        public Light sunLight;

        [Header("Rain Particles")]
        public ParticleSystem rainParticleSystem;
        public int rainEmissionRate = 250;
        public int typhoonEmissionRate = 700;

        [Header("Rain Motion")]
        [Tooltip("How fast rain falls, in metres per second. This and the settings below " +
                 "replace the particle system's own Start Speed, velocity and render mode " +
                 "whenever it starts, so change them here rather than on RainParticles.")]
        public float rainFallSpeed = 20f;
        [Tooltip("How fast typhoon rain falls, in metres per second.")]
        public float typhoonFallSpeed = 28f;
        [Tooltip("How hard the typhoon's wind drives its rain sideways, toward the east, in " +
                 "metres per second. Against a fall of 28, 7 leans the rain about 14 degrees. " +
                 "0 lets it fall straight down.")]
        public float typhoonWindSpeed = 7f;
        [Tooltip("Extra streak length for every metre per second a drop is moving.")]
        public float streakLengthPerSpeed = 0.025f;
        [Tooltip("The widest a drop can be drawn, as a fraction of the screen, so one falling " +
                 "right past the camera stays a streak instead of smearing across the view.")]
        public float maxDropScreenSize = 0.02f;

        [Header("Storm Sky")]
        [Tooltip("Colour of the sky overhead in rain, in full daylight. It fades in over the " +
                 "normal sky and hides the sun. Toward the horizon it blends into the rain fog " +
                 "colour, so the distant ground and the sky meet without a seam.")]
        public Color rainSkyColor = new Color(0.45f, 0.48f, 0.52f);
        [Tooltip("Colour of the sky overhead in a typhoon, in full daylight. Darker than rain.")]
        public Color typhoonSkyColor = new Color(0.27f, 0.29f, 0.32f);
        [Tooltip("How bright the storm sky overhead stays at night, as a fraction of its " +
                 "daytime colour.")]
        [Range(0f, 1f)]
        public float stormSkyNightBrightness = 0.08f;
        [Tooltip("Seconds the storm sky takes to cover the sun when a storm starts, and to " +
                 "clear again when it ends.")]
        [Min(0.1f)]
        public float stormSkyFadeSeconds = 2.5f;

        [Header("Lightning")]
        [Tooltip("Typhoons only. Shortest wait, in seconds, between flashes of lightning in the " +
                 "storm sky. Each wait is picked at random between this and the longest wait.")]
        [Min(0.5f)]
        public float lightningMinInterval = 6f;
        [Tooltip("Longest wait, in seconds, between flashes of lightning.")]
        [Min(0.5f)]
        public float lightningMaxInterval = 16f;
        [Tooltip("How far a flash lifts the storm sky toward the lightning colour. Low reads as " +
                 "lightning behind the clouds; 1 turns the whole sky the lightning colour.")]
        [Range(0f, 1f)]
        public float lightningBrightness = 0.35f;
        [Tooltip("The colour the sky flashes toward.")]
        public Color lightningColor = new Color(0.82f, 0.87f, 1f);

        [Header("Fog")]
        public bool controlFog = true;
        public Color clearFogColor = new Color(0.78f, 0.86f, 0.95f);
        public Color rainFogColor = new Color(0.55f, 0.62f, 0.72f);
        public Color typhoonFogColor = new Color(0.38f, 0.42f, 0.48f);
        public Color droughtFogColor = new Color(0.92f, 0.74f, 0.48f);

        public float clearFogDensity = 0.0015f;
        public float rainFogDensity = 0.0040f;
        public float typhoonFogDensity = 0.0100f;
        public float droughtFogDensity = 0.0025f;

        [Header("Ambient")]
        [Tooltip("Adds a soft, shadowless light from straight above that reaches what the sun " +
                 "does not: shade under a shade net or greenhouse, and the whole farm at night. " +
                 "The weather colours below tint it in daylight.")]
        public bool controlAmbientLight = true;
        public Color clearAmbientColor = new Color(0.95f, 0.95f, 0.95f);
        public Color rainAmbientColor = new Color(0.72f, 0.76f, 0.82f);
        public Color typhoonAmbientColor = new Color(0.50f, 0.54f, 0.60f);
        public Color droughtAmbientColor = new Color(1.00f, 0.82f, 0.62f);
        [Tooltip("How much of the weather colour above lights shaded places in full daylight. " +
                 "0 leaves shade as black as the scene had it; higher lifts it further, and " +
                 "brightens open ground a little too.")]
        [Range(0f, 1f)]
        public float daylightAmbientStrength = 0.35f;
        [Tooltip("The least light anywhere, at any time: what lights the farm at night, and the " +
                 "floor under the darkest storm. Lighter is brighter; pure white lights the night " +
                 "about as brightly as the sun lights the day.")]
        public Color minimumAmbientColor = new Color(0.35f, 0.37f, 0.44f);

        [Header("Sun")]
        public bool controlSunIntensity = true;
        public float clearSunMultiplier = 1.00f;
        public float rainSunMultiplier = 0.75f;
        public float typhoonSunMultiplier = 0.45f;
        public float droughtSunMultiplier = 1.15f;

        [Header("Transition")]
        public float visualBlendSpeed = 2.5f;

        /// <summary>Farthest out the storm sky is drawn. It is also kept inside the camera's far plane.</summary>
        private const float StormSkyMaxRadius = 1200f;

        /// <summary>Seconds for a flash of lightning to fall to about a third; three times this and it is all but gone.</summary>
        private const float LightningFade = 0.06f;

        /// <summary>Smallest change in the fill light, per colour channel, worth applying.</summary>
        private const float FillChangeThreshold = 0.002f;

        private static readonly int StormSkyColorId = Shader.PropertyToID("_Color");
        private static readonly int StormSkyHorizonColorId = Shader.PropertyToID("_HorizonColor");

        private WeatherEventType lastAppliedEvent = WeatherEventType.Clear;
        private bool initialized;

        private Color currentFogColor;
        private float currentFogDensity;
        private Color currentAmbientColor;
        private float currentSunMultiplierValue = 1f;

        private Color targetFogColor;
        private float targetFogDensity;
        private Color targetAmbientColor;
        private float targetSunMultiplierValue = 1f;

        private float baseSunIntensity = 1f;

        private Light fillLight;

        /// <summary>The fill light last applied, in linear colour.</summary>
        private Color appliedFill = new Color(-1f, -1f, -1f, 1f);

        private GameObject stormSky;
        private Material stormSkyMaterial;

        private Color currentStormSkyColor;
        private Color targetStormSkyColor;

        /// <summary>How much of the normal sky the storm sky hides: 0 none, 1 all of it, sun included.</summary>
        private float currentStormSkyCover;
        private float targetStormSkyCover;

        /// <summary>When the next flash is due, or below 0 when none is scheduled.</summary>
        private float nextLightningTime = -1f;
        private float lightningStartTime;
        private int lightningPulseCount;
        private readonly float[] lightningPulseDelays = new float[3];
        private readonly float[] lightningPulsePeaks = new float[3];

        private void Start()
        {
            if (followTarget == null)
            {
                Camera cam = Camera.main;
                if (cam != null)
                    followTarget = cam.transform;
            }

            if (sunLight == null && GameTimeSystem.Instance != null)
                sunLight = GameTimeSystem.Instance.sunLight;

            if (sunLight != null)
                baseSunIntensity = sunLight.intensity;

            if (rainParticleSystem == null)
                rainParticleSystem = GetComponentInChildren<ParticleSystem>();

            if (WeatherSystem.Instance != null)
                WeatherSystem.Instance.OnWeatherChanged += OnWeatherChanged;

            currentFogColor = RenderSettings.fogColor;
            currentFogDensity = RenderSettings.fogDensity;
            currentAmbientColor = RenderSettings.ambientLight;

            targetFogColor = currentFogColor;
            targetFogDensity = currentFogDensity;
            targetAmbientColor = currentAmbientColor;
            targetSunMultiplierValue = 1f;

            BuildFillLight();
            BuildStormSky();
            currentStormSkyColor = rainSkyColor;
            targetStormSkyColor = rainSkyColor;

            ApplyImmediateFromCurrentWeather();
            initialized = true;
        }

        private void OnDestroy()
        {
            if (WeatherSystem.Instance != null)
                WeatherSystem.Instance.OnWeatherChanged -= OnWeatherChanged;

            if (stormSky != null)
                Destroy(stormSky);

            if (stormSkyMaterial != null)
                Destroy(stormSkyMaterial);

            if (fillLight != null)
                Destroy(fillLight.gameObject);
        }

        private void Update()
        {
            FollowTarget();

            if (!initialized)
                return;

            SmoothVisuals();
        }

        private void FollowTarget()
        {
            if (followTarget == null)
            {
                Camera cam = Camera.main;
                if (cam != null)
                    followTarget = cam.transform;
            }

            if (followTarget != null)
            {
                transform.position = followTarget.position + followOffset;

                // Centred on the camera itself rather than on the rain above it, so
                // the dome's horizon sits level with the player's eye.
                if (stormSky != null)
                    stormSky.transform.position = followTarget.position;
            }
        }

        private void OnWeatherChanged()
        {
            ApplyTargetsFromWeather();
        }

        private void ApplyImmediateFromCurrentWeather()
        {
            ApplyTargetsFromWeather();

            currentFogColor = targetFogColor;
            currentFogDensity = targetFogDensity;
            currentAmbientColor = targetAmbientColor;
            currentSunMultiplierValue = targetSunMultiplierValue;
            currentStormSkyColor = targetStormSkyColor;
            currentStormSkyCover = targetStormSkyCover;

            if (controlFog)
            {
                RenderSettings.fog = true;
                RenderSettings.fogColor = currentFogColor;
                RenderSettings.fogDensity = currentFogDensity;
            }

            ApplyFillLight(true);
            ApplyRainStateImmediate();
            ApplySunImmediate();
            ApplyStormSky();
        }

        private void ApplyTargetsFromWeather()
        {
            WeatherEventType weather = WeatherSystem.Instance != null
                ? WeatherSystem.Instance.currentEvent
                : WeatherEventType.Clear;

            switch (weather)
            {
                case WeatherEventType.Rain:
                    targetFogColor = rainFogColor;
                    targetFogDensity = rainFogDensity;
                    targetAmbientColor = rainAmbientColor;
                    targetSunMultiplierValue = rainSunMultiplier;
                    targetStormSkyColor = rainSkyColor;
                    targetStormSkyCover = 1f;
                    break;

                case WeatherEventType.Typhoon:
                    targetFogColor = typhoonFogColor;
                    targetFogDensity = typhoonFogDensity;
                    targetAmbientColor = typhoonAmbientColor;
                    targetSunMultiplierValue = typhoonSunMultiplier;
                    targetStormSkyColor = typhoonSkyColor;
                    targetStormSkyCover = 1f;
                    break;

                case WeatherEventType.ExtremeDrought:
                    targetFogColor = droughtFogColor;
                    targetFogDensity = droughtFogDensity;
                    targetAmbientColor = droughtAmbientColor;
                    targetSunMultiplierValue = droughtSunMultiplier;
                    targetStormSkyCover = 0f;
                    break;

                default:
                    targetFogColor = clearFogColor;
                    targetFogDensity = clearFogDensity;
                    targetAmbientColor = clearAmbientColor;
                    targetSunMultiplierValue = clearSunMultiplier;
                    // The colour is left as it was, so a storm clears away in its own
                    // grey instead of changing shade on the way out.
                    targetStormSkyCover = 0f;
                    break;
            }

            ApplyRainStateImmediate();

            if (weather != lastAppliedEvent)
            {
                Debug.Log($"[WeatherVisuals] Applied visuals for weather={weather}");
                lastAppliedEvent = weather;
            }
        }

        private void ApplyRainStateImmediate()
        {
            if (rainParticleSystem == null)
                return;

            WeatherEventType weather = WeatherSystem.Instance != null
                ? WeatherSystem.Instance.currentEvent
                : WeatherEventType.Clear;

            var emission = rainParticleSystem.emission;

            if (weather == WeatherEventType.Rain)
            {
                ApplyRainMotion(rainFallSpeed, 0f);

                if (!rainParticleSystem.isPlaying)
                    rainParticleSystem.Play();

                emission.rateOverTime = rainEmissionRate;
            }
            else if (weather == WeatherEventType.Typhoon)
            {
                ApplyRainMotion(typhoonFallSpeed, typhoonWindSpeed);

                if (!rainParticleSystem.isPlaying)
                    rainParticleSystem.Play();

                emission.rateOverTime = typhoonEmissionRate;
            }
            else
            {
                emission.rateOverTime = 0f;

                if (rainParticleSystem.isPlaying)
                    rainParticleSystem.Stop();
            }
        }

        /// <summary>
        /// Makes the drops fall, whatever the particle system in the scene is set to.
        ///
        /// RainParticles emits from a flat 18 x 18 metre box eight metres over the
        /// camera. A box emits along its own forward axis, and nothing turns this
        /// one toward the ground - its rotation in the scene is zero - so every drop
        /// left at 22 m/s level with the ground, heading north, and none ever came
        /// down into view. Rotating the object would not fix it cleanly either: the
        /// box is flat on its own Y, so turned to point down it would stand up as a
        /// wall.
        ///
        /// So the drops are launched with no speed at all and moved by a velocity
        /// set in world space, which no rotation of the object can bend. They are
        /// drawn stretched along that velocity, because a drop falling this fast
        /// covers at least a third of a metre between frames, and drawn as a dot it
        /// reads as a flickering speck rather than as rain.
        /// </summary>
        private void ApplyRainMotion(float fallSpeed, float windSpeed)
        {
            ParticleSystem.MainModule main = rainParticleSystem.main;
            main.startSpeed = 0f;
            main.gravityModifier = 0f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            ParticleSystem.VelocityOverLifetimeModule velocity = rainParticleSystem.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;

            // All three as constants: Unity rejects the change if the axes are in
            // different modes.
            velocity.x = new ParticleSystem.MinMaxCurve(windSpeed);
            velocity.y = new ParticleSystem.MinMaxCurve(-Mathf.Abs(fallSpeed));
            velocity.z = new ParticleSystem.MinMaxCurve(0f);

            ParticleSystemRenderer rainRenderer = rainParticleSystem.GetComponent<ParticleSystemRenderer>();
            if (rainRenderer == null)
                return;

            rainRenderer.renderMode = ParticleSystemRenderMode.Stretch;
            rainRenderer.velocityScale = streakLengthPerSpeed;
            // Streaks follow the drop's own speed only. Stretched by the camera's
            // as well, every drop would smear sideways whenever the player walked.
            rainRenderer.cameraVelocityScale = 0f;
            rainRenderer.maxParticleSize = maxDropScreenSize;
        }

        private void SmoothVisuals()
        {
            float t = Mathf.Clamp01(Time.deltaTime * visualBlendSpeed);

            currentFogColor = Color.Lerp(currentFogColor, targetFogColor, t);
            currentFogDensity = Mathf.Lerp(currentFogDensity, targetFogDensity, t);
            currentAmbientColor = Color.Lerp(currentAmbientColor, targetAmbientColor, t);
            currentSunMultiplierValue = Mathf.Lerp(currentSunMultiplierValue, targetSunMultiplierValue, t);
            currentStormSkyColor = Color.Lerp(currentStormSkyColor, targetStormSkyColor, t);

            // Linear rather than eased like the rest, so the fade actually finishes:
            // the last few percent of an eased one would leave the sun glowing
            // through the clouds for seconds after the storm had arrived.
            currentStormSkyCover = Mathf.MoveTowards(currentStormSkyCover, targetStormSkyCover,
                Time.deltaTime / stormSkyFadeSeconds);

            if (controlFog)
            {
                RenderSettings.fog = true;
                RenderSettings.fogColor = currentFogColor;
                RenderSettings.fogDensity = currentFogDensity;
            }

            ApplyFillLight(false);
            ApplySunImmediate();
            UpdateLightning();
            ApplyStormSky();
        }

        private void ApplySunImmediate()
        {
            if (!controlSunIntensity || sunLight == null)
                return;

            float liveBaseIntensity = baseSunIntensity;

            if (GameTimeSystem.Instance != null && GameTimeSystem.Instance.sunLight == sunLight)
                liveBaseIntensity = sunLight.intensity;

            sunLight.intensity = liveBaseIntensity * currentSunMultiplierValue;
        }

        // ---------------------------------------------------------- fill light

        /// <summary>
        /// A soft light for everything the sun does not reach.
        ///
        /// The scene's environment light has its intensity at 0 in the Lighting
        /// settings, so the sun was the only light there was: the ground under a
        /// shade net or greenhouse went black at noon, and at night crops vanished.
        /// Turning that setting up would light every night like day, because that
        /// light is worked out once from the daytime sky and never dims. Setting the
        /// scene's ambient light from code had no visible effect either - Unity kept
        /// the zero.
        ///
        /// So the light comes from a real light, which nothing overrides: directional,
        /// shining straight down, casting no shadows, so it reaches under a shade net
        /// as easily as open ground. Being directional it looks the same whether a
        /// quality level lights extra lights per pixel or only at mesh corners, and it
        /// costs about the same as a second sun without shadows.
        ///
        /// It is made the scene's sun source first. URP takes the sun source as its
        /// main light when one is set, and otherwise the brightest directional light -
        /// which at night, with the sun turned almost to nothing, would be this, and
        /// the sky would put its sun disc overhead in the middle of the night.
        /// </summary>
        private void BuildFillLight()
        {
            if (!controlAmbientLight || sunLight == null)
                return;

            RenderSettings.sun = sunLight;

            GameObject go = new GameObject("DarkAreaFillLight");
            go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            fillLight = go.AddComponent<Light>();
            fillLight.type = LightType.Directional;
            fillLight.shadows = LightShadows.None;
            // The project turns colour temperature on for lights; left on, it would
            // tint this away from the colour set below.
            fillLight.useColorTemperature = false;
        }

        /// <summary>
        /// Sets the fill to follow the clock. In daylight it is the weather's ambient
        /// colour at a fraction of its strength, enough that shade still reads as
        /// shade; as the sun goes down it settles to the minimum and never goes below
        /// it, so crops and sprouts stay visible in the dark.
        /// </summary>
        private void ApplyFillLight(bool force)
        {
            if (fillLight == null)
                return;

            Color day = currentAmbientColor.linear * (daylightAmbientStrength * Daylight());
            Color floor = minimumAmbientColor.linear;
            Color fill = new Color(
                Mathf.Max(day.r, floor.r),
                Mathf.Max(day.g, floor.g),
                Mathf.Max(day.b, floor.b),
                1f);

            // Only when it has visibly changed: the light shifts slowly with the clock.
            if (!force &&
                Mathf.Abs(fill.r - appliedFill.r) < FillChangeThreshold &&
                Mathf.Abs(fill.g - appliedFill.g) < FillChangeThreshold &&
                Mathf.Abs(fill.b - appliedFill.b) < FillChangeThreshold)
            {
                return;
            }

            appliedFill = fill;

            float peak = Mathf.Max(fill.r, Mathf.Max(fill.g, fill.b));
            fillLight.enabled = peak > 0.0001f;
            if (!fillLight.enabled)
                return;

            // The project lights in linear intensity, where a light gives its colour
            // converted to linear times its intensity. So the brightest channel goes
            // in the intensity and the hue, converted back, in the colour.
            fillLight.color = new Color(fill.r / peak, fill.g / peak, fill.b / peak, 1f).gamma;
            fillLight.intensity = peak;
        }

        // ------------------------------------------------------------ storm sky

        /// <summary>
        /// The overcast sky for rain and typhoons: a dome around the camera that
        /// fades in over the normal sky.
        ///
        /// The sky is Unity's procedural skybox, and nothing on it turns it grey or
        /// puts its sun out gradually. Its sun disc is drawn from the sun light, and
        /// dimming that light would darken the whole farm with it. So rather than
        /// change the sky, this covers it: fully faded in it hides the sun
        /// completely, and fading out uncovers the sky and sun exactly as they were.
        ///
        /// It never writes depth, and the terrain and crops in front of it hide it
        /// as usual. The radius stays inside the camera's far plane; ground beyond
        /// it is lost in the storm's fog anyway.
        /// </summary>
        private void BuildStormSky()
        {
            Shader shader = Resources.Load<Shader>("Shaders/StormSky");
            if (shader == null || !shader.isSupported)
            {
                Debug.LogWarning("[WeatherVisuals] StormSky shader missing or unsupported; " +
                                 "rain and typhoons will keep the normal sky.");
                return;
            }

            // The mesh is borrowed from a throwaway primitive, destroyed at once: its
            // collider, a ball around the player, must not exist for even one
            // physics step.
            GameObject probe = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Mesh sphere = probe.GetComponent<MeshFilter>().sharedMesh;
            DestroyImmediate(probe);

            stormSky = new GameObject("StormSky", typeof(MeshFilter), typeof(MeshRenderer));
            stormSky.GetComponent<MeshFilter>().sharedMesh = sphere;

            float radius = StormSkyMaxRadius;
            Camera view = followTarget != null ? followTarget.GetComponent<Camera>() : Camera.main;
            if (view != null)
                radius = Mathf.Min(radius, view.farClipPlane * 0.9f);

            // The primitive is one metre across.
            stormSky.transform.localScale = Vector3.one * (radius * 2f);

            stormSkyMaterial = new Material(shader) { name = "StormSky (runtime)" };

            MeshRenderer dome = stormSky.GetComponent<MeshRenderer>();
            dome.sharedMaterial = stormSkyMaterial;
            dome.shadowCastingMode = ShadowCastingMode.Off;
            dome.receiveShadows = false;
            dome.lightProbeUsage = LightProbeUsage.Off;
            dome.reflectionProbeUsage = ReflectionProbeUsage.Off;

            stormSky.SetActive(false);
        }

        private void ApplyStormSky()
        {
            if (stormSkyMaterial == null)
                return;

            bool covering = currentStormSkyCover > 0.001f;
            if (stormSky.activeSelf != covering)
                stormSky.SetActive(covering);

            if (!covering)
                return;

            // Overhead darkens with the day, as a real sky does. The horizon takes
            // the fog colour unchanged, because that is the colour the fogged ground
            // meets it in, day or night.
            Color overhead = currentStormSkyColor * Mathf.Lerp(stormSkyNightBrightness, 1f, Daylight());
            Color horizon = RenderSettings.fogColor;

            // Lightning lights the clouds overhead more than the haze on the horizon.
            float flash = LightningFlash() * lightningBrightness;
            overhead = Color.Lerp(overhead, lightningColor, flash);
            horizon = Color.Lerp(horizon, lightningColor, flash * 0.6f);

            overhead.a = currentStormSkyCover;

            stormSkyMaterial.SetColor(StormSkyColorId, overhead);
            stormSkyMaterial.SetColor(StormSkyHorizonColorId, horizon);
        }

        /// <summary>1 with the sun well up, falling to 0 just after it sets.</summary>
        private float Daylight()
        {
            if (sunLight == null)
                return 1f;

            // GameTimeSystem turns the light to point straight down at noon, so the
            // sun itself lies back along the light's forward axis.
            float elevation = Vector3.Dot(-sunLight.transform.forward, Vector3.up);
            return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-0.1f, 0.3f, elevation));
        }

        // ------------------------------------------------------------ lightning

        /// <summary>
        /// Schedules lightning while a typhoon has the sky. Nothing is drawn here;
        /// ApplyStormSky reads the flash every frame.
        /// </summary>
        private void UpdateLightning()
        {
            bool typhoon = WeatherSystem.Instance != null &&
                           WeatherSystem.Instance.currentEvent == WeatherEventType.Typhoon;

            // Only once the storm has covered the sky, so a flash never lights a sky
            // that is still blue, or one already clearing.
            if (!typhoon || currentStormSkyCover < 0.95f)
            {
                nextLightningTime = -1f;
                return;
            }

            if (nextLightningTime < 0f)
            {
                nextLightningTime = Time.time + RandomBetween(lightningMinInterval, lightningMaxInterval);
                return;
            }

            if (Time.time < nextLightningTime)
                return;

            StartLightningFlash();
            nextLightningTime = Time.time + RandomBetween(lightningMinInterval, lightningMaxInterval);
        }

        /// <summary>
        /// One strike: a single flash, or a quick flicker of two or three, the way
        /// real lightning strikes again down the same path - each one after the
        /// first a little dimmer.
        /// </summary>
        private void StartLightningFlash()
        {
            lightningStartTime = Time.time;
            lightningPulseCount = lightningRandom.Next(1, lightningPulsePeaks.Length + 1);

            float delay = 0f;
            for (int i = 0; i < lightningPulseCount; i++)
            {
                lightningPulseDelays[i] = delay;
                lightningPulsePeaks[i] = i == 0 ? 1f : RandomBetween(0.45f, 0.85f);
                delay += RandomBetween(0.07f, 0.18f);
            }
        }

        /// <summary>
        /// Lightning draws on a generator of its own. Unity's shared one is also used
        /// by gameplay code, and every number a flash took from it would shift the
        /// numbers that code drew next - never its odds, but a purely visual effect
        /// should leave gameplay's randomness alone entirely.
        /// </summary>
        private readonly System.Random lightningRandom = new System.Random();

        private float RandomBetween(float min, float max)
        {
            return min + (float)lightningRandom.NextDouble() * (max - min);
        }

        /// <summary>How bright the lightning is right now, from 0 to 1.</summary>
        private float LightningFlash()
        {
            if (lightningPulseCount == 0)
                return 0f;

            float elapsed = Time.time - lightningStartTime;
            float flash = 0f;

            for (int i = 0; i < lightningPulseCount; i++)
            {
                float since = elapsed - lightningPulseDelays[i];
                if (since < 0f)
                    continue;

                // At its brightest the instant it strikes, then gone in a moment.
                flash = Mathf.Max(flash, lightningPulsePeaks[i] * Mathf.Exp(-since / LightningFade));
            }

            // Every pulse has struck and died away.
            if (elapsed > lightningPulseDelays[lightningPulseCount - 1] + LightningFade * 8f)
                lightningPulseCount = 0;

            return flash;
        }
    }
}
