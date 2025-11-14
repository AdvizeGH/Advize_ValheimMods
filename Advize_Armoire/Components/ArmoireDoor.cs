namespace Advize_Armoire;

using System;
using System.Collections.Generic;
using UnityEngine;
using static StaticMembers;

public class ArmoireDoor : MonoBehaviour, Hoverable, Interactable
{
    [Header("Transform References")]
    public Transform playerAttachPoint;
    public Transform cameraPosition;
    public GameObject equipmentAttachPoint;

    // Constants
    private const string AttachAnimation = "onGround";
    private const float UseDistance = 2f;

    // Static fields
    private static Gradient _hoverGradient;
    internal static List<EffectList> SoundEffects = [];

    // Static property
    private static Gradient HoverGradient => _hoverGradient ?? InitializeGradient();

    //Private instance fields
    private ZNetView _nview;
    private Animator _animator;
    private uint _lastDataRevision = uint.MaxValue;

    public void Awake()
    {
        _nview = GetComponent<ZNetView>();
        if (!_nview || !_nview.IsValid()) return;

        _animator = GetComponentInChildren<Animator>();
        _nview.Register("UseArmoire", new Action<long>(RPC_UseArmoire));
        InvokeRepeating("UpdateState", 0f, 5f);
    }

    private void RPC_UseArmoire(long uid)
    {
        if (!CanInteract) return;

        int currentState = _nview.GetZDO().GetInt(ZDOVars.s_state, 0);
        _nview.GetZDO().Set(ZDOVars.s_state, 1 - currentState, false);

        UpdateState();
    }

    private void UpdateState()
    {
        if (!_nview.IsValid() || _nview.m_zdo is not ZDO zdo || zdo.DataRevision == _lastDataRevision) return;

        _lastDataRevision = zdo.DataRevision;
        SetState(zdo.GetInt(ZDOVars.s_state, 0));
    }

    private void SetState(int state)
    {
        if (_animator.GetInteger("state") != state)
        {
            SoundEffects[state].Create(transform.position, transform.rotation, null, 1f, -1);
            _animator.SetInteger("state", state);
        }
    }

    public void ResetState()
    {
        _nview.InvokeRPC("UseArmoire");
        playerAttachPoint.localRotation = Quaternion.Euler(0, 0, 0);
    }

    internal bool CanInteract => _animator.GetCurrentAnimatorStateInfo(0).IsTag("openable");

    public string GetHoverText()
    {
        if (ArmoireUIController.IsArmoirePanelActive()) return string.Empty;

        float clampedPercentage = Mathf.Clamp01((float)AppearanceTracker.UnlockedPercentage);
        string color = ColorUtility.ToHtmlStringRGB(HoverGradient.Evaluate(clampedPercentage));

        return $"{GetHoverName()}\nCollected: <color=#{color}>{AppearanceTracker.TotalUnlocked}/{AppearanceTracker.TotalCollectable}</color>";
    }

    public string GetHoverName() => "Armoire";

    public bool Interact(Humanoid character, bool hold, bool alt)
    {
        Dbgl("Interacted with armoire");

        if (hold || !CanInteract) return false;

        if (character is not Player player || !InUsingDistance(player) || player.IsEncumbered()) return false;

        Player closestPlayer = Player.GetClosestPlayer(playerAttachPoint.position, 0.1f);
        if (closestPlayer && closestPlayer != Player.m_localPlayer)
        {
            Player.m_localPlayer.Message(MessageHud.MessageType.Center, "$msg_blocked", 0, null);
            return false;
        }

        if (!PrivateArea.CheckAccess(transform.position, 0f, true, false)) return true;

        if (!_animator.GetCurrentAnimatorStateInfo(0).IsName("opened"))
        {
            player.AttachStart(playerAttachPoint, null, false, false, false, AttachAnimation, default, cameraPosition);
            ArmoireUIController.ToggleArmoirePanel(this);
            equipmentAttachPoint.SetActive(true); //Did I put this here with the idea of setting it to inactive while the wardrobe is closed?
            _nview.ClaimOwnership(); // This might need to be placed elsewhere for best results, works for now
        }

        _nview.InvokeRPC("UseArmoire");
        //Game.instance.IncrementPlayerStat((_nview.GetZDO().GetInt(ZDOVars.s_state, 0) == 0) ? PlayerStatType.DoorsOpened : PlayerStatType.DoorsClosed, 1f);
        return true;
    }

    public bool UseItem(Humanoid user, ItemDrop.ItemData item) => false;

    private static Gradient InitializeGradient()
    {
        _hoverGradient = new();
        _hoverGradient.SetKeys(
        [
            new GradientColorKey(Color.red, 0.0f),
            new GradientColorKey(new Color(1f, 0.6470588f, 0f), 0.3f), // Orange #FFA500
            new GradientColorKey(Color.yellow, 0.6f),
            new GradientColorKey(Color.green, 0.9f),
            new GradientColorKey(Color.cyan, 1.0f)
        ], []);

        return _hoverGradient;
    }

    private bool InUsingDistance(Humanoid human) => Vector3.Distance(human.transform.position, playerAttachPoint.position) < UseDistance;
}
