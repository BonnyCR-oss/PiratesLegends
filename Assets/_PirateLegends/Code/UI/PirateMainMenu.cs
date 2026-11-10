using PirateLegends.Player;
using UnityEngine;
using UnityEngine.UI;

namespace PirateLegends.UI
{
    public sealed class PirateMainMenu : MonoBehaviour
    {
        private static readonly int Ability = Animator.StringToHash("Base Layer.DualWeaponCombo");
        private static readonly int Idle = Animator.StringToHash("Base Layer.Locomotion");
        private const float IdleDuration = 5.5f;

        [SerializeField] private GameObject firePrefab;
        [SerializeField] private Camera previewCamera;
        [SerializeField] private Transform stage;
        [SerializeField] private GameObject menu, modePanel, hud, settings;
        [SerializeField] private GameObject soloPanel, friendsPanel;
        [SerializeField] private GameObject[] mainOptions;
        [SerializeField] private Text status;
        [SerializeField] private Text teamCodeText, teamFeedback, displayModeText, qualityText, motionText;
        [SerializeField] private InputField joinCode;
        [SerializeField] private Slider masterVolume, musicVolume;
        [SerializeField] private AudioSource menuMusic;
        [SerializeField] private Renderer backdrop;
        private MaterialPropertyBlock backdropProperties;
        private int backdropIndex = -1;
        private bool reduceMotion;
        private string teamCode;
        private Animator animator;
        private bool canShowAbility;
        private bool showcasingAbility;
        private float nextAbility;

        private void Start()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            backdropProperties = new MaterialPropertyBlock();
            masterVolume.onValueChanged.AddListener(SetMasterVolume);
            musicVolume.onValueChanged.AddListener(SetMusicVolume);
            ApplySettings();
            PickBackdrop();
            menuMusic.Play();
            MountFirePreview();
            RestartShowcase();
        }

        private void Update()
        {
            if (reduceMotion || !canShowAbility || !stage.gameObject.activeInHierarchy || animator.IsInTransition(0))
                return;

            var state = animator.GetCurrentAnimatorStateInfo(0);
            if (state.fullPathHash != Idle)
                return;

            if (showcasingAbility)
            {
                showcasingAbility = false;
                nextAbility = Time.time + IdleDuration;
            }
            else if (Time.time >= nextAbility)
            {
                animator.CrossFadeInFixedTime(Ability, .14f, 0, 0);
                showcasingAbility = true;
            }
        }

        public void ShowMenu()
        {
            bool reopened = !menu.activeSelf;
            hud.SetActive(false);
            menu.SetActive(true);
            stage.gameObject.SetActive(true);
            ShowPanel(null);
            status.text = string.Empty;
            PlayerPrefs.Save();
            if (reopened)
            {
                PickBackdrop();
                menuMusic.UnPause();
                RestartShowcase();
            }
        }

        private void ShowPanel(GameObject active)
        {
            foreach (var option in mainOptions)
                option.SetActive(!active);
            modePanel.SetActive(active == modePanel);
            soloPanel.SetActive(active == soloPanel);
            friendsPanel.SetActive(active == friendsPanel);
            settings.SetActive(active == settings);
        }

        public void ChooseMode() => ShowPanel(modePanel);
        public void ShowSoloOptions() => ShowPanel(soloPanel);

        public void StartSolo()
        {
            ShowPanel(null);
            menu.SetActive(false);
            stage.gameObject.SetActive(false);
            menuMusic.Pause();
            hud.SetActive(true);
        }

        public void ShowFriends()
        {
            ShowPanel(friendsPanel);
            teamFeedback.text = string.Empty;
        }

        public void GenerateTeamCode()
        {
            const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            var characters = new char[6];
            for (int i = 0; i < characters.Length; i++)
                characters[i] = alphabet[Random.Range(0, alphabet.Length)];
            teamCode = new string(characters);
            teamCodeText.text = teamCode;
            teamFeedback.text = "Código local generado.";
        }

        public void CopyTeamCode()
        {
            if (string.IsNullOrEmpty(teamCode))
            {
                teamFeedback.text = "Genera un código primero.";
                return;
            }
            GUIUtility.systemCopyBuffer = teamCode;
            teamFeedback.text = "Código copiado.";
        }

        public void CheckJoinCode()
        {
            string code = joinCode.text.Trim().ToUpperInvariant();
            bool valid = code.Length == 6;
            foreach (char character in code)
                valid &= "ABCDEFGHJKLMNPQRSTUVWXYZ23456789".IndexOf(character) >= 0;
            teamFeedback.text = valid ? "Código válido. La conexión en línea está pendiente." : "Ingresa un código de 6 caracteres.";
        }

        public void Continue()
        {
            status.text = "No hay una partida guardada disponible.";
        }

        public void OpenSettings()
        {
            ShowPanel(settings);
            RefreshSettingsLabels();
        }

