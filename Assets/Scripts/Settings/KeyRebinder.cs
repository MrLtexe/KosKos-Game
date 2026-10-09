using System;
using UnityEngine;
using UnityEngine.InputSystem;

// Tuş atama: klavye/fare tuşlarını oyuncu değiştirebilir. Başka bir eylemde kullanılan tuş reddedilir.
// Duraklatma (Esc) ve sapan nişanı (fare konumu) değiştirilemez. Kontrolcü ataması ileride aynı sisteme eklenecek.
public static class KeyRebinder
{
    public readonly struct Entry
    {
        // Ekranda görünen adın çeviri anahtarı (Loc)
        public readonly string LabelKey;
        public readonly string Action;
        // Bileşik eylemin parçası (ör. Balance "negative"); null = eylemin ilk ataması
        public readonly string Part;

        public Entry(string labelKey, string action, string part = null)
        {
            LabelKey = labelKey;
            Action = action;
            Part = part;
        }

        // Seçili dildeki adı
        public string Label => Loc.Get(LabelKey);
    }

    public static readonly Entry[] Rebindables =
    {
        new Entry("action.jump", "Jump"),
        new Entry("action.dash", "Dash"),
        new Entry("action.sword", "Sword"),
        new Entry("action.boost", "BoostCharge"),
        new Entry("action.hook", "Hook"),
        new Entry("action.jetBag", "JetBag"),
        new Entry("action.magBoots", "MagBoots"),
        new Entry("action.balanceLeft", "Balance", "negative"),
        new Entry("action.balanceRight", "Balance", "positive")
    };

    // Esc hem atamayı iptal eder hem de duraklatma tuşudur; iptalden hemen sonra duraklatma tetiklenmesin (sn)
    private const float PauseGuardTime = 0.2f;

    private static InputActionRebindingExtensions.RebindingOperation operation;
    private static float lastEndTime = float.NegativeInfinity;

    public static bool IsRebinding => operation != null;
    // Duraklatma menüsü bunu kontrol eder
    public static bool BlocksPause => IsRebinding || Time.unscaledTime - lastEndTime < PauseGuardTime;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        operation = null;
        lastEndTime = float.NegativeInfinity;
    }

    public static string DisplayName(InputActionAsset asset, Entry entry)
    {
        InputAction action = asset.FindAction(entry.Action, true);
        return action.GetBindingDisplayString(BindingIndex(action, entry.Part));
    }

    // done: null = başarılı veya iptal, dolu = hata mesajı
    public static void Start(InputActionAsset asset, Entry entry, Action<string> done)
    {
        if (IsRebinding) return;

        InputAction action = asset.FindAction(entry.Action, true);
        int index = BindingIndex(action, entry.Part);
        string previousOverride = action.bindings[index].overridePath;
        bool wasEnabled = action.enabled;
        // Atama sırasında eylem kapalı olmalı
        action.Disable();

        operation = action.PerformInteractiveRebinding(index)
            .WithExpectedControlType("Button")
            .WithControlsHavingToMatchPath("<Keyboard>")
            .WithControlsHavingToMatchPath("<Mouse>")
            .WithCancelingThrough("<Keyboard>/escape")
            .OnMatchWaitForAnother(0.1f)
            .OnComplete(_ => Finish(asset, action, index, previousOverride, wasEnabled, true, done))
            .OnCancel(_ => Finish(asset, action, index, previousOverride, wasEnabled, false, done))
            .Start();
    }

    private static void Finish(InputActionAsset asset, InputAction action, int index, string previousOverride,
        bool wasEnabled, bool completed, Action<string> done)
    {
        operation?.Dispose();
        operation = null;
        lastEndTime = Time.unscaledTime;

        string message = null;
        if (completed)
        {
            string newPath = action.bindings[index].effectivePath;
            if (IsUsedElsewhere(asset, action, index, newPath))
            {
                // Çakışma: eski atamaya geri dön
                if (string.IsNullOrEmpty(previousOverride)) action.RemoveBindingOverride(index);
                else action.ApplyBindingOverride(index, previousOverride);
                message = Loc.Get("settings.keyInUse");
            }
            else
            {
                GameSettings.SaveBindings(asset);
            }
        }

        if (wasEnabled) action.Enable();
        done?.Invoke(message);
    }

    private static bool IsUsedElsewhere(InputActionAsset asset, InputAction action, int index, string path)
    {
        foreach (Entry other in Rebindables)
        {
            InputAction otherAction = asset.FindAction(other.Action, true);
            int otherIndex = BindingIndex(otherAction, other.Part);
            if (otherAction == action && otherIndex == index) continue;
            if (string.Equals(otherAction.bindings[otherIndex].effectivePath, path, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }

    // Bileşik parçanın (ör. "negative") atama sırası; part null ise ilk atama
    public static int BindingIndex(InputAction action, string part)
    {
        if (part == null) return 0;
        for (int i = 0; i < action.bindings.Count; i++)
        {
            if (action.bindings[i].isPartOfComposite && action.bindings[i].name == part) return i;
        }
        Debug.LogError($"[KosKos] '{action.name}' eyleminde '{part}' parçası bulunamadı.");
        return 0;
    }
}
