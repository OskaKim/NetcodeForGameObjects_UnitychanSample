using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UTJ.NetcodeGameObjectSample;

public class GoalBehaviour : MonoBehaviour {
    private void OnTriggerEnter(Collider other) {
        CharacterMoveController characterMoveController = null;
        if (other.gameObject.TryGetComponent<CharacterMoveController>(out characterMoveController)) {
            bool isMine = CharacterMoveController.Mine == characterMoveController;
            if (isMine) {
                Debug.Log("Goal!");
            }
        }
    }
}