        public void Quit()
        {
            PlayerPrefs.Save();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void PickBackdrop()
        {
            backdropIndex = backdropIndex < 0 ? Random.Range(0, 4) : (backdropIndex + Random.Range(1, 4)) % 4;
            UpdateBackdrop();
        }

        private void UpdateBackdrop()
        {
            backdropProperties.SetFloat("_BackdropIndex", Mathf.Max(0, backdropIndex));
            backdropProperties.SetFloat("_ReduceMotion", reduceMotion ? 1f : 0f);
            backdrop.SetPropertyBlock(backdropProperties);
        }

        private void ApplySettings()
        {
            masterVolume.SetValueWithoutNotify(PlayerPrefs.GetFloat("Pirates.Menu.Master", 1f));
            musicVolume.SetValueWithoutNotify(PlayerPrefs.GetFloat("Pirates.Menu.Music", .65f));
            AudioListener.volume = masterVolume.value;
            menuMusic.volume = musicVolume.value;
            Screen.fullScreen = PlayerPrefs.GetInt("Pirates.Menu.Fullscreen", Screen.fullScreen ? 1 : 0) != 0;
            QualitySettings.SetQualityLevel(Mathf.Clamp(PlayerPrefs.GetInt("Pirates.Menu.Quality",
                QualitySettings.GetQualityLevel()), 0, QualitySettings.names.Length - 1));
            reduceMotion = PlayerPrefs.GetInt("Pirates.Menu.Motion", 0) != 0;
            RefreshSettingsLabels();
            UpdateBackdrop();
        }

        private void SetMasterVolume(float value)
        {
            AudioListener.volume = value;
            PlayerPrefs.SetFloat("Pirates.Menu.Master", value);
        }

        private void SetMusicVolume(float value)
        {
            menuMusic.volume = value;
            PlayerPrefs.SetFloat("Pirates.Menu.Music", value);
        }

        public void ToggleDisplayMode()
        {
            bool fullScreen = PlayerPrefs.GetInt("Pirates.Menu.Fullscreen", Screen.fullScreen ? 1 : 0) == 0;
            Screen.fullScreen = fullScreen;
            PlayerPrefs.SetInt("Pirates.Menu.Fullscreen", fullScreen ? 1 : 0);
            RefreshSettingsLabels();
        }

        public void CycleQuality()
        {
            int level = (QualitySettings.GetQualityLevel() + 1) % QualitySettings.names.Length;
            QualitySettings.SetQualityLevel(level);
            PlayerPrefs.SetInt("Pirates.Menu.Quality", level);
            RefreshSettingsLabels();
        }

        public void ToggleMotion()
        {
            reduceMotion = !reduceMotion;
            PlayerPrefs.SetInt("Pirates.Menu.Motion", reduceMotion ? 1 : 0);
            RestartShowcase();
            UpdateBackdrop();
            RefreshSettingsLabels();
        }

        private void RefreshSettingsLabels()
        {
            displayModeText.text = PlayerPrefs.GetInt("Pirates.Menu.Fullscreen", Screen.fullScreen ? 1 : 0) != 0
                ? "Pantalla completa" : "Modo ventana";
            qualityText.text = "Calidad · " + QualitySettings.names[QualitySettings.GetQualityLevel()];
            motionText.text = "Reducir movimiento · " + (reduceMotion ? "Sí" : "No");
        }

        private void RestartShowcase()
        {
            if (animator && animator.HasState(0, Idle))
                animator.Play(Idle, 0, 0);
            showcasingAbility = false;
            nextAbility = Time.time + IdleDuration;
        }

        private void MountFirePreview()
        {
            if (!firePrefab)
                return;

            var holder = new GameObject("Montaje de vista previa");
            holder.SetActive(false);
            holder.transform.SetParent(stage, false);
            var preview = Instantiate(firePrefab, holder.transform, false);
            preview.name = "Pirata de Fuego";
            foreach (var script in preview.GetComponentsInChildren<MonoBehaviour>(true))
                script.enabled = script is FireAbility && script.enabled;
            foreach (var collider in preview.GetComponentsInChildren<Collider>(true))
                collider.enabled = false;

            animator = preview.GetComponentInChildren<Animator>();
            preview.transform.SetParent(stage, false);
            Destroy(holder);
            if (animator)
            {
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                canShowAbility = animator.HasState(0, Ability);
            }

            var meshes = preview.GetComponentsInChildren<SkinnedMeshRenderer>();
            if (meshes.Length == 0)
                return;

            var bounds = meshes[0].bounds;
            foreach (var mesh in meshes)
                bounds.Encapsulate(mesh.bounds);
            // The prefab root is at the feet; skinned bounds include animation padding.
            // Align that root with the deck instead of lifting it by the padded bounds.
            preview.transform.position += stage.position - new Vector3(bounds.center.x, preview.transform.position.y, bounds.center.z);
            float height = Mathf.Max(bounds.size.y, 1f);
            Vector3 focus = stage.position + Vector3.up * height * .46f;
            previewCamera.transform.position = focus + new Vector3(.21f, .09f, 1f) * height * 2.03f;
            previewCamera.transform.LookAt(focus);
            previewCamera.transform.position += previewCamera.transform.right * (height * .42f);
        }
    }
}
