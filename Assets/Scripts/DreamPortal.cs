using UnityEngine;

public class DreamPortal : MonoBehaviour
{
   

   private void OnTriggerEnter(Collider other) {
         if (other.CompareTag("Player"))
         {
                WorldManager.Instance.ToggleWorld();
         }
   }
}
