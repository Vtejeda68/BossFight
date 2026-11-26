using UnityEngine;
using UnityEngine.UI;

// Barra de vida del boss: arriba en el centro de la pantalla.
// Fondo gris = vida máxima; barra roja = vida actual (se vacía de derecha a izquierda).
//
// USO: ponlo en un GameObject vacío en la raíz de la escena (no dentro de otro Canvas)
// y arrastra el boss al campo 'Health'. El script crea solo el Canvas y las imágenes.
public class BossHealthBar : MonoBehaviour
{
    [Header("Referencia")]
    public BossHealth health;

    [Header("Aspecto")]
    public Vector2 size = new Vector2(700f, 28f);    // Ancho y alto de la barra (en unidades del Canvas)
    public float topMargin = 30f;                    // Separación con el borde superior
    public Color backgroundColor = new Color(0.25f, 0.25f, 0.25f, 1f);   // Gris: vida máxima
    public Color fillColor = new Color(0.85f, 0.1f, 0.1f, 1f);           // Rojo: vida actual
    public float smoothSpeed = 6f;                   // Suavizado al bajar (0 = instantáneo)

    private RectTransform fillRect;
    private float shownRatio = 1f;   // Lo que se está dibujando
    private float targetRatio = 1f;  // Lo que debería mostrarse

    void Awake()
    {
        BuildUI();
    }

    void OnEnable()
    {
        if (health != null) health.OnHealthChanged += HandleHealthChanged;
    }

    void OnDisable()
    {
        if (health != null) health.OnHealthChanged -= HandleHealthChanged;
    }

    void Start()
    {
        if (health == null)
        {
            Debug.LogWarning("BossHealthBar: falta asignar el campo 'Health' (arrastra el boss).");
            return;
        }

        // Estado inicial (BossHealth ya ha inicializado su vida en su Awake)
        targetRatio = shownRatio = Ratio(health.CurrentHealth, health.maxHealth);
        ApplyFill();
    }

    void Update()
    {
        if (Mathf.Approximately(shownRatio, targetRatio)) return;

        if (smoothSpeed <= 0f)
            shownRatio = targetRatio;
        else
            shownRatio = Mathf.Lerp(shownRatio, targetRatio, 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime));

        if (Mathf.Abs(shownRatio - targetRatio) < 0.001f) shownRatio = targetRatio;
        ApplyFill();
    }

    private void HandleHealthChanged(float current, float max)
    {
        targetRatio = Ratio(current, max);
    }

    private static float Ratio(float current, float max)
    {
        return max <= 0f ? 0f : Mathf.Clamp01(current / max);
    }

    // La barra roja se recorta cambiando el ancho de su ancla derecha
    private void ApplyFill()
    {
        if (fillRect != null)
            fillRect.anchorMax = new Vector2(shownRatio, 1f);
    }

    private void BuildUI()
    {
        // Canvas que cubre toda la pantalla
        Canvas canvas = gameObject.GetComponent<Canvas>();
        if (canvas == null) canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = gameObject.GetComponent<CanvasScaler>();
        if (scaler == null) scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        // Fondo gris (vida máxima), anclado arriba en el centro
        GameObject bg = new GameObject("BossBar_Background", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(transform, false);
        RectTransform bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = bgRect.anchorMax = new Vector2(0.5f, 1f);
        bgRect.pivot = new Vector2(0.5f, 1f);
        bgRect.anchoredPosition = new Vector2(0f, -topMargin);
        bgRect.sizeDelta = size;
        bg.GetComponent<Image>().color = backgroundColor;

        // Barra roja (vida actual): ocupa todo el fondo y se recorta desde la derecha
        GameObject fill = new GameObject("BossBar_Fill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(bg.transform, false);
        fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.pivot = new Vector2(0f, 0.5f);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        fill.GetComponent<Image>().color = fillColor;
    }
}
