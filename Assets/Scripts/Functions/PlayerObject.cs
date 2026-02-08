using UnityEngine;

public class PlayerObject : MonoBehaviour
{
    public GameObject[] characterModel;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public void ShowCharacter(int index)
    {
        for (int i = 0; i < characterModel.Length; i++)
        {
            characterModel[i].SetActive(i == index);
        }
    }

}
