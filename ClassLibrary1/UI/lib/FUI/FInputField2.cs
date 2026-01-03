using ONI_MP.DebugTools;
using TMPro;
using UnityEngine;

namespace UI.lib.UIcmp //Source: Aki
{
	public class FInputField2 : KScreen, IInputHandler
	{
		[MyCmpReq]
		public TMP_InputField inputField;

		[SerializeField]
		public string textPath = "Text";

		[SerializeField]
		public string placeHolderPath = "Placeholder";

		private bool allowInputs = true;
		public bool AllowInputs => allowInputs;

		private bool initialized;

		private bool DataTextUpdate = false;
		public void SetTextFromData(string newText)
		{
			DataTextUpdate = true;
			Text = newText;
			//inputField.ForceLabelUpdate();

			DataTextUpdate = false;
		}

		public bool IsEditing()
		{
			return isEditing;
		}

		public string Text
		{
			get => inputField.text;
			set
			{
				if (!initialized)
				{
					// rehook references, these were lost on LocText conversion
#if DEBUG
                    DebugConsole.Log("rehooking text input references");
#endif
					inputField.textComponent = inputField.textViewport.transform.Find(textPath).gameObject.AddOrGet<LocText>();
					inputField.placeholder = inputField.textViewport.transform.Find(placeHolderPath).gameObject.AddOrGet<LocText>();

					initialized = true;
				}

				inputField.text = value;
			}
		}

		public TMP_InputField.OnChangeEvent OnValueChanged => inputField.onValueChanged;

		public override void OnPrefabInit()
		{
			base.OnPrefabInit();
		}

		public override void OnSpawn()
		{
			base.OnSpawn();
			inputField.onFocus += OnEditStart;
			inputField.onEndEdit.AddListener(OnEditEnd);
			Activate();
		}

		public override void OnShow(bool show)
		{
			base.OnShow(show);

			if (show)
			{
				Activate();
				inputField.ActivateInputField();
			}
			else
			{
				Deactivate();
			}
		}

		public void Submit()
		{
			inputField.OnSubmit(null);
		}

		private void OnEditEnd(string input)
		{
			isEditing = false;
			inputField.DeactivateInputField();
		}

		public void ExternalStartEditing() => OnEditStart();

		private void OnEditStart()
		{
			isEditing = true;
			inputField.Select();
			inputField.ActivateInputField();

			KScreenManager.Instance.RefreshStack();
		}

		public override void OnKeyDown(KButtonEvent e)
		{
			if (!isEditing)
			{
				base.OnKeyDown(e);
				return;
			}

			if (e.TryConsume(Action.Escape))
			{
				inputField.DeactivateInputField();
				e.Consumed = true;
				isEditing = false;
			}

			if (e.TryConsume(Action.DialogSubmit))
			{
				e.Consumed = true;
				inputField.OnSubmit(null);
			}

			if (isEditing)
			{
				e.Consumed = true;
				return;
			}

			if (!e.Consumed)
			{
				base.OnKeyDown(e);
			}
		}

		public void SetInteractable(bool interactable)
		{
			allowInputs = interactable;
			if (inputField != null)
			{
				inputField.interactable = allowInputs;
			}
		}
	}
}