using System;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace ConnectionGuard;

internal sealed class ConnectionGuardRuntime : MonoBehaviour
{
	private const string HandshakeKey = "cg_hs";

	private static ConnectionGuardRuntime _instance;

	private GameObject _hudRoot;
	private Canvas _canvas;
	private Text _pingText;
	private Font _font;
	private float _nextTickAt;
	private float _nextApplyAt;
	private int _lastPing = int.MinValue;
	private int _lastQueuedOut = int.MinValue;
	private int _lastQueuedIn = int.MinValue;
	private bool _lastInRoom;
	private bool _hudVisible;
	private bool _timeoutsApplied;
	private int _appliedTimeoutMs = -1;
	private string _cachedSceneName = string.Empty;
	private bool _cachedGameplayScene;
	private bool _wasInRoom;
	private bool _loggedInstallReminder;
	private string _lastHandshakeWarn = string.Empty;
	private float _nextBacklogWarnAt;

	public ConnectionGuardRuntime(IntPtr ptr)
		: base(ptr)
	{
	}

	internal static void Create()
	{
		if ((Object)(object)_instance != (Object)null)
		{
			return;
		}

		GameObject go = new GameObject("ConnectionGuardRuntime");
		Object.DontDestroyOnLoad((Object)go);
		((Object)go).hideFlags = HideFlags.HideAndDontSave;
		_instance = go.AddComponent<ConnectionGuardRuntime>();
	}

	private void RefreshSceneCache()
	{
		string name = SceneManager.GetActiveScene().name ?? string.Empty;
		if (name == _cachedSceneName)
		{
			return;
		}

		_cachedSceneName = name;
		_cachedGameplayScene = IsGameplaySceneName(name);
		_timeoutsApplied = false;
		_nextApplyAt = 0f;
	}

	private void Update()
	{
		float now = Time.unscaledTime;
		if (now < _nextTickAt)
		{
			return;
		}

		_nextTickAt = now + 1f;
		RefreshSceneCache();

		if (!ConnectionGuardPlugin.Enabled.Value)
		{
			SetHudVisible(false);
			return;
		}

		if (now >= _nextApplyAt || !_timeoutsApplied)
		{
			_nextApplyAt = now + 30f;
			TryApplyTimeouts();
		}

		TickHandshake();
		UpdatePingHud();
	}

	private void TickHandshake()
	{
		if (!TryGetInRoom(out bool inRoom))
		{
			return;
		}

		if (inRoom && !_wasInRoom)
		{
			_loggedInstallReminder = false;
			_lastHandshakeWarn = string.Empty;
		}
		else if (!inRoom && _wasInRoom)
		{
			_loggedInstallReminder = false;
			_lastHandshakeWarn = string.Empty;
		}

		_wasInRoom = inRoom;
		if (!inRoom)
		{
			return;
		}

		if (!_loggedInstallReminder)
		{
			_loggedInstallReminder = true;
			ConnectionGuardPlugin.LogSource.LogInfo(
				(object)"Install ConnectionGuard on all PCs with matching timeout cfg.");
		}

		PublishOrCheckHandshake();
	}

	private static string BuildHandshakeValue()
	{
		int timeoutMs = Mathf.Clamp(ConnectionGuardPlugin.DisconnectTimeoutMs.Value, 10000, 60000);
		int allowance = Mathf.Clamp(ConnectionGuardPlugin.SentCountAllowance.Value, 5, 12);
		int quickResend = Mathf.Clamp(ConnectionGuardPlugin.QuickResends.Value, 2, 5);
		return ConnectionGuardPlugin.PluginVersion + "|" + timeoutMs + "|" + allowance + "|" + quickResend;
	}

	private void PublishOrCheckHandshake()
	{
		try
		{
			Room room = PhotonNetwork.CurrentRoom;
			if (room == null)
			{
				return;
			}

			string local = BuildHandshakeValue();
			bool isHost;
			try
			{
				isHost = PhotonNetwork.IsMasterClient;
			}
			catch
			{
				isHost = false;
			}

			if (isHost)
			{
				object existing = null;
				if (room.CustomProperties != null && room.CustomProperties.ContainsKey(HandshakeKey))
				{
					existing = room.CustomProperties[HandshakeKey];
				}

				if (existing == null || existing.ToString() != local)
				{
					Hashtable props = new Hashtable { [HandshakeKey] = local };
					room.SetCustomProperties(props);
				}

				return;
			}

			if (room.CustomProperties == null || !room.CustomProperties.ContainsKey(HandshakeKey))
			{
				return;
			}

			string roomValue = room.CustomProperties[HandshakeKey]?.ToString() ?? string.Empty;
			if (string.IsNullOrEmpty(roomValue) || roomValue == local || roomValue == _lastHandshakeWarn)
			{
				return;
			}

			_lastHandshakeWarn = roomValue;
			ConnectionGuardPlugin.LogSource.LogWarning(
				(object)("ConnectionGuard timeout cfg/version mismatch with host. Local=" + local + " Room=" + roomValue
					+ ". Match ConnectionGuard.dll + timeout cfg on all PCs."));
		}
		catch (Exception ex)
		{
			if (ConnectionGuardPlugin.DebugLogging.Value)
			{
				ConnectionGuardPlugin.LogSource.LogWarning((object)("ConnectionGuard handshake failed: " + ex.Message));
			}
		}
	}

