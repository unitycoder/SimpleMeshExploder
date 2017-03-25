using UnityEngine;
using System.Collections;

// note: fade doesnt work if the material doest support transparency..

public class MeshFader : MonoBehaviour
{
    private bool fadeOut = false;

    void Update()
    {
        if (fadeOut) return;

        // wait until rigibody is spleeping
        if (GetComponent<Rigidbody>().IsSleeping())
        {
            fadeOut = true;
            StartCoroutine(FadeOut());
        }
    }

    IEnumerator FadeOut()
    {
        float fadeTime = 2.0f;
        var rend = GetComponent<Renderer>();

        var startColor = Color.white;
        var endColor = new Color(1, 1, 1, 0);

        for (float t = 0.0f; t < fadeTime; t += Time.deltaTime)
        {
            rend.material.color = Color.Lerp(startColor, endColor, t / fadeTime);
            yield return null;
        }
        Destroy(gameObject);
    }
}
