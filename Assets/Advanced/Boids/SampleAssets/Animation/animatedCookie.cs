using System.Collections;
using UnityEngine;

public class animatedCookie : MonoBehaviour
{
    public Texture[] cookies;
    public float framesPerSecond = 15;
    private Light m_light;
    private int m_index = 0;

    private void Awake()
    {
        m_light = GetComponent<Light>();
        StartCoroutine(animateCookies());
    }

    private IEnumerator animateCookies()
    {
        while (true)
        {
            m_light.cookie = cookies[m_index];
            m_index++;
            if (m_index == cookies.Length)
            {
                m_index = 0;
            }

            yield return new WaitForSeconds(1 / framesPerSecond);
        }
    }
}