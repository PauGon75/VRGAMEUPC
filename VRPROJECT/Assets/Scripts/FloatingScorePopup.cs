using UnityEngine;
using TMPro;

public class FloatingScorePopup : MonoBehaviour
{
    [SerializeField] private TMP_Text textMesh;
    [SerializeField] private float floatSpeed = 1.2f;
    [SerializeField] private float lifetime = 0.65f;

    private Color startColor;
    private float elapsed = 0f;

    public void Setup(string text, Color color)
    {
        if (textMesh == null) textMesh = GetComponentInChildren<TMP_Text>();
        if (textMesh != null)
        {
            textMesh.text = text;
            textMesh.color = color;
            startColor = color;
        }

        if (Camera.main != null)
        {
            transform.rotation = Quaternion.LookRotation(transform.position - Camera.main.transform.position);
        }
    }

    private void Update()
    {
        transform.position += Vector3.up * (floatSpeed * Time.deltaTime);
        elapsed += Time.deltaTime;

        if (textMesh != null)
        {
            float alpha = Mathf.Clamp01(1f - (elapsed / lifetime));
            textMesh.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
        }

        if (elapsed >= lifetime)
        {
            Destroy(gameObject);
        }
    }
}