	private static bool TryGetInRoom(out bool inRoom)
	{
		try
		{
			inRoom = PhotonNetwork.InRoom;
			return true;
		}
		catch
		{
			inRoom = false;
			return false;
		}
	}

	private void TryApplyTimeouts()
	{
		try
		{
			LoadBalancingClient client = PhotonNetwork.NetworkingClient;
			if (client == null)
			{
				return;
			}

			LoadBalancingPeer peer = client.LoadBalancingPeer;
			if (peer == null)
			{
				return;
			}

			int timeoutMs = Mathf.Clamp(ConnectionGuardPlugin.DisconnectTimeoutMs.Value, 10000, 60000);
			int allowance = Mathf.Clamp(ConnectionGuardPlugin.SentCountAllowance.Value, 5, 12);
			int quickResends = Mathf.Clamp(ConnectionGuardPlugin.QuickResends.Value, 2, 5);
			byte quick = (byte)quickResends;

			if (_timeoutsApplied
				&& _appliedTimeoutMs == timeoutMs
				&& peer.DisconnectTimeout == timeoutMs
				&& peer.SentCountAllowance == allowance
				&& peer.QuickResendAttempts == quick)
			{
				return;
			}

			PhotonNetwork.KeepAliveInBackground = Mathf.Clamp(
				ConnectionGuardPlugin.KeepAliveInBackgroundSeconds.Value,
				10f,
				600f);
			PhotonNetwork.QuickResends = quickResends;
			peer.DisconnectTimeout = timeoutMs;
			peer.SentCountAllowance = allowance;
			peer.QuickResendAttempts = quick;

			_timeoutsApplied = true;
			_appliedTimeoutMs = timeoutMs;

			if (ConnectionGuardPlugin.DebugLogging.Value)
			{
				ConnectionGuardPlugin.LogSource.LogInfo(
					(object)$"Applied Photon timeouts: DisconnectTimeout={timeoutMs}ms SentCountAllowance={allowance} QuickResend={quick}");
			}
		}
		catch (Exception ex)
		{
			if (ConnectionGuardPlugin.DebugLogging.Value)
			{
				ConnectionGuardPlugin.LogSource.LogWarning((object)("Photon timeout apply failed: " + ex.Message));
			}
		}
	}

	private void UpdatePingHud()
	{
		if (!ConnectionGuardPlugin.ShowPingHud.Value || !_cachedGameplayScene)
		{
			SetHudVisible(false);
			return;
		}

		if (!TryGetInRoom(out bool inRoom))
		{
			SetHudVisible(false);
			return;
		}

		if (ConnectionGuardPlugin.ShowOnlyInMultiplayer.Value && !inRoom)
		{
			SetHudVisible(false);
			return;
		}

		EnsureHud();
		SetHudVisible(true);

		int ping = 0;
		int queuedOut = 0;
		int queuedIn = 0;
		if (inRoom)
		{
			try
			{
				ping = PhotonNetwork.GetPing();
			}
			catch
			{
				ping = 0;
			}

			TryReadQueues(out queuedOut, out queuedIn);
			MaybeWarnBacklog(ping, queuedOut, queuedIn);
		}

		if (ping == _lastPing
			&& queuedOut == _lastQueuedOut
			&& queuedIn == _lastQueuedIn
			&& inRoom == _lastInRoom)
		{
			return;
		}

		_lastPing = ping;
		_lastQueuedOut = queuedOut;
		_lastQueuedIn = queuedIn;
		_lastInRoom = inRoom;

		if ((Object)(object)_pingText == (Object)null)
		{
			return;
		}

		if (!inRoom)
		{
			_pingText.text = "PING  --";
		}
		else if (ConnectionGuardPlugin.ShowQueueOnHud.Value)
		{
			_pingText.text = $"PING  {ping} ms   Q {queuedOut}/{queuedIn}";
		}
		else
		{
			_pingText.text = $"PING  {ping} ms";
		}

		_pingText.color = PingColor(ping, queuedOut + queuedIn, inRoom);
	}

	private static void TryReadQueues(out int queuedOut, out int queuedIn)
	{
		queuedOut = 0;
		queuedIn = 0;
		try
		{
			LoadBalancingClient client = PhotonNetwork.NetworkingClient;
			LoadBalancingPeer peer = client?.LoadBalancingPeer;
			if (peer == null)
			{
				return;
			}

			queuedOut = peer.QueuedOutgoingCommands;
			queuedIn = peer.QueuedIncomingCommands;
		}
		catch
		{
		}
	}

