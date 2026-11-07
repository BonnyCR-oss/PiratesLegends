using System;
using PirateLegends.CameraSystem;
using PirateLegends.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using Cursor = UnityEngine.Cursor;

namespace PirateLegends.UI
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class TestCharacterSelector : MonoBehaviour
    {
        [Serializable]
        public sealed class CharacterSlot
        {
            public string displayName;
            [TextArea] public string description;
            [TextArea] public string controls;
            public Color accent = Color.white;
            public GameObject prefab;
        }

        [SerializeField] private CharacterSlot[] characters = new CharacterSlot[4];
        [SerializeField] private Camera testCamera;
        [SerializeField] private Transform spawnPoint;
        private VisualElement menu, hud, cards;
        private Label selectedName, selectedDescription, controlsLabel, hudName;
        private Button playButton;
        private Button[] slotButtons;
        private GameObject current;
        private PlayerInputReader reader;
        private PlayerLocomotion fireMotor;
        private TestWalkMotor walkMotor;
        private ThirdPersonOrbitCamera orbit;
        private int selectedIndex = -1;
        public int SelectedIndex => selectedIndex;
        public bool MenuVisible { get; private set; } = true;
        public GameObject CurrentCharacter => current;

        private void Start()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            menu = root.Q("selector"); hud = root.Q("hud"); cards = root.Q("cards");
            selectedName = root.Q<Label>("selected-name");
            selectedDescription = root.Q<Label>("selected-description");
            controlsLabel = root.Q<Label>("controls"); hudName = root.Q<Label>("hud-name");
            playButton = root.Q<Button>("play");
            playButton.clicked += BeginTest;
            root.Q<Button>("change").clicked += OpenMenu;
            slotButtons = new Button[characters.Length];
            for (int i = 0; i < characters.Length; i++)
            {
                int index = i;
                var slot = characters[i];
                var button = new Button(() => SelectCharacter(index));
                button.name = "character-" + i;
                button.AddToClassList("character-card");
                button.style.borderLeftColor = slot.accent;
                button.Add(new Label((i + 1).ToString("00")) { name = "number" });
                var nameLabel = new Label(slot.displayName); nameLabel.AddToClassList("card-name");
                button.Add(nameLabel);
                var status = new Label(slot.prefab ? "DISPONIBLE" : "PRÓXIMAMENTE");
                status.AddToClassList("card-status"); button.Add(status);
                button.SetEnabled(slot.prefab != null);
                slotButtons[i] = button; cards.Add(button);
            }
            if (!testCamera) testCamera = Camera.main;
            orbit = testCamera.GetComponent<ThirdPersonOrbitCamera>();
            if (!orbit) orbit = testCamera.gameObject.AddComponent<ThirdPersonOrbitCamera>();
            orbit.enabled = false;
            for (int i = 0; i < characters.Length; i++)
                if (characters[i].prefab) { SelectCharacter(i); break; }
            SetMenu(true);
        }

        public void SelectCharacter(int index)
        {
            if (index < 0 || index >= characters.Length || !characters[index].prefab) return;
            // Deactivate immediately so the old character cannot read input or render this frame.
            if (current) { current.SetActive(false); Destroy(current); }
            selectedIndex = index;
            var slot = characters[index];
            Vector3 position = spawnPoint ? spawnPoint.position : Vector3.zero;
            current = Instantiate(slot.prefab, position, Quaternion.identity);
            current.name = "TestCharacter_" + slot.displayName;
            fireMotor = current.GetComponent<PlayerLocomotion>();
            reader = current.GetComponent<PlayerInputReader>();
            if (!reader) reader = current.AddComponent<PlayerInputReader>();
            reader.enabled = false;
            if (fireMotor) { fireMotor.enabled = false; fireMotor.CameraTransform = testCamera.transform; }
            var animator = current.GetComponentInChildren<Animator>();
            if (!fireMotor)
            {
                animator.applyRootMotion = false;
                var renderers = current.GetComponentsInChildren<SkinnedMeshRenderer>();
                Bounds bounds = renderers[0].bounds;
                foreach (var r in renderers) bounds.Encapsulate(r.bounds);
                var body = current.GetComponent<CharacterController>();
                if (!body) body = current.AddComponent<CharacterController>();
                body.height = Mathf.Clamp(bounds.size.y, 1f, 2.5f);
                body.radius = .25f;
                body.center = current.transform.InverseTransformPoint(bounds.center);
                body.skinWidth = .025f;
                current.transform.position += Vector3.up * (position.y - bounds.min.y + .025f);
                walkMotor = current.AddComponent<TestWalkMotor>();
                walkMotor.Configure(testCamera.transform);
                walkMotor.enabled = false;
            }
            else walkMotor = null;
            var target = new GameObject("TestCameraTarget").transform;
            target.SetParent(current.transform, false);
            target.localPosition = Vector3.up * 1.25f;
            orbit.Configure(target, reader, current.transform);
            selectedName.text = slot.displayName;
            selectedName.style.color = slot.accent;
            selectedDescription.text = slot.description;
            controlsLabel.text = slot.controls;
            hudName.text = slot.displayName + "  /  " + slot.controls;
            for (int i = 0; i < slotButtons.Length; i++) slotButtons[i].EnableInClassList("selected", i == index);
            playButton.SetEnabled(true);
            SetMenu(true);
            // Offset the preview to the right of the selector panel.
            Vector3 focus = current.transform.position + Vector3.up * .95f;
            Vector3 offset = new Vector3(2.7f, .65f, 3.8f);
            testCamera.transform.position = focus + offset;
            testCamera.transform.LookAt(focus);
            testCamera.transform.position -= testCamera.transform.right * .9f;
        }

        public void BeginTest()
        {
            if (!current) return;
            SetMenu(false);
            reader.enabled = true;
            if (fireMotor) fireMotor.enabled = true;
            if (walkMotor) walkMotor.enabled = true;
            orbit.enabled = true;
            Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false;
        }

        public void OpenMenu()
        {
            if (selectedIndex >= 0) SelectCharacter(selectedIndex);
        }

        private void SetMenu(bool visible)
        {
            MenuVisible = visible;
            menu.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            hud.style.display = visible ? DisplayStyle.None : DisplayStyle.Flex;
            if (orbit) orbit.enabled = !visible;
            if (visible) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
        }

        private void Update()
        {
            if (!MenuVisible && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) OpenMenu();
        }

        private void LateUpdate()
        {
            if (MenuVisible) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
        }

        private void OnDisable()
        {
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        }
    }
}
