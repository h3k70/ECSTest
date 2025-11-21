using TMPro;
using UnityEngine;

public class FPSShower : MonoBehaviour
{
    [SerializeField] private TMP_Text _text;
    public float updateInterval = 0.5f; // Как часто обновлять значение FPS

    private float accum = 0.0f;
    private int frames = 0;
    private float timeleft;
    private float fps;

    private void Start()
    {
        timeleft = updateInterval;
    }

    private void Update()
    {
        // Сбор данных за промежуток времени
        timeleft -= Time.deltaTime;
        accum += Time.timeScale / Time.deltaTime;
        ++frames;

        // Интервал истек - вычисляем средний FPS
        if (timeleft <= 0.0f)
        {
            fps = accum / frames; // Среднее значение FPS
            timeleft = updateInterval;
            accum = 0.0f;
            frames = 0;
            _text.text = fps.ToString();
        }
    }
}
