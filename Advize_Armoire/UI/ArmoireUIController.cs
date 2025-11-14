namespace Advize_Armoire;

using UnityEngine;
using static StaticMembers;

static class ArmoireUIController
{
    internal static ArmoireUI ArmoireUIInstance { get; set; }

    private static ArmoireDoor lastUsedArmoire = null;
    private static Quaternion oldLookYaw = Quaternion.identity;
    private static float oldLookPitch;
    private static Vector3 oldLookDir = default;
    internal static bool cancelButtonWasClicked = false;

    internal static void CreateArmoireUI(Transform hudroot)
    {
        Dbgl("Creating ArmoireUI");
        (ArmoireUIInstance = Object.Instantiate(guiPrefab, hudroot, false).GetComponent<ArmoireUI>()).Initialize();
    }

    internal static void DestroyArmoireUI()
    {
        // Not sure this is necessary, but I feel safer cleaning up this way in case ArmoireUIInstance.gameObject never becomes active in the scene
        Dbgl("Destroying ArmoireUI");
        Object.Destroy(ArmoireUIInstance);
    }

    internal static bool IsArmoirePanelValid() => ArmoireUIInstance;

    internal static bool IsArmoirePanelActive() => IsArmoirePanelValid() && ArmoireUIInstance.gameObject.activeSelf;

    private static void HideArmoirePanel()
    {
        ArmoireUIInstance.gameObject.SetActive(false);
        ArmoireUIInstance.DestroyScrollableGrid();
    }

    private static void ShowArmoireUI()
    {
        ArmoireUIInstance.gameObject.SetActive(true);
        ArmoireUIInstance.slotsPanel.SetActive(true);
        ArmoireUIInstance.scrollView.SetActive(false);
    }

    internal static void ToggleArmoirePanel(ArmoireDoor openedArmoire = null)
    {
        if (IsArmoirePanelActive())
            CloseArmoirePanel();
        else
            OpenArmoirePanel(openedArmoire);
    }

    private static void CloseArmoirePanel()
    {
        HideArmoirePanel();

        if (!lastUsedArmoire) return;

        lastUsedArmoire.ResetState();

        Player player = Player.m_localPlayer;
        player.m_lookYaw = oldLookYaw;
        player.m_lookPitch = oldLookPitch;
        player.m_lookDir = oldLookDir;

        player.ResetAttachCameraPoint();
        player.AttachStop();

        lastUsedArmoire = null;
    }

    private static void OpenArmoirePanel(ArmoireDoor openedArmoire)
    {
        ShowArmoireUI();

        if (!openedArmoire) return;

        lastUsedArmoire = openedArmoire;

        Player player = Player.m_localPlayer;
        oldLookYaw = player.m_lookYaw;
        oldLookPitch = player.m_lookPitch;
        oldLookDir = player.m_lookDir;

        player.m_lookPitch = -20f;
        player.UpdateEyeRotation();
        player.m_lookDir = player.m_eye.forward;
    }

    internal static bool HandleEscapeOrCancelInput()
    {
        if (cancelButtonWasClicked)
        {
            cancelButtonWasClicked = false;
            if (lastUsedArmoire is ArmoireDoor { CanInteract: true })
            {
                ToggleArmoirePanel();
                return true;
            }
        }

        if (!lastUsedArmoire)
        {
            ArmoireUIInstance.ToggleExtraneousButtons(forceReset: true);
            ToggleArmoirePanel();
            return true;
        }

        return false;
    }
}
