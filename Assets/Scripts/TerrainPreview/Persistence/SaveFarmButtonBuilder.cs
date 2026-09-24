using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace AgriDabao3D
{
    public class SaveFarmButtonBuilder : MonoBehaviour
    {
        private Button saveButton;
        private Text saveText;

        private IEnumerator Start()
        {
            for (int i = 0; i < 120; i++)
            {
                if (Object.FindFirstObjectByType<Canvas>() != null)
                {
                    Build();
                    yield break;
                }

                yield return null;
            }

            Debug.LogWarning("SaveFarmButtonBuilder could not find a Canvas.");
        }

        private void Update()
        {
            if (FarmPersistenceManager.Instance == null)
                return;

            bool saving = FarmPersistenceManager.Instance.IsSaving;

            if (saveText != null)
                saveText.text = saving ? "Saving..." : "Save Farm";

            if (saveButton != null)
                saveButton.interactable = !saving;
        }

        private void Build()
        {
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
                return;

            saveButton = HudIconButton.Create(
                canvas.transform,
                "SaveFarmButton",
                UIThemeSprites.Instance?.saveFarmButton,
                HudIconButton.SlotSaveFarm,
                "Save Farm",
                ShowConfirmation,
                out _,
                out saveText);
        }

        private void ShowConfirmation()
        {
            if (FarmConfirmPopup.Instance == null)
            {
                StartCoroutine(FarmPersistenceManager.Instance.SaveFarm());
                return;
            }

            FarmConfirmPopup.Instance.Show(
                "Do you want to save this current farm and replace your old farm save?",
                () => StartCoroutine(FarmPersistenceManager.Instance.SaveFarm()),
                UIThemeSprites.Instance?.saveFarmLabel);
        }
    }
}