	private void MaybeWarnBacklog(int ping, int queuedOut, int queuedIn)
	{
		int queued = queuedOut + queuedIn;
		if (ping < 250 && queued < 40)
		{
			return;
		}

		float now = Time.unscaledTime;
		if (now < _nextBacklogWarnAt)
		{
			return;
		}

		_nextBacklogWarnAt = now + 15f;
		ConnectionGuardPlugin.LogSource.LogWarning(
			(object)($"Photon backlog risk: ping={ping}ms queuedOut={queuedOut} queuedIn={queuedIn}. "
				+ "If actions are minutes late, leave/rejoin the room — do not keep playing on a backed-up link."));
	}

	private static Color PingColor(int ping, int queued, bool inRoom)
	{
		if (!inRoom)
		{
			return new Color(0.75f, 0.78f, 0.82f, 0.95f);
		}

		if (queued >= 40 || ping >= 250)
		{
			return new Color(0.95f, 0.4f, 0.35f, 1f);
		}

		if (ping <= 80)
		{
			return new Color(0.45f, 0.9f, 0.55f, 1f);
		}

		if (ping <= 160)
		{
			return new Color(0.95f, 0.85f, 0.35f, 1f);
		}

		return new Color(0.95f, 0.4f, 0.35f, 1f);
	}

	private void EnsureHud()
	{
		if ((Object)(object)_hudRoot != (Object)null)
		{
			return;
		}

		_canvas = ((Component)this).gameObject.GetComponent<Canvas>();
		if ((Object)(object)_canvas == (Object)null)
		{
			_canvas = ((Component)this).gameObject.AddComponent<Canvas>();
		}

		_canvas.renderMode = RenderMode.ScreenSpaceOverlay;
		_canvas.sortingOrder = 500;
		_canvas.enabled = false;

		CanvasScaler scaler = ((Component)this).gameObject.GetComponent<CanvasScaler>();
		if ((Object)(object)scaler == (Object)null)
		{
			scaler = ((Component)this).gameObject.AddComponent<CanvasScaler>();
		}

		scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
		scaler.referenceResolution = new Vector2(1920f, 1080f);
		scaler.matchWidthOrHeight = 0.5f;

		_font = Resources.GetBuiltinResource<Font>("Arial.ttf");

		_hudRoot = new GameObject("PingHud");
		_hudRoot.transform.SetParent(((Component)this).transform, false);
		Image bg = _hudRoot.AddComponent<Image>();
		bg.color = new Color(0.05f, 0.07f, 0.09f, 0.72f);
		bg.raycastTarget = false;
		RectTransform rootRt = _hudRoot.GetComponent<RectTransform>();
		rootRt.anchorMin = new Vector2(1f, 1f);
		rootRt.anchorMax = new Vector2(1f, 1f);
		rootRt.pivot = new Vector2(1f, 1f);
		rootRt.sizeDelta = new Vector2(220f, 34f);
		rootRt.anchoredPosition = new Vector2(
			ConnectionGuardPlugin.HudOffsetX.Value,
			ConnectionGuardPlugin.HudOffsetY.Value);

		GameObject textGo = new GameObject("PingText");
		textGo.transform.SetParent(_hudRoot.transform, false);
		_pingText = textGo.AddComponent<Text>();
		if ((Object)(object)_font != (Object)null)
		{
			_pingText.font = _font;
		}

		_pingText.fontSize = 16;
		_pingText.fontStyle = FontStyle.Bold;
		_pingText.alignment = TextAnchor.MiddleCenter;
		_pingText.color = Color.white;
		_pingText.text = "PING  --";
		_pingText.raycastTarget = false;
		RectTransform textRt = ((Graphic)_pingText).rectTransform;
		textRt.anchorMin = Vector2.zero;
		textRt.anchorMax = Vector2.one;
		textRt.offsetMin = new Vector2(8f, 2f);
		textRt.offsetMax = new Vector2(-8f, -2f);

		_hudRoot.SetActive(false);
	}

	private void SetHudVisible(bool visible)
	{
		if (_hudVisible == visible
			&& ((Object)(object)_hudRoot == (Object)null || _hudRoot.activeSelf == visible))
		{
			return;
		}

		_hudVisible = visible;

		if (!visible)
		{
			if ((Object)(object)_hudRoot != (Object)null && _hudRoot.activeSelf)
			{
				_hudRoot.SetActive(false);
			}

			if ((Object)(object)_canvas != (Object)null)
			{
				_canvas.enabled = false;
			}

			_lastPing = int.MinValue;
			return;
		}

		if ((Object)(object)_hudRoot == (Object)null)
		{
			EnsureHud();
		}

		if ((Object)(object)_canvas != (Object)null)
		{
			_canvas.enabled = true;
		}

		if (!_hudRoot.activeSelf)
		{
			_hudRoot.SetActive(true);
		}

		RectTransform rootRt = _hudRoot.GetComponent<RectTransform>();
		rootRt.anchoredPosition = new Vector2(
			ConnectionGuardPlugin.HudOffsetX.Value,
			ConnectionGuardPlugin.HudOffsetY.Value);
	}

	private static bool IsGameplaySceneName(string name)
	{
		if (string.IsNullOrEmpty(name))
		{
			return false;
		}

		string lower = name.ToLowerInvariant();
		return !lower.Contains("menu") && !lower.Contains("boot") && !lower.Contains("logo");
	}
}
