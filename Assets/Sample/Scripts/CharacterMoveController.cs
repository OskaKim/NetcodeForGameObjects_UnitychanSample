﻿//using MLAPI.Messaging;
//using MLAPI.NetworkVariable;
using Unity.Netcode;


using Unity.Netcode.Components;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Services.Authentication;

namespace UTJ.NetcodeGameObjectSample
{
    // キャラクターの動きのコントローラー
    public class CharacterMoveController : Unity.Netcode.NetworkBehaviour
    {
        [SerializeField] private WeaponBehaviour weaponBehaviour;
        [SerializeField] private UnityEngine.UI.Text text;
        public TextMesh playerNameTextMesh;
        public ParticleSystem soundPlayingParticle;
        public AudioSource audioSouceComponent;
        public static CharacterMoveController Mine { get; private set; }

        public AudioClip[] audios;

        private Rigidbody rigidbodyComponent;
        private Animator animatorComponent;

        // Networkで同期する変数を作成します
        #region NETWORKED_VAR
        // Animationに流すスピード変数
        private NetworkVariable<float> speed = new NetworkVariable<float>( 0.0f);
        // プレイヤー名
        private NetworkVariable<Unity.Collections.FixedString64Bytes> playerName = new NetworkVariable<Unity.Collections.FixedString64Bytes>();
        #endregion NETWORKED_VAR


        // NetworkVariableはサーバーでしか更新できないので更新を依頼します
        [Unity.Netcode.ServerRpc(RequireOwnership = true)]
        private void SetSpeedServerRpc(float speed)
        {
            this.speed.Value = speed;
        }
        [Unity.Netcode.ServerRpc(RequireOwnership = true)]
        private void SetPlayerNameServerRpc(string name)
        {
            this.playerName.Value = name;
        }

        private void Awake()
        {
            text = GameObject.Find("Log").GetComponent<UnityEngine.UI.Text>();

            this.rigidbodyComponent = this.GetComponent<Rigidbody>();
            this.animatorComponent = this.GetComponent<Animator>();

            // Player名が変更になった時のコールバック指定
            this.playerName.OnValueChanged += OnChangePlayerName;

            // あとServer時に余計なものを削除します
#if UNITY_SERVER
            NetworkUtility.RemoveAllStandaloneComponents(this.gameObject);
#elif ENABLE_AUTO_CLIENT
            if (NetworkUtility.IsBatchModeRun)
            {
                NetworkUtility.RemoveAllStandaloneComponents(this.gameObject);
            }
#endif
        }


        private void Start()
        {
            if (IsOwner)
            {
                // プレイヤー名をセットします
                SetPlayerNameServerRpc( ConfigureConnectionBehaviour.playerName);
                // コントローラーの有効化をします
                ControllerBehaviour.Instance.Enable();
                Mine = this;
            }
        }
        private new void OnDestroy()
        {
            base.OnDestroy();
            if (IsOwner)
            {
                // コントローラーの無効化をします
                if (ControllerBehaviour.Instance)
                {
                    ControllerBehaviour.Instance.Disable();
                }
            }
        }

        // player名変更のコールバック
        void OnChangePlayerName(Unity.Collections.FixedString64Bytes prev,
            Unity.Collections.FixedString64Bytes current)
        {
            if (playerNameTextMesh != null)
            {
                playerNameTextMesh.text = current.Value;
            }
        }

        // Update is called once per frame
        void Update()
        {
            // TODO:::なんか OnValueChangedがおかしい…。
            // 自分より前にSpawnされた人の名前取れないんで Workaround
            playerNameTextMesh.text = this.playerName.Value.Value;
            // Animatorの速度更新(歩き・走り・静止などをSpeedでコントロールしてます)
            animatorComponent.SetFloat("Speed", speed.Value);
            // 音量調整
            this.audioSouceComponent.volume = SoundVolume.VoiceValue;

            // オーナーとして管理している場合、ここのUpdateを呼びます
            if (IsOwner)
            {
                UpdateAsOwner();
            }
        }


        // オーナーとしての処理
        private void UpdateAsOwner()
        {
            // 移動処理
            Vector3 move = ControllerBehaviour.Instance.LPadVector;
            float speedValue = move.magnitude;
            this.SetSpeedServerRpc(speedValue);
            move *= Time.deltaTime * 4.0f;
            rigidbodyComponent.position += move;

            // 移動している方角に向きます
            if (move.sqrMagnitude > 0.00001f)
            {
                rigidbodyComponent.rotation = Quaternion.LookRotation(move, Vector3.up);
            }
            // 底に落ちたら適当に復帰します。
            if (transform.position.y < -10.0f)
            {
                var randomPosition = new Vector3(Random.Range(-7, 7), 5.0f, Random.Range(-7, 7));
                transform.position = randomPosition;
            }
            // キーを押して音を流します
            for (int i = 0; i < this.audios.Length; ++i)
            {
                var type = (ButtonType)i;
                if (ControllerBehaviour.Instance.IsKeyDown(type))
                {
                    // 他の人に流してもらうために、サーバーにRPCします。
                    PlayAudioRequestOnServerRpc(i);
                }
            }
            if (ControllerBehaviour.Instance.IsKeyDown(ButtonType.Attack)) {
                AttacServerRpc();
            }
            // 入力の通知を通知します
            ControllerBehaviour.Instance.OnUpdateEnd();
        }

        [Unity.Netcode.ServerRpc(RequireOwnership = true)]
        public void AttacServerRpc(ServerRpcParams serverRpcParams = default) {
            var clientId = serverRpcParams.Receive.SenderClientId;
            Debug.Log(clientId);
            //AttackClientRpc(AuthenticationService.Instance.PlayerId);
        }

        [Unity.Netcode.ClientRpc]
        private void AttackClientRpc(string playerId) {
            Debug.Log("Attack Client Rpc");
            text.text = $"Attack Client Rpc : {playerId}";
            //weaponBehaviour.Fire();
        }

        // Clientからサーバーに呼び出されるRPCです。
        [Unity.Netcode.ServerRpc(RequireOwnership = true)]
        private void PlayAudioRequestOnServerRpc(int idx,ServerRpcParams serverRpcParams = default)
        {
            // PlayAudioを呼び出します
            PlayAudioClientRpc(idx);
        }

        // 音を再生します。付随してParticleをPlayします
        [Unity.Netcode.ClientRpc]
        private void PlayAudioClientRpc(int idx,ClientRpcParams clientRpcParams = default)
        {
            PlayAudio(idx);
        }
        private void PlayAudio(int idx) { 
            this.audioSouceComponent.clip = audios[idx];
            this.audioSouceComponent.Play();

            this.soundPlayingParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var mainModule = soundPlayingParticle.main;
            mainModule.duration = audios[idx].length;

            this.soundPlayingParticle.Play();
        }
    }
}