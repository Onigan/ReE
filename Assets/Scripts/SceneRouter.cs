using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneRouter : MonoBehaviour
{
    public void GoVillage() { SceneManager.LoadScene("Village"); }
    public void GoForest() { SceneManager.LoadScene("Forest"); }
    public void GoBattle() { SceneManager.LoadScene("Battle"); }
}
